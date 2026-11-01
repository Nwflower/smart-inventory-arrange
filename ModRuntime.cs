using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace SmartInventoryArrange;

/// <summary>Official-loader services shared by the ported solver and native adapter.</summary>
internal static class ModRuntime
{
    internal const string ModId = "smart_inventory_arrange";
    private static ModLog? _log;
    private static GameObject? _host;
    private static ConfigEntry<bool>? _keepFixedItems;

    internal static bool KeepFixedItems => _keepFixedItems?.Value ?? true;

    internal static void Init(ModManifest manifest)
    {
        _log = new ModLog(manifest);
        var config = ModConfig.For(ModId);
        _keepFixedItems = config.Define("keep_fixed_items", true,
            "整理", () => "智能排列（不动固定物品）固定图钉物品",
            () => "使用“智能排列（不动固定物品）”时，游戏内用图钉固定的物品保持原位置和朝向。");
        config.Save();
    }

    internal static void Start()
    {
        if (_host != null) return;
        _host = new GameObject("SmartInventoryArrange_Runner");
        UnityEngine.Object.DontDestroyOnLoad(_host);
        _host.AddComponent<CoroutineRunner>();
    }

    internal static void Stop()
    {
        FlushRepeat();
        if (_host != null) UnityEngine.Object.Destroy(_host);
        _host = null;
    }

    internal static void Run(IEnumerator routine)
    {
        if (_host == null) throw new InvalidOperationException("Coroutine host is not available.");
        _host.GetComponent<CoroutineRunner>().StartCoroutine(routine);
    }

    internal static void WriteText(string relativePath, string text, bool append = false)
    {
        string previous = append && ModHelper.ModDataFileExists(ModId, relativePath)
            ? ModHelper.ReadModDataFileText(ModId, relativePath)
            : "";
        ModHelper.WriteModDataFileText(ModId, relativePath, previous + text);
    }

    // 同一行连续重复时只写一次，并在下一行不同日志前补上次数。
    // 例：ContextResetPatch 每次上下文重置都会打一行，单局可达数百次，刷屏且没有新信息。
    private static string? _lastMessage;
    private static int _repeatCount;

    internal static void Msg(string message)
    {
        if (string.Equals(message, _lastMessage, StringComparison.Ordinal))
        {
            _repeatCount++;
            return;
        }
        FlushRepeat();
        _lastMessage = message;
        _log?.Log(message);
    }

    private static void FlushRepeat()
    {
        if (_repeatCount > 0 && _log != null)
            _log.Log($"（上一条日志重复 {_repeatCount} 次，已合并）");
        _repeatCount = 0;
        _lastMessage = null;
    }

    internal static void Warning(string message)
    {
        FlushRepeat();
        _log?.Log("WARNING: " + message);
    }

    internal static void Error(string message)
    {
        FlushRepeat();
        _log?.Log("ERROR: " + message);
    }

    private sealed class CoroutineRunner : MonoBehaviour { }
}

/// <summary>
/// Mono has stable managed object references rather than IL2CPP Pointer wrappers.
/// Synthetic IDs keep the ported immutable snapshot model unchanged; they are
/// identity tokens only and never affect layout ordering.
/// </summary>
internal static class NativeIdentity
{
    private sealed class Token
    {
        internal readonly nint Value;
        internal Token(nint value) { Value = value; }
    }

    private static readonly ConditionalWeakTable<object, Token> Tokens = new();
    private static long _next;

    internal static nint Of(object? value)
    {
        if (value == null) return 0;
        return Tokens.GetValue(value, _ => new Token((nint)Interlocked.Increment(ref _next))).Value;
    }
}

