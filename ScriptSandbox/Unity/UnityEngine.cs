// Mock Unity classes to enable compilation of Unity scripts
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    // Enum for FindObjects search options
    public enum FindObjectsInactive
    {
        Exclude,
        Include
    }

    // Enum for FindObjects sort mode
    public enum FindObjectsSortMode
    {
        None,
        InstanceID
    }

    // Base Unity object class
    public class Object
    {
        public string name { get; set; }
        
        public static void Destroy(Object obj) { }
        public static void DestroyImmediate(Object obj) { }
        public static void DontDestroyOnLoad(Object target) { } // Mock DontDestroyOnLoad
        public static T FindAnyObjectByType<T>() where T : Object => default(T);
        public static T FindFirstObjectByType<T>() where T : Object => default(T);
        public static T FindFirstObjectByType<T>(FindObjectsInactive findObjectsInactive) where T : Object => default(T);
        public static T FindObjectOfType<T>() where T : Object => default(T);
        public static T[] FindObjectsOfType<T>() where T : Object => new T[0];
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object => new T[0];
        public static T[] FindObjectsByType<T>(FindObjectsInactive findObjectsInactive, FindObjectsSortMode sortMode) where T : Object => new T[0];
        
        // Instantiate methods
        public static T Instantiate<T>(T original) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent, bool instantiateInWorldSpace) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object => original;
        public static Object Instantiate(Object original) => original;
        public static Object Instantiate(Object original, Transform parent) => original;
        public static Object Instantiate(Object original, Transform parent, bool instantiateInWorldSpace) => original;
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation) => original;
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent) => original;
        
        public int GetInstanceID() => GetHashCode();
    }

    // Component base class
    public class Component : Object
    {
        public GameObject gameObject { get; set; }
        public Transform transform { get; set; }
        
        public T GetComponent<T>() where T : Component => default(T);
        public T GetComponentInChildren<T>() where T : Component => default(T);
        public T GetComponentInChildren<T>(bool includeInactive) where T : Component => default(T);
        public T GetComponentInParent<T>() where T : Component => default(T);
        public T[] GetComponents<T>() where T : Component => new T[0];
        public T[] GetComponentsInChildren<T>() where T : Component => new T[0];
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component => new T[0];
        public T AddComponent<T>() where T : Component => default(T);
    }

    // MonoBehaviour - base for Unity scripts
    public class MonoBehaviour : Component
    {
        public bool enabled { get; set; } = true;
        
        // Unity lifecycle methods (empty implementations)
        protected virtual void Awake() { }
        protected virtual void Start() { }
        protected virtual void Update() { }
        protected virtual void FixedUpdate() { }
        protected virtual void LateUpdate() { }
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
        protected virtual void OnDestroy() { }
        
        // Coroutine support
        public Coroutine StartCoroutine(IEnumerator routine) => new Coroutine();
        public void StopCoroutine(Coroutine routine) { }
        public void StopAllCoroutines() { }
        
        // Invoke methods
        public void Invoke(string methodName, float time) { }
        public void InvokeRepeating(string methodName, float time, float repeatRate) { }
        public void CancelInvoke() { }
        public void CancelInvoke(string methodName) { }
        public bool IsInvoking() => false;
        public bool IsInvoking(string methodName) => false;
        
        public int GetInstanceID() => GetHashCode();
    }

    // GameObject class
    public class GameObject : Object
    {
        public bool activeSelf { get; set; } = true;
        public bool activeInHierarchy { get; set; } = true;
        public Transform transform { get; set; }
        
        public GameObject() { }
        public GameObject(string name) { this.name = name; }
        
        public T GetComponent<T>() where T : Component => default(T);
        public T GetComponentInChildren<T>() where T : Component => default(T);
        public T GetComponentInChildren<T>(bool includeInactive) where T : Component => default(T);
        public T GetComponentInParent<T>() where T : Component => default(T);
        public T[] GetComponents<T>() where T : Component => new T[0];
        public T[] GetComponentsInChildren<T>() where T : Component => new T[0];
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component => new T[0];
        public T AddComponent<T>() where T : Component => default(T);
        
        public void SetActive(bool value) { activeSelf = value; }
        
        // Static methods
        public static GameObject Find(string name) => null; // Mock implementation returns null
    }

    // Transform class with IEnumerable support
    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 localScale { get; set; } = Vector3.one;
        public Transform parent { get; set; }
        public int childCount { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 lossyScale { get; set; } = Vector3.one;
        
        public Transform GetChild(int index) => new Transform();
        public void SetParent(Transform parent) { this.parent = parent; }
        public void SetParent(Transform parent, bool worldPositionStays) { this.parent = parent; }
        public void SetSiblingIndex(int index) { }
        public void SetAsLastSibling() { }
        public int GetSiblingIndex() => 0;
        public Transform Find(string name) => new Transform();
        
        // IEnumerable implementation for foreach loops
        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < childCount; i++)
            {
                yield return GetChild(i);
            }
        }
    }

    // Vector3 structure
    [System.Serializable]
    public struct Vector3
    {
        public float x, y, z;
        
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; this.z = 0; }
        
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 left => new Vector3(-1, 0, 0);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 back => new Vector3(0, 0, -1);
        
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        
        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z);
        public Vector3 normalized => magnitude > 0 ? this / magnitude : zero;
        
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * Mathf.Clamp01(t);
    }

    // Vector2 structure
    [System.Serializable]
    public struct Vector2
    {
        public float x, y;
        
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public static Vector2 up => new Vector2(0, 1);
        public static Vector2 down => new Vector2(0, -1);
        public static Vector2 left => new Vector2(-1, 0);
        public static Vector2 right => new Vector2(1, 0);
        
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);
        
        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public Vector2 normalized => magnitude > 0 ? this / magnitude : zero;
        
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
    }

    // Quaternion structure
    [System.Serializable]
    public struct Quaternion
    {
        public float x, y, z, w;
        
        public Quaternion(float x, float y, float z, float w) 
        { this.x = x; this.y = y; this.z = z; this.w = w; }
        
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);
        
        public static Quaternion operator *(Quaternion lhs, Quaternion rhs) => identity;
        public static Vector3 operator *(Quaternion rotation, Vector3 point) => point;
        
        public static Quaternion Euler(float x, float y, float z) => identity;
        public static Quaternion Euler(Vector3 euler) => identity;
        public static Quaternion AngleAxis(float angle, Vector3 axis) => identity;
        public static Quaternion LookRotation(Vector3 forward) => identity;
        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards) => identity;
        public static Quaternion Lerp(Quaternion a, Quaternion b, float t) => identity;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => identity;
        
        public Vector3 eulerAngles => Vector3.zero;
    }

    // Color structure
    [System.Serializable]
    public struct Color
    {
        public float r, g, b, a;
        
        public Color(float r, float g, float b, float a = 1f) 
        { this.r = r; this.g = g; this.b = b; this.a = a; }
        
        public static Color white => new Color(1, 1, 1, 1);
        public static Color black => new Color(0, 0, 0, 1);
        public static Color red => new Color(1, 0, 0, 1);
        public static Color green => new Color(0, 1, 0, 1);
        public static Color blue => new Color(0, 0, 1, 1);
        public static Color yellow => new Color(1, 1, 0, 1);
        public static Color cyan => new Color(0, 1, 1, 1);
        public static Color magenta => new Color(1, 0, 1, 1);
        public static Color clear => new Color(0, 0, 0, 0);
        public static Color gray => new Color(0.5f, 0.5f, 0.5f, 1);
        
        public static Color Lerp(Color a, Color b, float t) => a;
    }

    // Common Unity attributes
    public class SerializeField : Attribute { }
    public class HideInInspector : Attribute { }
    public class Header : Attribute 
    { 
        public string header; 
        public Header(string header) { this.header = header; }
    }
    public class Tooltip : Attribute 
    { 
        public string tooltip; 
        public Tooltip(string tooltip) { this.tooltip = tooltip; }
    }
    public class Range : Attribute 
    { 
        public float min, max; 
        public Range(float min, float max) { this.min = min; this.max = max; }
    }
    public class ContextMenu : Attribute 
    { 
        public string menuName; 
        public ContextMenu(string menuName) { this.menuName = menuName; }
    }

    // Mathf utility class
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public const float Deg2Rad = PI / 180f;
        public const float Rad2Deg = 180f / PI;
        public const float Infinity = float.PositiveInfinity;
        public const float NegativeInfinity = float.NegativeInfinity;
        public const float Epsilon = float.Epsilon;
        
        public static float Abs(float f) => Math.Abs(f);
        public static int Abs(int value) => Math.Abs(value);
        public static float Acos(float f) => (float)Math.Acos(f);
        public static float Asin(float f) => (float)Math.Asin(f);
        public static float Atan(float f) => (float)Math.Atan(f);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Ceil(float f) => (float)Math.Ceiling(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        public static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
        public static float Clamp01(float value) => Clamp(value, 0f, 1f);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Floor(float f) => (float)Math.Floor(f);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
        public static float Round(float f) => (float)Math.Round(f);
        public static int RoundToInt(float f) => (int)Math.Round(f);
        public static float Sign(float f) => Math.Sign(f);
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Tan(float f) => (float)Math.Tan(f);
        
        public static bool Approximately(float a, float b) => Abs(b - a) < Max(1E-06f * Max(Abs(a), Abs(b)), Epsilon * 8f);
    }

    // Time class
    public static class Time
    {
        public static float time { get; set; } = 0f;
        public static float deltaTime => 0.016f; // 60 FPS
        public static float fixedDeltaTime => 0.02f; // 50 FPS
        public static float unscaledTime => 0f;
        public static float unscaledDeltaTime => 0.016f;
        public static float timeScale { get; set; } = 1f;
        public static int frameCount => 0;
        public static float realtimeSinceStartup => 0f;
    }

    // Debug class for logging
    public static class Debug
    {
        public static void Log(object message) => Console.WriteLine($"[LOG] {message}");
        public static void Log(object message, Object context) => Console.WriteLine($"[LOG] {message}");
        public static void LogWarning(object message) => Console.WriteLine($"[WARNING] {message}");
        public static void LogWarning(object message, Object context) => Console.WriteLine($"[WARNING] {message}");
        public static void LogError(object message) => Console.WriteLine($"[ERROR] {message}");
        public static void LogError(object message, Object context) => Console.WriteLine($"[ERROR] {message}");
        public static void LogException(Exception exception) => Console.WriteLine($"[EXCEPTION] {exception}");
        public static void LogException(Exception exception, Object context) => Console.WriteLine($"[EXCEPTION] {exception}");
    }

    // Coroutine class
    public class Coroutine { }
    
    // WaitForSeconds class
    public class WaitForSeconds
    {
        public float seconds;
        public WaitForSeconds(float seconds) { this.seconds = seconds; }
    }

    // WaitUntil class
    public class WaitUntil
    {
        public System.Func<bool> predicate;
        public WaitUntil(System.Func<bool> predicate) { this.predicate = predicate; }
    }

    // WaitForEndOfFrame class
    public class WaitForEndOfFrame { }

    // Resources class for loading assets
    public static class Resources
    {
        public static T Load<T>(string path) where T : Object => default(T);
        public static Object Load(string path) => null;
        public static Object Load(string path, Type systemTypeInstance) => null;
        public static T[] LoadAll<T>(string path) where T : Object => new T[0];
        public static Object[] LoadAll(string path) => new Object[0];
        public static Object[] LoadAll(string path, Type systemTypeInstance) => new Object[0];
        public static T GetBuiltinResource<T>(string path) where T : Object => default(T);
    }

    // JsonUtility class for JSON serialization
    public static class JsonUtility
    {
        public static string ToJson(object obj) => Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        public static T FromJson<T>(string json) => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        public static object FromJson(string json, Type type) => Newtonsoft.Json.JsonConvert.DeserializeObject(json, type);
    }

    // Random class
    public static class Random
    {
        private static System.Random _random = new System.Random();
        
        public static float Range(float min, float max) => (float)(_random.NextDouble() * (max - min) + min);
        public static int Range(int min, int max) => _random.Next(min, max);
        public static float value => (float)_random.NextDouble();
    }

    // ColorUtility class
    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string htmlString, out Color color)
        {
            color = Color.white;
            return true; // Simplified implementation
        }
        
        public static string ToHtmlStringRGB(Color color)
        {
            return $"#{(int)(color.r * 255):X2}{(int)(color.g * 255):X2}{(int)(color.b * 255):X2}";
        }
        
        public static string ToHtmlStringRGBA(Color color)
        {
            return $"#{(int)(color.r * 255):X2}{(int)(color.g * 255):X2}{(int)(color.b * 255):X2}{(int)(color.a * 255):X2}";
        }
    }

    // Application class
    public static class Application
    {
        public static string dataPath => "";
        public static string persistentDataPath => "";
        public static string streamingAssetsPath => "";
        public static string temporaryCachePath => "";
        public static RuntimePlatform platform => RuntimePlatform.LinuxPlayer;
        public static bool isPlaying => true;
        public static bool isEditor => false;
        public static string productName => "ScrapLine";
        public static string version => "1.0.0";
        
        public static void Quit() { }
        public static void Quit(int exitCode) { }
        
        public enum RuntimePlatform 
        { 
            OSXEditor, OSXPlayer, WindowsPlayer, WindowsEditor, IPhonePlayer, Android, 
            LinuxPlayer, LinuxEditor, WebGLPlayer, WSAPlayerX86, WSAPlayerX64, WSAPlayerARM 
        }
    }

    // PlayerPrefs class
    public static class PlayerPrefs
    {
        private static Dictionary<string, object> prefs = new Dictionary<string, object>();
        
        public static void SetInt(string key, int value) => prefs[key] = value;
        public static void SetFloat(string key, float value) => prefs[key] = value;
        public static void SetString(string key, string value) => prefs[key] = value;
        
        public static int GetInt(string key, int defaultValue = 0) => 
            prefs.TryGetValue(key, out var value) && value is int i ? i : defaultValue;
        public static float GetFloat(string key, float defaultValue = 0f) => 
            prefs.TryGetValue(key, out var value) && value is float f ? f : defaultValue;
        public static string GetString(string key, string defaultValue = "") => 
            prefs.TryGetValue(key, out var value) && value is string s ? s : defaultValue;
        
        public static bool HasKey(string key) => prefs.ContainsKey(key);
        public static void DeleteKey(string key) => prefs.Remove(key);
        public static void DeleteAll() => prefs.Clear();
        public static void Save() { }
    }

    // TextAsset class
    public class TextAsset : Object
    {
        public string text { get; set; }
        public byte[] bytes { get; set; }
    }

    // ScriptableObject class
    public class ScriptableObject : Object
    {
        protected ScriptableObject() { }
        
        public static T CreateInstance<T>() where T : ScriptableObject => System.Activator.CreateInstance<T>();
        public static ScriptableObject CreateInstance(Type type) => (ScriptableObject)System.Activator.CreateInstance(type);
    }

    // Material class
    public class Material : Object
    {
        public Color color { get; set; }
        public Texture mainTexture { get; set; }
        public new string name { get; set; }
        
        public Material() { }
        public Material(Shader shader) { }
        public Material(Material source) { }
        
        public void SetColor(string name, Color value) { }
        public void SetFloat(string name, float value) { }
        public void SetTexture(string name, Texture value) { }
        public void SetTextureOffset(string name, Vector2 value) { }
        public Color GetColor(string name) => Color.white;
        public float GetFloat(string name) => 0f;
        public Texture GetTexture(string name) => null;
        public Vector2 GetTextureOffset(string name) => Vector2.zero;
    }

    // Texture class
    public class Texture : Object
    {
        public int width { get; set; }
        public int height { get; set; }
    }

    // Texture2D class
    public class Texture2D : Texture
    {
        public Texture2D(int width, int height) { this.width = width; this.height = height; }
    }

    // Sprite class
    public class Sprite : Object
    {
        public Texture2D texture { get; set; }
        public Rect rect { get; set; }
        public Vector2 pivot { get; set; }
        
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot) => new Sprite();
    }

    // Rect structure
    [System.Serializable]
    public struct Rect
    {
        public float x, y, width, height;
        
        public Rect(float x, float y, float width, float height)
        {
            this.x = x; this.y = y; this.width = width; this.height = height;
        }
        
        public Vector2 position => new Vector2(x, y);
        public Vector2 size => new Vector2(width, height);
        public Vector2 center => new Vector2(x + width / 2, y + height / 2);
        
        public bool Contains(Vector2 point) => point.x >= x && point.x < x + width && point.y >= y && point.y < y + height;
        public bool Contains(Vector3 point) => Contains(new Vector2(point.x, point.y));
    }

    // Shader class
    public class Shader : Object
    {
        public static Shader Find(string name) => new Shader();
    }

    // Enums used throughout Unity
    public enum Space { Self, World }
    public enum SendMessageOptions { RequireReceiver, DontRequireReceiver }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum KeyCode 
    { 
        None, Space, Enter, Escape, LeftArrow, RightArrow, UpArrow, DownArrow,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Alpha0, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
        Mouse0, Mouse1, Mouse2, Mouse3, Mouse4, Mouse5, Mouse6
    }

    // RectTransformUtility class
    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera cam, out Vector2 localPoint)
        {
            localPoint = screenPoint;
            return true;
        }
        
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 screenPoint, Camera cam) => true;
    }

    // RaycastResult struct
    public struct RaycastResult
    {
        public GameObject gameObject;
        public Vector3 worldPosition;
        public Vector2 screenPosition;
        public float distance;
        public bool isValid;
    }

    // Input class
    public static class Input
    {
        public static Vector3 mousePosition => Vector3.zero;
        public static bool mousePresent => true;
        public static Vector2 mouseScrollDelta => Vector2.zero;
        
        public static bool GetKey(KeyCode key) => false;
        public static bool GetKeyDown(KeyCode key) => false;
        public static bool GetKeyUp(KeyCode key) => false;
        public static bool GetKey(string name) => false;
        public static bool GetKeyDown(string name) => false;
        public static bool GetKeyUp(string name) => false;
        
        public static bool GetMouseButton(int button) => false;
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButtonUp(int button) => false;
        
        public static float GetAxis(string axisName) => 0f;
        public static float GetAxisRaw(string axisName) => 0f;
        
        public static string inputString => "";
        public static bool anyKey => false;
        public static bool anyKeyDown => false;
    }

    // Canvas render mode enum (in UnityEngine namespace)  
    public enum RenderMode 
    { 
        ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace 
    }

    // Canvas class (in UnityEngine namespace)
    public class Canvas : Component
    {
        public RenderMode renderMode { get; set; } = RenderMode.ScreenSpaceOverlay;
        public Camera worldCamera { get; set; }
        public int sortingOrder { get; set; }
        public bool overrideSorting { get; set; }
    }

    // Color32 struct for UI components
    public struct Color32
    {
        public byte r, g, b, a;
        
        public Color32(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
        
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
        public static implicit operator Color32(Color c) => new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(c.a * 255));
    }

    // Cursor class for mouse cursor control
    public static class Cursor
    {
        public static bool visible { get; set; } = true;
        public static void SetCursor(Texture2D texture, Vector2 hotspot, CursorMode cursorMode) { }
    }

    // CursorMode enum
    public enum CursorMode
    {
        Auto,
        ForceSoftware
    }

    // Global functions
    public static class UnityGlobals
    {
        public static void DontDestroyOnLoad(Object obj) { }
    }

    // Unity global methods as static class  
    public static class Unity
    {
        public static void DontDestroyOnLoad(Object obj) => UnityGlobals.DontDestroyOnLoad(obj);
    }
}

// Global Unity methods - available without namespace outside the UnityEngine namespace
public static class GlobalUnityMethods
{
    public static void DontDestroyOnLoad(UnityEngine.Object obj) { }
}
