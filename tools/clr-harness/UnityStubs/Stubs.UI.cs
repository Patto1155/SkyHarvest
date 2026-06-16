// UnityEngine stub — UI: Canvas, RectTransform, Image, Text, Button, ScrollRect, EventSystem, etc.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    // -------------------------------------------------------------------------
    // Font
    // -------------------------------------------------------------------------
    public partial class Font : Object
    {
        public int fontSize { get; set; } = 12;
        public bool dynamic { get; set; }
        public static Font? CreateDynamicFontFromOSFont(string name, int size) => new Font { name = name, fontSize = size };
        public bool HasCharacter(char c) => true;
        public static string[] GetOSInstalledFontNames() => Array.Empty<string>();
    }

    // -------------------------------------------------------------------------
    // TextAnchor / FontStyle
    // -------------------------------------------------------------------------
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public enum TextAlignment { Left, Center, Right }

    // -------------------------------------------------------------------------
    // RectTransform
    // -------------------------------------------------------------------------
    public partial class RectTransform : Transform
    {
        public Vector2 anchoredPosition { get; set; }
        public Vector2 anchorMin { get; set; } = new Vector2(0.5f, 0.5f);
        public Vector2 anchorMax { get; set; } = new Vector2(0.5f, 0.5f);
        public Vector2 pivot { get; set; } = new Vector2(0.5f, 0.5f);
        public Vector2 sizeDelta { get; set; } = new Vector2(100, 100);
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Rect rect => new Rect(-sizeDelta.x * pivot.x, -sizeDelta.y * pivot.y, sizeDelta.x, sizeDelta.y);

        public void SetInsetAndSizeFromParentEdge(RectTransform.Edge edge, float inset, float size) { }
        public void SetSizeWithCurrentAnchors(RectTransform.Axis axis, float size) { }
        public void ForceUpdateRectTransforms() { }

        public enum Edge { Left, Right, Top, Bottom }
        public enum Axis { Horizontal, Vertical }
    }
}

namespace UnityEngine.UI
{
    using UnityEngine;

    // -------------------------------------------------------------------------
    // Graphic (base)
    // -------------------------------------------------------------------------
    public partial class Graphic : Behaviour
    {
        public Color color { get; set; } = Color.white;
        public bool raycastTarget { get; set; } = true;
        public Material? material { get; set; }
        public RectTransform rectTransform => (RectTransform)transform;
        public virtual void SetAllDirty() { }
        public virtual void SetLayoutDirty() { }
        public virtual void SetVerticesDirty() { }
        public virtual void SetMaterialDirty() { }
        public virtual void Rebuild(CanvasUpdate executing) { }
        public virtual void LayoutComplete() { }
        public virtual void GraphicUpdateComplete() { }
    }

    public enum CanvasUpdate { Prelayout, Layout, PostLayout, PreRender, LatePreRender, MaxUpdateValue }

    // -------------------------------------------------------------------------
    // MaskableGraphic
    // -------------------------------------------------------------------------
    public partial class MaskableGraphic : Graphic
    {
        public bool maskable { get; set; } = true;
    }

    // -------------------------------------------------------------------------
    // Image
    // -------------------------------------------------------------------------
    public partial class Image : MaskableGraphic
    {
        public Sprite? sprite { get; set; }
        public Image.Type type { get; set; }
        public float fillAmount { get; set; } = 1f;
        public bool preserveAspect { get; set; }
        public bool useSpriteMesh { get; set; }

        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
        public Image.FillMethod fillMethod { get; set; }
        public bool fillClockwise { get; set; } = true;
        public int fillOrigin { get; set; }
    }

    // -------------------------------------------------------------------------
    // Text
    // -------------------------------------------------------------------------
    public partial class Text : MaskableGraphic
    {
        public string text { get; set; } = string.Empty;
        public Font? font { get; set; }
        public int fontSize { get; set; } = 14;
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; } = TextAnchor.UpperLeft;
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public int resizeTextMinSize { get; set; } = 10;
        public int resizeTextMaxSize { get; set; } = 40;
        public float lineSpacing { get; set; } = 1f;
    }

    // -------------------------------------------------------------------------
    // RawImage
    // -------------------------------------------------------------------------
    public partial class RawImage : MaskableGraphic
    {
        public Texture2D? texture { get; set; }
        public Rect uvRect { get; set; } = new Rect(0, 0, 1, 1);
    }

    // -------------------------------------------------------------------------
    // Button
    // -------------------------------------------------------------------------
    public partial class Button : Selectable
    {
        public ButtonClickedEvent onClick { get; } = new ButtonClickedEvent();

        public class ButtonClickedEvent : UnityEvent { }
    }

    // -------------------------------------------------------------------------
    // Toggle
    // -------------------------------------------------------------------------
    public partial class Toggle : Selectable
    {
        public bool isOn { get; set; }
        public ToggleEvent onValueChanged { get; } = new ToggleEvent();
        public class ToggleEvent : UnityEvent<bool> { }
    }

    // -------------------------------------------------------------------------
    // Slider
    // -------------------------------------------------------------------------
    public partial class Slider : Selectable
    {
        public float value { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; } = 1f;
        public bool wholeNumbers { get; set; }
        public SliderEvent onValueChanged { get; } = new SliderEvent();
        public class SliderEvent : UnityEvent<float> { }
    }

    // -------------------------------------------------------------------------
    // InputField
    // -------------------------------------------------------------------------
    public partial class InputField : Selectable
    {
        public string text { get; set; } = string.Empty;
        public int characterLimit { get; set; }
        public OnChangeEvent onValueChanged { get; } = new OnChangeEvent();
        public SubmitEvent onEndEdit { get; } = new SubmitEvent();
        public class OnChangeEvent : UnityEvent<string> { }
        public class SubmitEvent : UnityEvent<string> { }
    }

    // -------------------------------------------------------------------------
    // Selectable (base for interactive UI)
    // -------------------------------------------------------------------------
    public partial class Selectable : Behaviour
    {
        public bool interactable { get; set; } = true;
        public virtual void Select() { }
    }

    // -------------------------------------------------------------------------
    // ScrollRect
    // -------------------------------------------------------------------------
    public partial class ScrollRect : Behaviour
    {
        public RectTransform? content { get; set; }
        public RectTransform? viewport { get; set; }
        public float horizontalNormalizedPosition { get; set; }
        public float verticalNormalizedPosition { get; set; }
        public bool horizontal { get; set; } = true;
        public bool vertical { get; set; } = true;
        public ScrollRectEvent onValueChanged { get; } = new ScrollRectEvent();
        public class ScrollRectEvent : UnityEvent<Vector2> { }
        public void StopMovement() { }
        public void EnsureLayoutHasRebuilt() { }
    }

    // -------------------------------------------------------------------------
    // Layout Groups
    // -------------------------------------------------------------------------
    public partial class LayoutGroup : Behaviour
    {
        public RectOffset padding { get; set; } = new RectOffset();
        public TextAnchor childAlignment { get; set; }
    }

    public partial class HorizontalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; } = true;
        public bool childForceExpandHeight { get; set; } = true;
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
    }

    public partial class VerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; } = true;
        public bool childForceExpandHeight { get; set; } = true;
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
    }

    public partial class GridLayoutGroup : LayoutGroup
    {
        public Vector2 cellSize { get; set; } = new Vector2(100, 100);
        public Vector2 spacing { get; set; }
        public GridLayoutGroup.Corner startCorner { get; set; }
        public GridLayoutGroup.Axis startAxis { get; set; }
        public GridLayoutGroup.Constraint constraint { get; set; }
        public int constraintCount { get; set; }
        public enum Corner { UpperLeft, UpperRight, LowerLeft, LowerRight }
        public enum Axis { Horizontal, Vertical }
        public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
    }

    public partial class ContentSizeFitter : Behaviour
    {
        public ContentSizeFitter.FitMode horizontalFit { get; set; }
        public ContentSizeFitter.FitMode verticalFit { get; set; }
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
    }

    public partial class LayoutElement : Behaviour
    {
        public bool ignoreLayout { get; set; }
        public float minWidth { get; set; } = -1;
        public float minHeight { get; set; } = -1;
        public float preferredWidth { get; set; } = -1;
        public float preferredHeight { get; set; } = -1;
        public float flexibleWidth { get; set; } = -1;
        public float flexibleHeight { get; set; } = -1;
        public int layoutPriority { get; set; } = 1;
    }

    public class RectOffset
    {
        public int left, right, top, bottom;
        public int horizontal => left + right;
        public int vertical => top + bottom;
    }

    // -------------------------------------------------------------------------
    // Canvas / CanvasScaler / GraphicRaycaster / CanvasRenderer
    // -------------------------------------------------------------------------
}

namespace UnityEngine
{
    public partial class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public int sortingOrder { get; set; }
        public bool pixelPerfect { get; set; }
        public float scaleFactor { get; set; } = 1f;
        public float referencePixelsPerUnit { get; set; } = 100f;
        public Camera? worldCamera { get; set; }
        public int planeDistance { get; set; } = 100;
        public bool overrideSorting { get; set; }

        public static Canvas? FindObjectOfType() => null;
        public static void ForceUpdateCanvases() { }
    }

    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    public partial class CanvasScaler : Behaviour
    {
        public CanvasScaler.ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; } = new Vector2(1920, 1080);
        public float matchWidthOrHeight { get; set; } = 0.5f;
        public float physicalUnit { get; set; }
        public float fallbackScreenDPI { get; set; } = 96;
        public float defaultSpriteDPI { get; set; } = 96;
        public float scaleFactor { get; set; } = 1f;
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
    }

    public partial class GraphicRaycaster : Behaviour
    {
        public bool ignoreReversedGraphics { get; set; } = true;
        public GraphicRaycaster.BlockingObjects blockingObjects { get; set; }
        public enum BlockingObjects { None, TwoD, ThreeD, All }
    }

    public partial class CanvasRenderer : Component
    {
        public float alpha { get; set; } = 1f;
        public bool cullTransparentMesh { get; set; }
        public void SetColor(Color c) { }
        public void SetAlpha(float a) { alpha = a; }
    }
}

// -------------------------------------------------------------------------
// UnityEvent (simple delegate wrappers)
// -------------------------------------------------------------------------
namespace UnityEngine.Events
{
    public class UnityEvent
    {
        private System.Action? _action;
        public void AddListener(System.Action call) => _action += call;
        public void RemoveListener(System.Action call) => _action -= call;
        public void RemoveAllListeners() => _action = null;
        public void Invoke() => _action?.Invoke();
    }

    public class UnityEvent<T>
    {
        private System.Action<T>? _action;
        public void AddListener(System.Action<T> call) => _action += call;
        public void RemoveListener(System.Action<T> call) => _action -= call;
        public void RemoveAllListeners() => _action = null;
        public void Invoke(T arg) => _action?.Invoke(arg);
    }

    public class UnityEvent<T0, T1>
    {
        private System.Action<T0, T1>? _action;
        public void AddListener(System.Action<T0, T1> call) => _action += call;
        public void RemoveListener(System.Action<T0, T1> call) => _action -= call;
        public void RemoveAllListeners() => _action = null;
        public void Invoke(T0 a, T1 b) => _action?.Invoke(a, b);
    }
}

// Bring UI UnityEvent types into scope by inheriting from Events
namespace UnityEngine.UI
{
    public class UnityEvent : UnityEngine.Events.UnityEvent { }
    public class UnityEvent<T> : UnityEngine.Events.UnityEvent<T> { }
}

namespace UnityEngine.EventSystems
{
    public partial class EventSystem : UnityEngine.Behaviour
    {
        private static EventSystem? _current;
        public static EventSystem? current => _current ??= new EventSystem();
        public bool sendNavigationEvents { get; set; } = true;
        public int pixelDragThreshold { get; set; } = 10;
        public bool IsPointerOverGameObject(int pointerId = -1) => false;
        public bool IsPointerOverGameObject() => false;
    }

    public partial class StandaloneInputModule : UnityEngine.Behaviour
    {
        public string horizontalAxis { get; set; } = "Horizontal";
        public string verticalAxis { get; set; } = "Vertical";
        public string submitButton { get; set; } = "Submit";
        public string cancelButton { get; set; } = "Cancel";
    }

    public partial class BaseInputModule : UnityEngine.Behaviour { }

    public partial class PointerEventData
    {
        public enum InputButton { Left = 0, Right = 1, Middle = 2 }

        public UnityEngine.Vector2 position { get; set; }
        public UnityEngine.GameObject? pointerEnter { get; set; }
        public UnityEngine.GameObject? pointerCurrentRaycast { get; set; }
        public int pointerId { get; set; }
        public InputButton button { get; set; }
        public bool IsScrolling() => false;
    }

    public interface IPointerClickHandler { void OnPointerClick(PointerEventData eventData); }
    public interface IPointerDownHandler { void OnPointerDown(PointerEventData eventData); }
    public interface IPointerUpHandler { void OnPointerUp(PointerEventData eventData); }
    public interface IPointerEnterHandler { void OnPointerEnter(PointerEventData eventData); }
    public interface IPointerExitHandler { void OnPointerExit(PointerEventData eventData); }
    public interface IBeginDragHandler { void OnBeginDrag(PointerEventData eventData); }
    public interface IDragHandler { void OnDrag(PointerEventData eventData); }
    public interface IEndDragHandler { void OnEndDrag(PointerEventData eventData); }
    public interface IDropHandler { void OnDrop(PointerEventData eventData); }
    public interface IScrollHandler { void OnScroll(PointerEventData eventData); }

}

namespace UnityEngine
{
    // Real Unity exposes RectTransformUtility in the UnityEngine namespace, not
    // UnityEngine.EventSystems — GameCursor.cs (which only `using`s UnityEngine +
    // UnityEngine.UI) couldn't see it otherwise.
    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(
            RectTransform rect, Vector2 screenPoint, Camera? cam, out Vector2 localPoint)
        {
            localPoint = screenPoint;
            return true;
        }
    }
}
