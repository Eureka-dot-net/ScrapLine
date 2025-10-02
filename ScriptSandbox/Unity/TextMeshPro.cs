// Mock TextMeshPro classes
using System;
using UnityEngine;
using UnityEngine.UI;

namespace TMPro
{
    // Base TMP_Text class
    public class TMP_Text : Graphic
    {
        public string text { get; set; } = "";
        public float fontSize { get; set; } = 18f;
        public FontStyles fontStyle { get; set; } = FontStyles.Normal;
        public new Color color { get; set; } = Color.white;
        public TextAlignmentOptions alignment { get; set; } = TextAlignmentOptions.TopLeft;
        public bool enableAutoSizing { get; set; } = false;
        public float fontSizeMin { get; set; } = 0f;
        public float fontSizeMax { get; set; } = 72f;
        public TMP_FontAsset font { get; set; }
        public bool enableWordWrapping { get; set; } = true;
        public TextOverflowModes overflowMode { get; set; } = TextOverflowModes.Overflow;
        
        public enum FontStyles
        {
            Normal = 0,
            Bold = 1,
            Italic = 2,
            Underline = 4,
            LowerCase = 8,
            UpperCase = 16,
            SmallCaps = 32,
            Strikethrough = 64,
            Superscript = 128,
            Subscript = 256,
            Highlight = 512
        }
        
        public enum TextAlignmentOptions
        {
            TopLeft, Top, TopRight, TopJustified, TopFlush, TopGeoAligned,
            Left, Center, Right, Justified, Flush, CenterGeoAligned,
            BottomLeft, Bottom, BottomRight, BottomJustified, BottomFlush, BottomGeoAligned,
            BaselineLeft, Baseline, BaselineRight, BaselineJustified, BaselineFlush, BaselineGeoAligned,
            MidlineLeft, Midline, MidlineRight, MidlineJustified, MidlineFlush, MidlineGeoAligned,
            CaplineLeft, Capline, CaplineRight, CaplineJustified, CaplineFlush, CaplineGeoAligned,
            Converted
        }
    }
    
    // TextOverflowModes enum for TMP
    public enum TextOverflowModes
    {
        Overflow = 0,
        Ellipsis = 1,
        Masking = 2,
        Truncate = 3,
        ScrollRect = 4,
        Page = 5,
        Linked = 6
    }

    // TextMeshProUGUI class for UI text
    public class TextMeshProUGUI : TMP_Text
    {
        // Inherits all functionality from TMP_Text
    }

    // TextMeshPro class for 3D text
    public class TextMeshPro : TMP_Text
    {
        // Same functionality as TMP_Text but for 3D text meshes
    }

    // TMP_FontAsset class
    public class TMP_FontAsset : ScriptableObject
    {
        public new string name { get; set; }
        public Font sourceFontFile { get; set; }
    }

    // TMP_InputField class
    public class TMP_InputField : Selectable
    {
        public string text { get; set; } = "";
        public TMP_Text textComponent { get; set; }
        public TMP_Text placeholder { get; set; }
        public OnChangeEvent onValueChanged { get; set; } = new OnChangeEvent();
        public SubmitEvent onSubmit { get; set; } = new SubmitEvent();
        public SubmitEvent onEndEdit { get; set; } = new SubmitEvent();
        
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

    // TMP_Dropdown class
    public class TMP_Dropdown : Selectable
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