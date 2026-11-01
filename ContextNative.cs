using HarmonyLib;

namespace SmartInventoryArrange;

/// <summary>Strongly typed access to private Mono fields exposed publicly by IL2CPP interop upstream.</summary>
internal static class ContextNative
{
    private static readonly AccessTools.FieldRef<ItemContextHandler, GameGridInventory> CurrentInventoryField =
        AccessTools.FieldRefAccess<ItemContextHandler, GameGridInventory>("currentInventory");
    private static readonly AccessTools.FieldRef<ItemContextHandler, PixelWindow> SortWindowField =
        AccessTools.FieldRefAccess<ItemContextHandler, PixelWindow>("sortWindow");
    private static readonly AccessTools.FieldRef<ItemContextHandler, RenderHandler.ColorPalette> TextColorField =
        AccessTools.FieldRefAccess<ItemContextHandler, RenderHandler.ColorPalette>("textColor");
    private static readonly AccessTools.FieldRef<ItemContextHandler, RichTextElement> BigStartField =
        AccessTools.FieldRefAccess<ItemContextHandler, RichTextElement>("buttonSortBigStart");
    private static readonly AccessTools.FieldRef<ItemContextHandler, RichTextElement> SmallStartField =
        AccessTools.FieldRefAccess<ItemContextHandler, RichTextElement>("buttonSortSmallStart");
    private static readonly AccessTools.FieldRef<ItemContextHandler, RichTextElement> BigEndField =
        AccessTools.FieldRefAccess<ItemContextHandler, RichTextElement>("buttonSortBigEnd");
    private static readonly AccessTools.FieldRef<ItemContextHandler, RichTextElement> SmallEndField =
        AccessTools.FieldRefAccess<ItemContextHandler, RichTextElement>("buttonSortSmallEnd");
    private static readonly AccessTools.FieldRef<ItemContextHandler, RichTextElement> UndoField =
        AccessTools.FieldRefAccess<ItemContextHandler, RichTextElement>("buttonSortUndo");
    private static readonly AccessTools.FieldRef<RichTextElement, int> LeftPaddingField = AccessTools.FieldRefAccess<RichTextElement, int>("leftPadding");
    private static readonly AccessTools.FieldRef<RichTextElement, int> RightPaddingField = AccessTools.FieldRefAccess<RichTextElement, int>("rightPadding");
    private static readonly AccessTools.FieldRef<RichTextElement, int> TopPaddingField = AccessTools.FieldRefAccess<RichTextElement, int>("topPadding");
    private static readonly AccessTools.FieldRef<RichTextElement, int> BottomPaddingField = AccessTools.FieldRefAccess<RichTextElement, int>("bottomPadding");
    private static readonly AccessTools.FieldRef<GridPixelElement, int> XSpacingField = AccessTools.FieldRefAccess<GridPixelElement, int>("_xSpacing");
    private static readonly AccessTools.FieldRef<GridPixelElement, int> YSpacingField = AccessTools.FieldRefAccess<GridPixelElement, int>("_ySpacing");

    internal static GameGridInventory Inventory(ItemContextHandler owner) => CurrentInventoryField(owner);
    internal static PixelWindow SortWindow(ItemContextHandler owner) => SortWindowField(owner);
    internal static RenderHandler.ColorPalette TextColor(ItemContextHandler owner) => TextColorField(owner);
    internal static RichTextElement SmallEnd(ItemContextHandler owner) => SmallEndField(owner);
    internal static RichTextElement[] NativeSortButtons(ItemContextHandler owner) =>
        new[] { BigStartField(owner), SmallStartField(owner), BigEndField(owner), SmallEndField(owner), UndoField(owner) };
    internal static (int Left, int Right, int Top, int Bottom) Padding(RichTextElement element) =>
        (LeftPaddingField(element), RightPaddingField(element), TopPaddingField(element), BottomPaddingField(element));
    internal static int XSpacing(GridPixelElement grid) => XSpacingField(grid);
    internal static int YSpacing(GridPixelElement grid) => YSpacingField(grid);
}
