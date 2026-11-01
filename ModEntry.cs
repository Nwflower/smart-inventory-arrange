using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using SmartInventoryArrange.Core;
using UnityEngine;

namespace SmartInventoryArrange;

/// <summary>Formal-edition IMod entry; original algorithm by xiepeixuan (MIT).</summary>
public sealed class ModEntry : IMod
{
    private const string HarmonyId = "smart_inventory_arrange.context";
    private static readonly GlobalArrangeController Search = new();
    private static Harmony? _harmony;
    internal static bool Busy;

    public void Init(ModManifest manifest)
    {
        ModRuntime.Init(manifest);
        ModRuntime.Msg($"{manifest.Name} v{manifest.ModVersion} initialized; algorithm ported from SmartInventoryArrange v3.17.10 (MIT).");
    }

    public void OnEnable()
    {
        try
        {
            ModRuntime.Start();
            _harmony = new Harmony(HarmonyId);
            Patch("OnEventInit", typeof(ContextInitPatch), nameof(ContextInitPatch.Postfix), false);
            Patch("TriggerButton", typeof(ContextTriggerPatch), nameof(ContextTriggerPatch.Prefix), true);
            Patch("OnEventReset", typeof(ContextResetPatch), nameof(ContextResetPatch.Postfix), false);
            ModRuntime.Msg("Smart arrange context-menu patches enabled.");
        }
        catch (Exception ex)
        {
            ModRuntime.Error("Patch setup failed: " + ex);
            try { _harmony?.UnpatchAll(HarmonyId); } catch { }
            _harmony = null;
        }
    }

    private static void Patch(string targetName, Type patchType, string patchName, bool prefix)
    {
        var target = AccessTools.Method(typeof(ItemContextHandler), targetName)
            ?? throw new InvalidOperationException("Missing target " + targetName);
        var method = new HarmonyMethod(patchType, patchName);
        if (prefix) _harmony!.Patch(target, prefix: method);
        else _harmony!.Patch(target, postfix: method);
    }

    public void OnDisable()
    {
        try { _harmony?.UnpatchAll(HarmonyId); } catch { }
        _harmony = null;
        NativeSmartMenu.Close();
        if (!Busy) Search.Reset();
        ModRuntime.Stop();
        ModRuntime.Msg("Smart arrange disabled.");
    }

    internal static void Run(GameGridInventory inventory, bool preserveFixedItems = false)
    {
        if (Busy) { ModRuntime.Warning("Smart arrange ignored: an arrangement is already running."); return; }
        Busy = true;
        try
        {
            bool keepFixedItems = preserveFixedItems && ModRuntime.KeepFixedItems;
            var tx = InventoryTransaction.Capture(inventory, keepFixedItems);
            ModRuntime.Msg($"Smart arrange mode: requested={(preserveFixedItems ? "preserve-fixed" : "smart")}, keepFixedItems={keepFixedItems}, preservedFixedItems={tx.PreservedFixedCount}");
            string snapshotPath = tx.WriteDiagnosticSnapshot();
            ModRuntime.Msg($"Smart arrange exact input: {tx.WriteExactInput()}");
            double occupancy = DeterministicSolver.OccupancyRatio(tx.Problem);
            ModRuntime.Msg($"Smart arrange snapshot: {tx.Width}x{tx.Height}, occupancy={occupancy:P1}, mode={DeterministicSolver.DensityMode(tx.Problem)}, movable={tx.MovableCount}, fixed={tx.FixedCount}, categories={tx.CategoryCount}, multi-category={tx.MultiCategoryCount}, unknown={tx.UnknownCategoryCount}, details={snapshotPath}");
            if (tx.MovableCount == 0)
            {
                tx.WriteResultSnapshot(tx.OriginalPlacements, "empty");
                ModRuntime.Msg("Smart arrange: inventory is empty or contains only fixed items.");
                Busy = false;
                return;
            }
            var request = Search.Begin(tx.Problem, tx.OriginalPlacements, tx.SessionContext);
            ModRuntime.Msg($"Smart arrange request: action={(request.Restarted ? "fresh-global" : "resume-frontier")}, input={request.InputSignature}, nodeBudget={request.NodeBudget}, previousNodes={request.Session.TotalNodes}");
            ModRuntime.Run(SolveAndApply(tx, request));
        }
        catch (Exception ex)
        {
            Busy = false;
            Search.Reset();
            ModRuntime.Error("Smart arrange snapshot cancelled: " + ex);
        }
    }

    private static IEnumerator SolveAndApply(InventoryTransaction tx, GlobalArrangeController.Request request)
    {
        bool applied = false, applicationStarted = false;
        var task = Task.Run(() =>
        {
            var progress = request.Session.Advance(request.NodeBudget, request.SolutionBatch);
            var baseline = DeterministicSolver.Evaluate(tx.Problem, tx.OriginalPlacements);
            return (result: tx.OrderInstances(progress.Result), baseline, Diagnostic: request.Session.Diagnostic, progress);
        });
        while (!task.IsCompleted) yield return null;
        try
        {
            if (task.IsFaulted) throw task.Exception?.GetBaseException() ?? new InvalidOperationException("Background solver failed.");
            if (task.IsCanceled) throw new OperationCanceledException("Background solver was cancelled.");
            var solved = task.Result;
            var result = solved.result;
            tx.AssertSnapshotCurrent();
            ModRuntime.Msg($"Smart arrange search complete: batch={solved.progress.Batch}, newNodes={solved.progress.NewNodes}, totalNodes={solved.progress.TotalNodes}, newCandidates={solved.progress.NewCandidates}, uniqueCandidates={solved.progress.TotalCandidates}, canContinue={solved.progress.CanContinue}, reason={solved.progress.Reason}");
            if (!Native34Solver.ValidateLayout(tx.Problem, result.Placements))
                throw new InvalidOperationException("Final immutable geometry validation failed.");
            if (!result.Complete || Native34Solver.SameLayout(result.Placements, tx.OriginalPlacements))
            {
                Search.Complete(request, tx.Problem, tx.OriginalPlacements);
                tx.WriteResultSnapshot(tx.OriginalPlacements, "kept-original");
                ModRuntime.Msg($"Smart arrange kept original layout: reason={(result.UsedFallback ? "no-fresh-complete-candidate-original-preserved" : solved.progress.Reason)}, mode={DeterministicSolver.DensityMode(tx.Problem)}, occupancy={DeterministicSolver.OccupancyRatio(tx.Problem):P1}, elapsed={result.Elapsed.TotalMilliseconds:F1}ms, visual={VisualSummary(result.Score)}, baseline={VisualSummary(solved.baseline)}, diagnostic={solved.Diagnostic}");
                yield break;
            }
            applicationStarted = true;
            tx.Apply(result.Placements);
            applied = true;
            tx.WriteResultSnapshot(result.Placements, "applied");
            Search.Complete(request, tx.Problem, result.Placements);
            ModRuntime.Msg($"Smart arrange success: mode={DeterministicSolver.DensityMode(tx.Problem)}, occupancy={DeterministicSolver.OccupancyRatio(tx.Problem):P1}, placed={result.Placements.Count}, rotations={result.Placements.Count(p => p.Rotated)}, holes={result.Holes}, narrow={result.NarrowFragments}, elapsed={result.Elapsed.TotalMilliseconds:F1}ms, visual={VisualSummary(result.Score)}, splitGroups={DeterministicSolver.DescribeSplits(result.Placements)}, native34={solved.Diagnostic}");
        }
        catch (Exception ex)
        {
            Search.Reset();
            try
            {
                if (applied || applicationStarted) tx.RestoreCapturedSnapshot();
                tx.WriteResultSnapshot(tx.OriginalPlacements, applied || applicationStarted ? "cancelled-restored" : "cancelled-before-apply");
            }
            catch (Exception logEx) { ModRuntime.Warning("Final coordinate snapshot failed: " + logEx.Message); }
            ModRuntime.Error($"Smart arrange {(applied || applicationStarted ? "cancelled-restored" : "cancelled-before-apply")}: " + ex);
        }
        finally { Busy = false; }
    }

    private static string VisualSummary(LayoutScore score) =>
        $"typeSplits={-score.NegativeTypeRuns},typeRows={score.SameTypeRowAlignment},typeEdges={score.SameTypeHorizontalContact},cohortSplits={-score.NegativeCohortRuns},cohortRows={score.CohortRowAlignment},cohortEdges={score.CohortHorizontalContact}";
}

internal static class ContextInitPatch
{
    internal static void Postfix(ItemContextHandler __instance, bool __result)
    {
        if (!__result || ContextNative.SortWindow(__instance) == null || ContextNative.SmallEnd(__instance) == null) return;
        try { NativeSmartMenu.Open(__instance); }
        catch (Exception ex) { NativeSmartMenu.Close(); ModRuntime.Error("Smart arrange menu injection failed: " + ex); }
    }

    internal static bool IsChinese()
    {
        try
        {
            string code = LocHelper.GetCurrentLocaleCode() ?? "";
            return code.StartsWith("zh", StringComparison.OrdinalIgnoreCase) || code.IndexOf("chinese", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch { return Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified || Application.systemLanguage == SystemLanguage.ChineseTraditional; }
    }
}

internal static class NativeSmartMenu
{
    internal static RichTextElement? Button, PreserveButton;
    internal static GridPixelElement? Grid;
    private static PixelWindow? _window;

    internal static void Open(ItemContextHandler owner)
    {
        var window = ContextNative.SortWindow(owner);
        var oldElement = window.childElement ?? throw new InvalidOperationException("Sort window has no child element.");
        if (Grid != null && Button != null && PreserveButton != null && ReferenceEquals(_window, window) && ReferenceEquals(oldElement, Grid)) return;
        Close();
        var oldGrid = oldElement as GridPixelElement ?? throw new InvalidOperationException("Native sort element is not a grid.");
        var nativeButtons = ContextNative.NativeSortButtons(owner);
        if (nativeButtons.Any(x => x == null)) throw new InvalidOperationException("One or more native sort buttons are missing.");
        var source = ContextNative.SmallEnd(owner);
        int rowHeight = Math.Max(1, source.heightPixels);
        int width = Math.Max(window.widthPixels, nativeButtons.Max(x => x.widthPixels));
        RichTextElement MakeButton(string text)
        {
            var button = new RichTextElement(false, width, rowHeight, true);
            var padding = ContextNative.Padding(source);
            button.SetPadding(padding.Left, padding.Right, padding.Top, padding.Bottom);
            button.SetText(text, Math.Max(source.maxCharWidth, text.Length), ContextNative.TextColor(owner));
            button.Validate();
            return button;
        }
        var smart = MakeButton(ContextInitPatch.IsChinese() ? "智能排列" : "Smart Arrange");
        var preserve = MakeButton(ContextInitPatch.IsChinese() ? "智能排列（不动固定物品）" : "Smart Arrange (Keep Fixed Items)");
        width = Math.Max(width, Math.Max(smart.widthPixels, preserve.widthPixels));
        var grid = new GridPixelElement(1, nativeButtons.Length + 2, false).SetSpacing(ContextNative.XSpacing(oldGrid), ContextNative.YSpacing(oldGrid));
        grid.justifyWidth = oldGrid.justifyWidth;
        grid.searchUsingPriority = oldGrid.searchUsingPriority;
        int oldWidth = window.widthPixels, oldHeight = window.heightPixels;
        if (!window.Detach(oldElement)) throw new InvalidOperationException("Failed to detach the native sort grid.");
        oldGrid.DetachAll();
        try
        {
            for (int row = 0; row < nativeButtons.Length; row++)
                if (!grid.AttachPos(nativeButtons[row], 0, row)) throw new InvalidOperationException($"Failed to attach native sort row {row}.");
            if (!grid.AttachPos(smart, 0, nativeButtons.Length) || !grid.AttachPos(preserve, 0, nativeButtons.Length + 1)) throw new InvalidOperationException("Failed to attach smart sort rows.");
            if (!window.Attach(grid)) throw new InvalidOperationException($"Failed to attach the expanded 1x{nativeButtons.Length + 2} sort grid.");
            window.ResizePixels(width, oldHeight + 2 * (rowHeight + ContextNative.YSpacing(oldGrid)));
            window.Validate();
            Button = smart; PreserveButton = preserve; Grid = grid; _window = window;
        }
        catch
        {
            window.Detach(grid); grid.DetachAll();
            for (int row = 0; row < nativeButtons.Length; row++) oldGrid.AttachPos(nativeButtons[row], 0, row);
            window.Attach(oldElement); window.ResizePixels(oldWidth, oldHeight); window.Validate();
            Close(); throw;
        }
        ModRuntime.Msg($"Smart arrange menu injected: nativeGrid={oldGrid.gridWidth}x{oldGrid.gridHeight}, expandedGrid=1x{nativeButtons.Length + 2}, window={window.widthPixels}x{window.heightPixels}");
    }

    internal static bool IsCurrent(ItemContextHandler owner) => Grid != null && ReferenceEquals(ContextNative.SortWindow(owner), _window) && ReferenceEquals(ContextNative.SortWindow(owner).childElement, Grid);
    internal static void Close() { Button = null; PreserveButton = null; Grid = null; _window = null; }
}

internal static class ContextTriggerPatch
{
    internal static bool Prefix(ItemContextHandler __instance, RichTextElement button)
    {
        try
        {
            bool current = NativeSmartMenu.IsCurrent(__instance);
            ModRuntime.Msg($"Smart arrange TriggerButton observed: current={current}");
            if (!current) return true;
            bool smart = ReferenceEquals(button, NativeSmartMenu.Button);
            bool preserve = ReferenceEquals(button, NativeSmartMenu.PreserveButton);
            if (!smart && !preserve) return true;
            ModRuntime.Msg($"Smart arrange button triggered: mode={(preserve ? "preserve-fixed" : "smart")}");
            var inventory = ContextNative.Inventory(__instance);
            ContextNative.SortWindow(__instance)?.Hide();
            if (inventory != null) ModEntry.Run(inventory, preserve);
            return false;
        }
        catch (Exception ex) { ModRuntime.Error("Smart arrange button failed; using native handler: " + ex); return true; }
    }
}

internal static class ContextResetPatch
{
    internal static void Postfix() => ModRuntime.Msg("Smart arrange context reset observed");
}
