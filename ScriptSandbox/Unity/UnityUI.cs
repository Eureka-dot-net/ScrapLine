// Mock Unity UI classes to enable compilation of Unity UI scripts
using System;
using UnityEngine;

namespace UnityEngine.UI
{
    // Selectable base class for UI elements
    public class Selectable : MonoBehaviour
    {
        public bool interactable { get; set; } = true;
        public Graphic targetGraphic { get; set; }
        public ColorBlock colors { get; set; } = ColorBlock.defaultColorBlock;
        
        public virtual void OnPointerDown(EventSystems.PointerEventData eventData) { }
        public virtual void OnPointerUp(EventSystems.PointerEventData eventData) { }
        public virtual void OnPointerEnter(EventSystems.PointerEventData eventData) { }
        public virtual void OnPointerExit(EventSystems.PointerEventData eventData) { }
    }

    // ColorBlock for UI color states
    [System.Serializable]
    public struct ColorBlock
    {
        public Color normalColor;
        public Color highlightedColor;
        public Color pressedColor;
        public Color selectedColor;
        public Color disabledColor;
        public float colorMultiplier;
        public float fadeDuration;
        
        public static ColorBlock defaultColorBlock => new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.9f, 0.9f, 0.9f),
            pressedColor = new Color(0.7f, 0.7f, 0.7f),
            selectedColor = new Color(0.9f, 0.9f, 0.9f),
            disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
    }

    // Button class
    public class Button : Selectable
    {
        public ButtonClickedEvent onClick { get; set; } = new ButtonClickedEvent();
        
        [System.Serializable]
        public class ButtonClickedEvent
        {
            private System.Action listeners;
            
            public void AddListener(System.Action call) => listeners += call;
            public void RemoveListener(System.Action call) => listeners -= call;
            public void RemoveAllListeners() => listeners = null;
            public void Invoke() => listeners?.Invoke();
        }
    }

    // Enums that exist in the UnityEngine.UI namespace (outside of classes)
    public enum TextAnchor 
    { 
        UpperLeft, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight
    }
    
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    // Image class
    public class Image : Graphic
    {
        public Sprite sprite { get; set; }
        public Type type { get; set; } = Type.Simple;
        public float fillAmount { get; set; } = 1f;
        public FillMethod fillMethod { get; set; } = FillMethod.Horizontal;
        public RectTransform rectTransform => GetComponent<RectTransform>();
        
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
    }

    // RawImage class
    public class RawImage : Graphic
    {
        public Texture texture { get; set; }
        public Rect uvRect { get; set; } = new Rect(0, 0, 1, 1);
        public RectTransform rectTransform => GetComponent<RectTransform>();
    }

    // Text class
    public class Text : Graphic
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; } = 14;
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public bool supportRichText { get; set; } = true;
        
        public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    }

    // Graphic base class
    public class Graphic : MonoBehaviour
    {
        public Color color { get; set; } = Color.white;
        public Material material { get; set; }
        public bool raycastTarget { get; set; } = true;
        
        public virtual void SetVerticesDirty() { }
        public virtual void SetMaterialDirty() { }
    }

    // CanvasGroup class
    public class CanvasGroup : Component
    {
        public float alpha { get; set; } = 1f;
        public bool interactable { get; set; } = true;
        public bool blocksRaycasts { get; set; } = true;
        public bool ignoreParentGroups { get; set; } = false;
    }

    // Layout elements
    public class LayoutElement : MonoBehaviour
    {
        public float minWidth { get; set; } = -1f;
        public float minHeight { get; set; } = -1f;
        public float preferredWidth { get; set; } = -1f;
        public float preferredHeight { get; set; } = -1f;
        public float flexibleWidth { get; set; } = -1f;
        public float flexibleHeight { get; set; } = -1f;
        public int layoutPriority { get; set; } = 1;
    }

    // GridLayoutGroup class
    public class GridLayoutGroup : MonoBehaviour
    {
        public Vector2 cellSize { get; set; } = new Vector2(100, 100);
        public Vector2 spacing { get; set; } = Vector2.zero;
        public Constraint constraint { get; set; } = Constraint.Flexible;
        public int constraintCount { get; set; } = 2;
        
        public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
    }

    // AspectRatioFitter class
    public class AspectRatioFitter : MonoBehaviour
    {
        public AspectMode aspectMode { get; set; } = AspectMode.FitInParent;
        public float aspectRatio { get; set; } = 1f;
        
        public enum AspectMode { None, WidthControlsHeight, HeightControlsWidth, FitInParent, EnvelopeParent }
    }

    // Outline effect
    public class Outline : MonoBehaviour
    {
        public Color effectColor { get; set; } = Color.black;
        public Vector2 effectDistance { get; set; } = new Vector2(1f, -1f);
        public bool useGraphicAlpha { get; set; } = true;
    }

    // ScrollRect class
    public class ScrollRect : MonoBehaviour
    {
        public RectTransform content { get; set; }
        public bool horizontal { get; set; } = true;
        public bool vertical { get; set; } = true;
        public ScrollbarVisibility horizontalScrollbarVisibility { get; set; }
        public ScrollbarVisibility verticalScrollbarVisibility { get; set; }
        
        public enum ScrollbarVisibility { Permanent, AutoHide, AutoHideAndExpandViewport }
    }

    // Slider class
    public class Slider : Selectable
    {
        public float value { get; set; } = 0f;
        public float minValue { get; set; } = 0f;
        public float maxValue { get; set; } = 1f;
        public SliderEvent onValueChanged { get; set; } = new SliderEvent();
        
        [System.Serializable]
        public class SliderEvent
        {
            private System.Action<float> listeners;
            
            public void AddListener(System.Action<float> call) => listeners += call;
            public void RemoveListener(System.Action<float> call) => listeners -= call;
            public void RemoveAllListeners() => listeners = null;
            public void Invoke(float value) => listeners?.Invoke(value);
        }
    }

    // Toggle class
    public class Toggle : Selectable
    {
        public bool isOn { get; set; } = false;
        public ToggleEvent onValueChanged { get; set; } = new ToggleEvent();
        
        [System.Serializable]
        public class ToggleEvent
        {
            private System.Action<bool> listeners;
            
            public void AddListener(System.Action<bool> call) => listeners += call;
            public void RemoveListener(System.Action<bool> call) => listeners -= call;
            public void RemoveAllListeners() => listeners = null;
            public void Invoke(bool value) => listeners?.Invoke(value);
        }
    }

    // InputField class
    public class InputField : Selectable
    {
        public string text { get; set; } = "";
        public InputType inputType { get; set; } = InputType.Standard;
        public OnChangeEvent onValueChanged { get; set; } = new OnChangeEvent();
        public SubmitEvent onEndEdit { get; set; } = new SubmitEvent();
        
        public enum InputType { Standard, AutoCorrect, IntegerNumber, DecimalNumber, Alphanumeric, Name, EmailAddress, Password, Pin }
        
        [System.Serializable]
        public class OnChangeEvent
        {
            private System.Action<string> listeners;
            
            public void AddListener(System.Action<string> call) => listeners += call;
            public void RemoveListener(System.Action<string> call) => listeners -= call;
            public void RemoveAllListeners() => listeners = null;
            public void Invoke(string value) => listeners?.Invoke(value);
        }
        
        [System.Serializable]
        public class SubmitEvent
        {
            private System.Action<string> listeners;
            
            public void AddListener(System.Action<string> call) => listeners += call;
            public void RemoveListener(System.Action<string> call) => listeners -= call;
            public void RemoveAllListeners() => listeners = null;
            public void Invoke(string value) => listeners?.Invoke(value);
        }
    }

    // Dropdown class
    public class Dropdown : Selectable
    {
        public int value { get; set; } = 0;
        public DropdownEvent onValueChanged { get; set; } = new DropdownEvent();
        
        [System.Serializable]
        public class DropdownEvent
        {
            private System.Action<int> listeners;
            
            public void AddListener(System.Action<int> call) => listeners += call;
            public void RemoveListener(System.Action<int> call) => listeners -= call;
            public void RemoveAllListeners() => listeners = null;
            public void Invoke(int value) => listeners?.Invoke(value);
        }
    }
}

namespace UnityEngine.EventSystems
{
    // PointerEventData class
    public class PointerEventData
    {
        public Vector2 position { get; set; }
        public Vector2 delta { get; set; }
        public GameObject pointerEnter { get; set; }
        public GameObject pointerPress { get; set; }
        public bool dragging { get; set; }
        public float clickTime { get; set; }
        public int clickCount { get; set; }
    }

    // Event interfaces
    public interface IPointerDownHandler
    {
        void OnPointerDown(PointerEventData eventData);
    }

    public interface IPointerUpHandler
    {
        void OnPointerUp(PointerEventData eventData);
    }

    public interface IPointerEnterHandler
    {
        void OnPointerEnter(PointerEventData eventData);
    }

    public interface IPointerExitHandler
    {
        void OnPointerExit(PointerEventData eventData);
    }

    public interface IDragHandler
    {
        void OnDrag(PointerEventData eventData);
    }

    public interface IBeginDragHandler
    {
        void OnBeginDrag(PointerEventData eventData);
    }

    public interface IEndDragHandler
    {
        void OnEndDrag(PointerEventData eventData);
    }

    public interface IDropHandler
    {
        void OnDrop(PointerEventData eventData);
    }

    public interface IScrollHandler
    {
        void OnScroll(PointerEventData eventData);
    }

    public interface IUpdateSelectedHandler
    {
        void OnUpdateSelected(BaseEventData eventData);
    }

    public interface ISelectHandler
    {
        void OnSelect(BaseEventData eventData);
    }

    public interface IDeselectHandler
    {
        void OnDeselect(BaseEventData eventData);
    }

    public interface IMoveHandler
    {
        void OnMove(AxisEventData eventData);
    }

    public interface ISubmitHandler
    {
        void OnSubmit(BaseEventData eventData);
    }

    public interface ICancelHandler
    {
        void OnCancel(BaseEventData eventData);
    }

    // BaseEventData class
    public class BaseEventData
    {
        public EventSystem currentInputModule { get; set; }
        public GameObject selectedObject { get; set; }
    }

    // AxisEventData class
    public class AxisEventData : BaseEventData
    {
        public Vector2 moveVector { get; set; }
        public MoveDirection moveDir { get; set; }
        
        public enum MoveDirection { Left, Up, Right, Down, None }
    }

    // EventSystem class
    public class EventSystem : MonoBehaviour
    {
        public static EventSystem current { get; set; }
        public GameObject currentSelectedGameObject { get; set; }
        
        public void SetSelectedGameObject(GameObject selected) => currentSelectedGameObject = selected;
        public void RaycastAll(PointerEventData eventData, System.Collections.Generic.List<RaycastResult> raycastResults) { }
    }

    // Standalone Input Module
    public class StandaloneInputModule : MonoBehaviour
    {
        public string horizontalAxis { get; set; } = "Horizontal";
        public string verticalAxis { get; set; } = "Vertical";
        public string submitButton { get; set; } = "Submit";
        public string cancelButton { get; set; } = "Cancel";
    }

    // FindObjectsInactive enum
    public enum FindObjectsInactive
    {
        Exclude,
        Include
    }
}

// RectTransform class (extends Transform for UI)
namespace UnityEngine
{
    public class RectTransform : Transform
    {
        public Vector2 anchoredPosition { get; set; }
        public Vector2 anchoredPosition3D { get; set; }
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Vector2 pivot { get; set; } = new Vector2(0.5f, 0.5f);
        public Vector2 sizeDelta { get; set; }
        public Rect rect { get; set; }
        
        public void SetInsetAndSizeFromParentEdge(RectTransform.Edge edge, float inset, float size) { }
        public void SetSizeWithCurrentAnchors(RectTransform.Axis axis, float size) { }
        
        public enum Edge { Left, Right, Top, Bottom }
        public enum Axis { Horizontal, Vertical }
    }

    // Canvas class
    public class Canvas : Component
    {
        public UnityEngine.UI.RenderMode renderMode { get; set; } = UnityEngine.UI.RenderMode.ScreenSpaceOverlay;
        public Camera worldCamera { get; set; }
        public int sortingOrder { get; set; }
        public bool overrideSorting { get; set; }
    }

    // CanvasRenderer class
    public class CanvasRenderer : Component
    {
        public bool cull { get; set; }
        public Color color { get; set; } = Color.white;
        public float alpha { get; set; } = 1f;
    }

    // Font class
    public class Font : Object
    {
        public int fontSize { get; set; }
        public string[] fontNames { get; set; }
        
        public static Font CreateDynamicFontFromOSFont(string fontname, int size) => new Font();
        public static Font CreateDynamicFontFromOSFont(string[] fontnames, int size) => new Font();
    }

    // Camera class
    public class Camera : Component
    {
        public float fieldOfView { get; set; } = 60f;
        public float nearClipPlane { get; set; } = 0.3f;
        public float farClipPlane { get; set; } = 1000f;
        public ClearFlags clearFlags { get; set; } = ClearFlags.Skybox;
        public Color backgroundColor { get; set; } = Color.blue;
        public bool orthographic { get; set; } = false;
        public float orthographicSize { get; set; } = 5f;
        
        public static Camera main { get; set; }
        public static Camera current { get; set; }
        
        public Vector3 ScreenToWorldPoint(Vector3 position) => position;
        public Vector3 WorldToScreenPoint(Vector3 position) => position;
        
        public enum ClearFlags { Skybox, Color, SolidColor, Nothing }
    }
}