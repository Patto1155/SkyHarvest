// Assets/Tests/EditMode/UILayoutTests.cs
// Portrait HUD layout math — the hotbar must fit the narrow edge of a phone screen.
using NUnit.Framework;
using UnityEngine;
using SkyHarvest.UI;

[TestFixture]
public class UILayoutTests
{
    private const float Usable = UILayout.RefWidth - 2f * UILayout.Margin;

    [Test]
    public void Ten_Slots_Fit_Across_A_Portrait_Screen()
    {
        float spacing = UILayout.HotbarSpacing(10, 56f, 4f, Usable);
        float leftEdge  = UILayout.SlotOffsetX(0, 10, spacing) - 28f;
        float rightEdge = UILayout.SlotOffsetX(9, 10, spacing) + 28f;
        Assert.GreaterOrEqual(leftEdge, -Usable / 2f - 0.01f);
        Assert.LessOrEqual(rightEdge, Usable / 2f + 0.01f);
    }

    [Test]
    public void Spacing_Prefers_Slot_Plus_Gap_When_There_Is_Room()
    {
        Assert.AreEqual(60f, UILayout.HotbarSpacing(4, 56f, 4f, 1000f), 0.001f);
    }

    [Test]
    public void Spacing_Shrinks_Rather_Than_Overflowing()
    {
        float spacing = UILayout.HotbarSpacing(10, 56f, 4f, 300f);
        Assert.Less(spacing, 60f);
        Assert.AreEqual(300f, UILayout.SlotOffsetX(9, 10, spacing) - UILayout.SlotOffsetX(0, 10, spacing) + 56f, 0.01f);
    }

    [Test]
    public void Slot_Offsets_Are_Symmetric_About_Centre()
    {
        float spacing = UILayout.HotbarSpacing(10, 56f, 4f, Usable);
        Assert.AreEqual(-UILayout.SlotOffsetX(0, 10, spacing), UILayout.SlotOffsetX(9, 10, spacing), 0.001f);
    }

    [Test]
    public void Single_Slot_Sits_Dead_Centre()
    {
        Assert.AreEqual(0f, UILayout.SlotOffsetX(0, 1, UILayout.HotbarSpacing(1, 56f, 4f, Usable)), 0.001f);
    }

    [Test]
    public void Reference_Canvas_Is_Portrait()
    {
        Assert.Less(UILayout.RefWidth, UILayout.RefHeight);
    }

    [Test]
    public void Anchor_Helpers_Set_Edge_Relative_Anchors()
    {
        var rt = new GameObject("t", typeof(RectTransform)).GetComponent<RectTransform>();

        UILayout.AnchorBottomCenter(rt, 80f);
        Assert.AreEqual(new Vector2(0.5f, 0f), rt.anchorMin);
        Assert.AreEqual(new Vector2(0f, 80f), rt.anchoredPosition);

        UILayout.AnchorTopRight(rt, 24f, 28f);
        Assert.AreEqual(new Vector2(1f, 1f), rt.anchorMin);
        Assert.AreEqual(new Vector2(-24f, -28f), rt.anchoredPosition);

        UILayout.Stretch(rt);
        Assert.AreEqual(Vector2.zero, rt.anchorMin);
        Assert.AreEqual(Vector2.one, rt.anchorMax);
    }
}
