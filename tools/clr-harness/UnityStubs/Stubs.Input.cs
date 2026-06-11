// UnityEngine stub — Input: Input, KeyCode, Screen, Cursor, Touch
using System;

namespace UnityEngine
{
    // -------------------------------------------------------------------------
    // KeyCode (partial — includes all common keys)
    // -------------------------------------------------------------------------
    public enum KeyCode
    {
        None = 0,
        Backspace = 8, Delete = 127, Tab = 9, Clear = 12, Return = 13, Pause = 19, Escape = 27, Space = 32,
        Exclaim = 33, DoubleQuote = 34, Hash = 35, Dollar = 36, Percent = 37, Ampersand = 38, Quote = 39,
        LeftParen = 40, RightParen = 41, Asterisk = 42, Plus = 43, Comma = 44, Minus = 45, Period = 46, Slash = 47,
        Alpha0 = 48, Alpha1 = 49, Alpha2 = 50, Alpha3 = 51, Alpha4 = 52, Alpha5 = 53, Alpha6 = 54, Alpha7 = 55, Alpha8 = 56, Alpha9 = 57,
        Colon = 58, Semicolon = 59, Less = 60, Equals = 61, Greater = 62, Question = 63, At = 64,
        LeftBracket = 91, Backslash = 92, RightBracket = 93, Caret = 94, Underscore = 95, BackQuote = 96,
        A = 97, B = 98, C = 99, D = 100, E = 101, F = 102, G = 103, H = 104, I = 105, J = 106, K = 107,
        L = 108, M = 109, N = 110, O = 111, P = 112, Q = 113, R = 114, S = 115, T = 116, U = 117,
        V = 118, W = 119, X = 120, Y = 121, Z = 122,
        LeftCurlyBracket = 123, Pipe = 124, RightCurlyBracket = 125, Tilde = 126,
        Numlock = 300, CapsLock = 301, ScrollLock = 302, RightShift = 303, LeftShift = 304,
        RightControl = 305, LeftControl = 306, RightAlt = 307, LeftAlt = 308,
        RightCommand = 309, LeftCommand = 310, LeftWindows = 311, RightWindows = 312, AltGr = 313,
        Help = 315, Print = 316, SysReq = 317, Break = 318, Menu = 319,
        F1 = 282, F2 = 283, F3 = 284, F4 = 285, F5 = 286, F6 = 287, F7 = 288, F8 = 289, F9 = 290, F10 = 291, F11 = 292, F12 = 293,
        Insert = 277, Home = 278, End = 279, PageUp = 280, PageDown = 281,
        UpArrow = 273, DownArrow = 274, RightArrow = 275, LeftArrow = 276,
        Keypad0 = 256, Keypad1 = 257, Keypad2 = 258, Keypad3 = 259, Keypad4 = 260,
        Keypad5 = 261, Keypad6 = 262, Keypad7 = 263, Keypad8 = 264, Keypad9 = 265,
        KeypadPeriod = 266, KeypadDivide = 267, KeypadMultiply = 268, KeypadMinus = 269,
        KeypadPlus = 270, KeypadEnter = 271, KeypadEquals = 272,
        Mouse0 = 323, Mouse1 = 324, Mouse2 = 325, Mouse3 = 326, Mouse4 = 327, Mouse5 = 328, Mouse6 = 329,
        JoystickButton0 = 330, JoystickButton1 = 331, JoystickButton2 = 332, JoystickButton3 = 333,
        JoystickButton4 = 334, JoystickButton5 = 335, JoystickButton6 = 336, JoystickButton7 = 337,
        JoystickButton8 = 338, JoystickButton9 = 339, JoystickButton10 = 340, JoystickButton11 = 341,
        JoystickButton12 = 342, JoystickButton13 = 343, JoystickButton14 = 344, JoystickButton15 = 345,
        JoystickButton16 = 346, JoystickButton17 = 347, JoystickButton18 = 348, JoystickButton19 = 349
    }

    // -------------------------------------------------------------------------
    // Input (harness: always returns false/0 — use dependency injection in game code)
    // -------------------------------------------------------------------------
    public static partial class Input
    {
        public static Vector3 mousePosition { get; set; } = Vector3.zero;
        public static Vector2 mouseScrollDelta { get; set; } = Vector2.zero;
        public static bool mousePresent => false;

        public static bool GetKey(KeyCode key) => false;
        public static bool GetKey(string name) => false;
        public static bool GetKeyDown(KeyCode key) => false;
        public static bool GetKeyDown(string name) => false;
        public static bool GetKeyUp(KeyCode key) => false;
        public static bool GetKeyUp(string name) => false;

        public static bool GetMouseButton(int button) => false;
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButtonUp(int button) => false;

        public static float GetAxis(string axisName) => 0f;
        public static float GetAxisRaw(string axisName) => 0f;
        public static bool GetButton(string buttonName) => false;
        public static bool GetButtonDown(string buttonName) => false;
        public static bool GetButtonUp(string buttonName) => false;

        public static bool anyKey => false;
        public static bool anyKeyDown => false;
        public static string inputString => string.Empty;

        public static Touch[] touches => Array.Empty<Touch>();
        public static int touchCount => 0;
        public static bool touchSupported => false;
        public static bool multiTouchEnabled { get; set; }

        public static Touch GetTouch(int index) => default;
        public static bool simulateMouseWithTouches { get; set; } = true;

        public static string[] GetJoystickNames() => Array.Empty<string>();
        public static bool IsJoystickPreconfigured(string joystickName) => false;

        public static bool backButtonLeavesApp { get; set; }
        public static AccelerationEvent acceleration => default;
        public static AccelerationEvent[] accelerationEvents => Array.Empty<AccelerationEvent>();
        public static int accelerationEventCount => 0;
        public static AccelerationEvent GetAccelerationEvent(int index) => default;
        public static Gyroscope gyro { get; } = new Gyroscope();
        public static Compass compass { get; } = new Compass();
        public static LocationService location { get; } = new LocationService();
        public static DeviceOrientation deviceOrientation => DeviceOrientation.Unknown;
        public static IMECompositionMode imeCompositionMode { get; set; }
        public static string compositionString => string.Empty;
        public static Vector2 compositionCursorPos { get; set; }
        public static bool eatKeyPressOnTextFieldFocus { get; set; }
    }

    // -------------------------------------------------------------------------
    // Touch / Acceleration stubs
    // -------------------------------------------------------------------------
    public struct Touch
    {
        public int fingerId;
        public Vector2 position, deltaPosition, rawPosition;
        public float deltaTime;
        public int tapCount;
        public TouchPhase phase;
        public float pressure, maximumPossiblePressure;
        public bool isDirectTouch;
    }

    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }

    public struct AccelerationEvent
    {
        public Vector3 acceleration;
        public float deltaTime;
    }

    public class Gyroscope
    {
        public bool enabled { get; set; }
        public Vector3 gravity { get; set; }
        public Vector3 userAcceleration { get; set; }
        public Vector3 rotationRate { get; set; }
        public Vector3 rotationRateUnbiased { get; set; }
        public Quaternion attitude { get; set; } = Quaternion.identity;
        public float updateInterval { get; set; } = 0.1f;
    }

    public class Compass
    {
        public bool enabled { get; set; }
        public float magneticHeading { get; set; }
        public float trueHeading { get; set; }
        public float headingAccuracy { get; set; }
        public Vector3 rawVector { get; set; }
        public double timestamp { get; set; }
        public float updateInterval { get; set; } = 0.1f;
    }

    public class LocationService
    {
        public bool isEnabledByUser => false;
        public LocationServiceStatus status => LocationServiceStatus.Stopped;
        public LocationInfo lastData => default;
        public void Start() { }
        public void Stop() { }
    }

    public struct LocationInfo
    {
        public float latitude, longitude, altitude;
        public float horizontalAccuracy, verticalAccuracy;
        public double timestamp;
    }

    public enum LocationServiceStatus { Stopped, Initializing, Running, Failed }
    public enum DeviceOrientation { Unknown, Portrait, PortraitUpsideDown, LandscapeLeft, LandscapeRight, FaceUp, FaceDown }
    public enum IMECompositionMode { Auto, On, Off }

    // -------------------------------------------------------------------------
    // Screen
    // -------------------------------------------------------------------------
    public static partial class Screen
    {
        public static int width { get; set; } = 1920;
        public static int height { get; set; } = 1080;
        public static float dpi => 96f;
        public static FullScreenMode fullScreenMode { get; set; } = FullScreenMode.Windowed;
        public static bool fullScreen { get => fullScreenMode != FullScreenMode.Windowed; set { } }
        public static Resolution currentResolution => new Resolution { width = width, height = height, refreshRate = 60 };
        public static Resolution[] resolutions => new[] { currentResolution };
        public static bool sleepTimeout { get; set; }
        public static int sleepTimeoutValue { get; set; }
        public static ScreenOrientation orientation { get; set; } = ScreenOrientation.Landscape;
        public static bool autorotateToPortrait { get; set; }
        public static bool autorotateToPortraitUpsideDown { get; set; }
        public static bool autorotateToLandscapeLeft { get; set; } = true;
        public static bool autorotateToLandscapeRight { get; set; } = true;
        public static void SetResolution(int w, int h, bool fs) { width = w; height = h; }
        public static void SetResolution(int w, int h, FullScreenMode mode, int preferredRefreshRate = 0) { width = w; height = h; fullScreenMode = mode; }
    }

    public enum FullScreenMode { ExclusiveFullScreen, FullScreenWindow, MaximizedWindow, Windowed }
    public enum ScreenOrientation { Portrait, PortraitUpsideDown, LandscapeLeft, LandscapeRight, AutoRotation, Landscape }

    public struct Resolution
    {
        public int width, height, refreshRate;
        public override string ToString() => $"{width}x{height}@{refreshRate}Hz";
    }

    // -------------------------------------------------------------------------
    // Cursor
    // -------------------------------------------------------------------------
    public static partial class Cursor
    {
        public static bool visible { get; set; } = true;
        public static CursorLockMode lockState { get; set; }
        public static void SetCursor(Texture2D? texture, Vector2 hotspot, CursorMode cursorMode) { }
    }

    public enum CursorLockMode { None, Locked, Confined }
    public enum CursorMode { Auto, ForceSoftware }
}
