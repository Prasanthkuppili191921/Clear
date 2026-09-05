using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace AiInterviewAssistant
{
    public static class HotKeysRegister
    {
        // =========================================================
        // WINDOWS API - REGISTER HOTKEY
        // =========================================================

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(
            IntPtr hWnd,
            int id,
            uint fsModifiers,
            uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(
            IntPtr hWnd,
            int id);


        // =========================================================
        // WINDOWS API - KEY STATE
        // =========================================================

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(
            int vKey);


        // =========================================================
        // WINDOWS API - LOW LEVEL KEYBOARD HOOK
        // =========================================================

        [DllImport(
            "user32.dll",
            CharSet = CharSet.Auto,
            SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport(
            "user32.dll",
            CharSet = CharSet.Auto,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(
            IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport(
            "kernel32.dll",
            CharSet = CharSet.Auto,
            SetLastError = true)]
        private static extern IntPtr GetModuleHandle(
            string lpModuleName);


        // =========================================================
        // HOOK TYPES
        // =========================================================

        private const int WH_KEYBOARD_LL = 13;


        // =========================================================
        // KEYBOARD MESSAGES
        // =========================================================

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;


        // =========================================================
        // WM_HOTKEY
        // =========================================================

        private const int WM_HOTKEY = 0x0312;


        // =========================================================
        // MODIFIERS
        // =========================================================

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_NOREPEAT = 0x4000;


        // =========================================================
        // VIRTUAL KEYS
        // =========================================================

        private const uint VK_ESCAPE = 0x1B;
        private const uint VK_SPACE = 0x20;
        private const uint VK_RETURN = 0x0D;

        private const uint VK_LEFT = 0x25;
        private const uint VK_UP = 0x26;
        private const uint VK_RIGHT = 0x27;
        private const uint VK_DOWN = 0x28;

        private const uint VK_OEM_5 = 0xDC;
        private const uint VK_OEM_102 = 0xE2;

        private const uint VK_M = 0x4D;

        private const uint VK_S = 0x53;


        // =========================================================
        // CONTROL KEYS
        // =========================================================

        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;

        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;

        private const int VK_LMENU = 0xA4;
        private const int VK_RMENU = 0xA5;

        private const uint VK_BACK = 0x08;


        // =========================================================
        // HOTKEY IDS
        // =========================================================

        public const int TOGGLE_MAIN_WINDOW_HOTKEY_ID = 9100;

        public const int MOVE_LEFT_HOTKEY_ID = 9101;
        public const int MOVE_RIGHT_HOTKEY_ID = 9102;
        public const int MOVE_UP_HOTKEY_ID = 9103;
        public const int MOVE_DOWN_HOTKEY_ID = 9104;

        public const int SCROLL_UP_HOTKEY_ID = 9105;
        public const int SCROLL_DOWN_HOTKEY_ID = 9106;

        public const int ESCAPE_HOTKEY_ID = 9107;

        public const int VOICE_HOTKEY_ID = 9108;

        public const int VISION_HOTKEY_ID = 9109;
        public const int SEND_HOTKEY_ID = 9110;

        public const int SETTINGS_HOTKEY_ID = 9112;

        public const int MESSAGE_HOTKEY_ID = 9114;

        public const int SMART_ANSWER_HOTKEY_ID = 9115;

        public const int CLEAR_CHAT_HOTKEY_ID = 9116;


        // =========================================================
        // GLOBAL STATE
        // =========================================================

        private static IntPtr _hotkeyHandle =
            IntPtr.Zero;

        private static HwndSource _hwndSource;

        private static bool _registered;

        private static DateTime _lastEscapePressTime =
            DateTime.MinValue;

        private const int DOUBLE_ESCAPE_INTERVAL_MS =
            500;

        private static readonly List<int>
            _registeredHotkeyIds =
                new List<int>();


        // =========================================================
        // CALLBACKS
        // =========================================================

        private static Action _toggleMainWindow;

        private static Action _moveLeft;
        private static Action _moveRight;
        private static Action _moveUp;
        private static Action _moveDown;

        private static Action _scrollUp;
        private static Action _scrollDown;

        private static Action _escape;
        private static Action _voice;
        private static Action _vision;
        private static Action _send;

        private static Action _settings;

        private static Action _message;

        private static Action _smartAnswer;

        private static Action _clearChat;


        // =========================================================
        // LOW LEVEL KEYBOARD HOOK
        // =========================================================

        private static IntPtr _keyboardHook =
            IntPtr.Zero;

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        private static LowLevelKeyboardProc
            _keyboardProc;


        // =========================================================
        // REPEAT TIMER
        // =========================================================

        private static DispatcherTimer
            _repeatTimer;


        // =========================================================
        // ACTIVE REPEAT KEY
        // =========================================================

        private static int
            _activeRepeatKey = 0;

        private static bool
            _repeatKeyDown = false;


        // =========================================================
        // ACTIVE MODIFIER
        //
        // 1 = CTRL
        // 2 = ALT
        // =========================================================

        private static int
            _activeModifier = 0;


        // =========================================================
        // TIMING
        // =========================================================

        private const int REPEAT_INITIAL_DELAY =
            250;

        private const int REPEAT_INTERVAL =
            45;


        // =========================================================
        // REGISTER
        // =========================================================

        public static bool Register(
            MainWindow window,
            Action toggleMainWindow,
            Action moveLeft,
            Action moveRight,
            Action moveUp,
            Action moveDown,
            Action scrollUp,
            Action scrollDown,
            Action escape,
            Action voice,
            Action vision,
            Action send,
            Action settings,
            Action message,
            Action smartAnswer,
            Action clearChat)
        {
            try
            {
                if (window == null)
                    return false;

                if (!window.IsInitialized)
                    return false;


                // =================================================
                // REMOVE PREVIOUS REGISTRATION
                // =================================================

                Unregister();


                // =================================================
                // SAVE CALLBACKS
                // =================================================

                _toggleMainWindow =
                    toggleMainWindow;

                _moveLeft =
                    moveLeft;

                _moveRight =
                    moveRight;

                _moveUp =
                    moveUp;

                _moveDown =
                    moveDown;

                _scrollUp =
                    scrollUp;

                _scrollDown =
                    scrollDown;

                _escape =
                    escape;

                _voice =
                    voice;

                _vision =
                    vision;

                _send =
                    send;

                _settings =
                    settings;

                _message =
                    message;

                _smartAnswer =
                    smartAnswer;

                _clearChat =
                    clearChat;


                // =================================================
                // GET HWND
                // =================================================

                WindowInteropHelper helper =
                    new WindowInteropHelper(window);

                _hotkeyHandle =
                    helper.Handle;

                if (_hotkeyHandle == IntPtr.Zero)
                    return false;


                // =================================================
                // HWND SOURCE
                // =================================================

                _hwndSource =
                    HwndSource.FromHwnd(
                        _hotkeyHandle);

                if (_hwndSource == null)
                    return false;


                _hwndSource.AddHook(
                    HwndHook);


                // =================================================
                // CTRL + \
                // =================================================

                RegisterSingleHotkey(
                    TOGGLE_MAIN_WINDOW_HOTKEY_ID,
                    MOD_CONTROL |
                    MOD_NOREPEAT,
                    VK_OEM_5);


                // =================================================
                // CTRL + LEFT
                // =================================================

                RegisterSingleHotkey(
                    MOVE_LEFT_HOTKEY_ID,
                    MOD_CONTROL |
                    MOD_NOREPEAT,
                    VK_LEFT);


                // =================================================
                // CTRL + RIGHT
                // =================================================

                RegisterSingleHotkey(
                    MOVE_RIGHT_HOTKEY_ID,
                    MOD_CONTROL |
                    MOD_NOREPEAT,
                    VK_RIGHT);


                // =================================================
                // CTRL + UP
                // =================================================

                RegisterSingleHotkey(
                    MOVE_UP_HOTKEY_ID,
                    MOD_CONTROL |
                    MOD_NOREPEAT,
                    VK_UP);


                // =================================================
                // CTRL + DOWN
                // =================================================

                RegisterSingleHotkey(
                    MOVE_DOWN_HOTKEY_ID,
                    MOD_CONTROL |
                    MOD_NOREPEAT,
                    VK_DOWN);


                // =================================================
                // ALT + UP
                // =================================================

                RegisterSingleHotkey(
                    SCROLL_UP_HOTKEY_ID,
                    MOD_ALT |
                    MOD_NOREPEAT,
                    VK_UP);


                // =================================================
                // ALT + DOWN
                // =================================================

                RegisterSingleHotkey(
                    SCROLL_DOWN_HOTKEY_ID,
                    MOD_ALT |
                    MOD_NOREPEAT,
                    VK_DOWN);


                // =================================================
                // ESC
                // =================================================

                RegisterSingleHotkey(
                    ESCAPE_HOTKEY_ID,
                    MOD_NOREPEAT,
                    VK_ESCAPE);


                // =================================================
                // CTRL + SPACE
                // =================================================

                RegisterSingleHotkey(
                    VOICE_HOTKEY_ID,
                    MOD_CONTROL |
                    MOD_NOREPEAT,
                    VK_SPACE);


                // =================================================
                // ALT + ENTER
                // =================================================

                RegisterSingleHotkey(
                    VISION_HOTKEY_ID,
                    MOD_ALT |
                    MOD_NOREPEAT,
                    VK_RETURN);


                // =================================================
                // CTRL + ENTER
                // =================================================

                RegisterSingleHotkey(
                    SEND_HOTKEY_ID,
                    MOD_CONTROL |
                    MOD_NOREPEAT,
                    VK_RETURN);


                // =================================================
                // CTRL + SHIFT + \
                // =================================================

                RegisterSingleHotkey(
                    SETTINGS_HOTKEY_ID,
                    MOD_CONTROL |
                    MOD_SHIFT |
                    MOD_NOREPEAT,
                    VK_OEM_5);


                // =================================================
                // CTRL + M
                // =================================================

                RegisterSingleHotkey(
                    MESSAGE_HOTKEY_ID,
                    MOD_CONTROL |
                    MOD_NOREPEAT,
                    VK_M);

                // =================================================
                // CTRL + SHIFT + S
                // SMART ANSWER ON / OFF
                // =================================================

                RegisterSingleHotkey(
                    SMART_ANSWER_HOTKEY_ID,
                    MOD_CONTROL |
                    MOD_SHIFT |
                    MOD_NOREPEAT,
                    VK_S);

                // =================================================
                // CTRL + SHIFT + BACKSPACE
                // CLEAR CHAT
                // =================================================

                RegisterSingleHotkey(
                    CLEAR_CHAT_HOTKEY_ID,
                    MOD_CONTROL |
                    MOD_SHIFT |
                    MOD_NOREPEAT,
                    VK_BACK);


                // =================================================
                // CREATE REPEAT TIMER
                // =================================================

                CreateRepeatTimer();


                // =================================================
                // INSTALL LOW LEVEL KEYBOARD HOOK
                // =================================================

                InstallKeyboardHook();


                // =================================================
                // REGISTERED
                // =================================================

                _registered = true;

                return true;
            }
            catch
            {
                Unregister();

                return false;
            }
        }


        // =========================================================
        // REGISTER SINGLE HOTKEY
        // =========================================================

        private static bool RegisterSingleHotkey(
            int id,
            uint modifiers,
            uint virtualKey)
        {
            try
            {
                if (_hotkeyHandle == IntPtr.Zero)
                    return false;


                bool result =
                    RegisterHotKey(
                        _hotkeyHandle,
                        id,
                        modifiers,
                        virtualKey);


                if (result)
                {
                    _registeredHotkeyIds.Add(id);
                }


                return result;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // CREATE REPEAT TIMER
        // =========================================================

        private static void CreateRepeatTimer()
        {
            try
            {
                StopRepeatTimer();


                _repeatTimer =
                    new DispatcherTimer(
                        DispatcherPriority.Input);


                _repeatTimer.Interval =
                    TimeSpan.FromMilliseconds(
                        REPEAT_INITIAL_DELAY);


                _repeatTimer.Tick +=
                    RepeatTimer_Tick;
            }
            catch
            {
            }
        }


        // =========================================================
        // INSTALL KEYBOARD HOOK
        // =========================================================

        private static void InstallKeyboardHook()
        {
            try
            {
                if (_keyboardHook != IntPtr.Zero)
                    return;


                _keyboardProc =
                    KeyboardHookCallback;


                using (
                    Process process =
                        Process.GetCurrentProcess())
                {
                    using (
                        ProcessModule module =
                            process.MainModule)
                    {
                        if (module == null)
                            return;


                        IntPtr moduleHandle =
                            GetModuleHandle(
                                module.ModuleName);


                        _keyboardHook =
                            SetWindowsHookEx(
                                WH_KEYBOARD_LL,
                                _keyboardProc,
                                moduleHandle,
                                0);
                    }
                }
            }
            catch
            {
                _keyboardHook =
                    IntPtr.Zero;
            }
        }


        // =========================================================
        // KEYBOARD HOOK CALLBACK
        // =========================================================

        private static IntPtr KeyboardHookCallback(
            int nCode,
            IntPtr wParam,
            IntPtr lParam)
        {
            try
            {
                if (nCode < 0)
                {
                    return CallNextHookEx(
                        _keyboardHook,
                        nCode,
                        wParam,
                        lParam);
                }


                int message =
                    wParam.ToInt32();


                int virtualKey =
                    Marshal.ReadInt32(lParam);


                // =================================================
                // KEY DOWN
                // =================================================

                if (message == WM_KEYDOWN ||
                    message == WM_SYSKEYDOWN)
                {
                    // ---------------------------------------------
                    // CTRL + \
                    // CTRL + SHIFT + \
                    // ---------------------------------------------

                    if ((virtualKey == (int)VK_OEM_5 ||
                         virtualKey == (int)VK_OEM_102) &&
                        IsControlPressed())
                    {
                        bool shiftPressed =
                            IsShiftPressed();


                        if (shiftPressed)
                        {
                            Execute(
                                _settings);
                        }
                        else
                        {
                            Execute(
                                _toggleMainWindow);
                        }


                        return (IntPtr)1;
                    }


                    // ---------------------------------------------
                    // CTRL + ARROW
                    // ALT + ARROW
                    // ---------------------------------------------

                    HandleRepeatKeyDown(
                        virtualKey);
                }


                // =================================================
                // KEY UP
                // =================================================

                else if (
                    message == WM_KEYUP ||
                    message == WM_SYSKEYUP)
                {
                    HandleRepeatKeyUp(
                        virtualKey);


                    // ---------------------------------------------
                    // CTRL + \ KEY UP
                    // ---------------------------------------------

                    if ((virtualKey == (int)VK_OEM_5 ||
                         virtualKey == (int)VK_OEM_102) &&
                        IsControlPressed())
                    {
                        return (IntPtr)1;
                    }
                }
            }
            catch
            {
            }


            // =====================================================
            // PASS EVERYTHING ELSE
            // =====================================================

            return CallNextHookEx(
                _keyboardHook,
                nCode,
                wParam,
                lParam);
        }


        // =========================================================
        // HANDLE REPEAT KEY DOWN
        // =========================================================

        private static void HandleRepeatKeyDown(
            int virtualKey)
        {
            try
            {
                int keyId =
                    GetArrowActionId(
                        virtualKey);


                if (keyId == 0)
                    return;


                int modifier;


                // =================================================
                // CTRL + ARROW = MOVEMENT
                // =================================================

                if (IsControlPressed())
                {
                    modifier = 1;
                }


                // =================================================
                // ALT + ARROW = SCROLL
                // =================================================

                else if (IsAltPressed())
                {
                    modifier = 2;
                }
                else
                {
                    return;
                }


                // =================================================
                // SAME KEY ALREADY ACTIVE
                // =================================================

                if (_repeatKeyDown &&
                    _activeRepeatKey == keyId &&
                    _activeModifier == modifier)
                {
                    return;
                }


                // =================================================
                // START NEW REPEAT
                // =================================================

                StopRepeatTimer();


                _repeatKeyDown =
                    true;

                _activeRepeatKey =
                    keyId;

                _activeModifier =
                    modifier;


                if (_repeatTimer == null)
                {
                    CreateRepeatTimer();
                }


                _repeatTimer.Interval =
                    TimeSpan.FromMilliseconds(
                        REPEAT_INITIAL_DELAY);


                _repeatTimer.Start();
            }
            catch
            {
            }
        }


        // =========================================================
        // HANDLE REPEAT KEY UP
        // =========================================================

        private static void HandleRepeatKeyUp(
            int virtualKey)
        {
            try
            {
                int keyId =
                    GetArrowActionId(
                        virtualKey);


                if (keyId != 0 &&
                    keyId ==
                    _activeRepeatKey)
                {
                    StopRepeatTimer();

                    _repeatKeyDown =
                        false;

                    _activeModifier =
                        0;

                    return;
                }


                // =================================================
                // CTRL RELEASE
                // =================================================

                if (virtualKey ==
                        VK_LCONTROL ||
                    virtualKey ==
                        VK_RCONTROL)
                {
                    if (_activeModifier == 1)
                    {
                        StopRepeatTimer();

                        _repeatKeyDown =
                            false;

                        _activeModifier =
                            0;
                    }

                    return;
                }


                // =================================================
                // ALT RELEASE
                // =================================================

                if (virtualKey ==
                        VK_LMENU ||
                    virtualKey ==
                        VK_RMENU)
                {
                    if (_activeModifier == 2)
                    {
                        StopRepeatTimer();

                        _repeatKeyDown =
                            false;

                        _activeModifier =
                            0;
                    }
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // REPEAT TIMER
        // =========================================================

        private static void RepeatTimer_Tick(
            object sender,
            EventArgs e)
        {
            try
            {
                if (!_repeatKeyDown)
                {
                    StopRepeatTimer();
                    return;
                }


                if (_activeRepeatKey == 0)
                {
                    StopRepeatTimer();
                    return;
                }


                // =================================================
                // CTRL MOVEMENT
                // =================================================

                if (_activeModifier == 1)
                {
                    if (!IsControlPressed())
                    {
                        StopRepeatTimer();

                        _repeatKeyDown =
                            false;

                        _activeModifier =
                            0;

                        return;
                    }
                }


                // =================================================
                // ALT SCROLL
                // =================================================

                else if (_activeModifier == 2)
                {
                    if (!IsAltPressed())
                    {
                        StopRepeatTimer();

                        _repeatKeyDown =
                            false;

                        _activeModifier =
                            0;

                        return;
                    }
                }


                // =================================================
                // CHECK ARROW STILL PRESSED
                // =================================================

                int virtualKey =
                    GetVirtualKeyFromActionId(
                        _activeRepeatKey);


                if (virtualKey == 0)
                {
                    StopRepeatTimer();

                    _repeatKeyDown =
                        false;

                    return;
                }


                if (!IsKeyPressed(
                    virtualKey))
                {
                    StopRepeatTimer();

                    _repeatKeyDown =
                        false;

                    _activeModifier =
                        0;

                    return;
                }


                // =================================================
                // EXECUTE
                // =================================================

                if (_activeModifier == 1)
                {
                    ExecuteMovement(
                        _activeRepeatKey);
                }
                else if (_activeModifier == 2)
                {
                    ExecuteScroll(
                        _activeRepeatKey);
                }


                // =================================================
                // FAST REPEAT
                // =================================================

                _repeatTimer.Interval =
                    TimeSpan.FromMilliseconds(
                        REPEAT_INTERVAL);
            }
            catch
            {
                StopRepeatTimer();

                _repeatKeyDown =
                    false;

                _activeModifier =
                    0;
            }
        }


        // =========================================================
        // EXECUTE MOVEMENT
        // =========================================================

        private static void ExecuteMovement(
            int movementKey)
        {
            try
            {
                switch (movementKey)
                {
                    case MOVE_LEFT_HOTKEY_ID:

                        Execute(
                            _moveLeft);

                        break;


                    case MOVE_RIGHT_HOTKEY_ID:

                        Execute(
                            _moveRight);

                        break;


                    case MOVE_UP_HOTKEY_ID:

                        Execute(
                            _moveUp);

                        break;


                    case MOVE_DOWN_HOTKEY_ID:

                        Execute(
                            _moveDown);

                        break;
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // EXECUTE SCROLL
        // =========================================================

        private static void ExecuteScroll(
            int scrollKey)
        {
            try
            {
                switch (scrollKey)
                {
                    case SCROLL_UP_HOTKEY_ID:

                        Execute(
                            _scrollUp);

                        break;


                    case SCROLL_DOWN_HOTKEY_ID:

                        Execute(
                            _scrollDown);

                        break;
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // GET ARROW ACTION ID
        // =========================================================

        private static int GetArrowActionId(
            int virtualKey)
        {
            switch (virtualKey)
            {
                case (int)VK_LEFT:

                    if (IsControlPressed())
                        return MOVE_LEFT_HOTKEY_ID;

                    if (IsAltPressed())
                        return SCROLL_UP_HOTKEY_ID;

                    return 0;


                case (int)VK_RIGHT:

                    if (IsControlPressed())
                        return MOVE_RIGHT_HOTKEY_ID;

                    if (IsAltPressed())
                        return SCROLL_DOWN_HOTKEY_ID;

                    return 0;


                case (int)VK_UP:

                    if (IsControlPressed())
                        return MOVE_UP_HOTKEY_ID;

                    if (IsAltPressed())
                        return SCROLL_UP_HOTKEY_ID;

                    return 0;


                case (int)VK_DOWN:

                    if (IsControlPressed())
                        return MOVE_DOWN_HOTKEY_ID;

                    if (IsAltPressed())
                        return SCROLL_DOWN_HOTKEY_ID;

                    return 0;


                default:
                    return 0;
            }
        }


        // =========================================================
        // GET VIRTUAL KEY
        // =========================================================

        private static int GetVirtualKeyFromActionId(
            int actionId)
        {
            switch (actionId)
            {
                case MOVE_LEFT_HOTKEY_ID:
                    return (int)VK_LEFT;

                case MOVE_RIGHT_HOTKEY_ID:
                    return (int)VK_RIGHT;

                case MOVE_UP_HOTKEY_ID:
                    return (int)VK_UP;

                case MOVE_DOWN_HOTKEY_ID:
                    return (int)VK_DOWN;

                case SCROLL_UP_HOTKEY_ID:
                    return (int)VK_UP;

                case SCROLL_DOWN_HOTKEY_ID:
                    return (int)VK_DOWN;

                default:
                    return 0;
            }
        }


        // =========================================================
        // CHECK CTRL
        // =========================================================

        private static bool IsControlPressed()
        {
            try
            {
                bool left =
                    (GetAsyncKeyState(
                        VK_LCONTROL) &
                        0x8000) != 0;


                bool right =
                    (GetAsyncKeyState(
                        VK_RCONTROL) &
                        0x8000) != 0;


                return left || right;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // CHECK ALT
        // =========================================================

        private static bool IsAltPressed()
        {
            try
            {
                bool left =
                    (GetAsyncKeyState(
                        VK_LMENU) &
                        0x8000) != 0;


                bool right =
                    (GetAsyncKeyState(
                        VK_RMENU) &
                        0x8000) != 0;


                return left || right;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // CHECK SHIFT
        // =========================================================

        private static bool IsShiftPressed()
        {
            try
            {
                bool left =
                    (GetAsyncKeyState(
                        VK_LSHIFT) &
                        0x8000) != 0;


                bool right =
                    (GetAsyncKeyState(
                        VK_RSHIFT) &
                        0x8000) != 0;


                return left || right;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // CHECK KEY
        // =========================================================

        private static bool IsKeyPressed(
            int virtualKey)
        {
            try
            {
                return
                    (GetAsyncKeyState(
                        virtualKey) &
                        0x8000) != 0;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // STOP REPEAT TIMER
        // =========================================================

        private static void StopRepeatTimer()
        {
            try
            {
                if (_repeatTimer != null)
                {
                    _repeatTimer.Stop();
                }
            }
            catch
            {
            }


            _activeRepeatKey =
                0;

            _activeModifier =
                0;
        }


        // =========================================================
        // HWND HOOK
        //
        // SINGLE PRESS HOTKEYS ARE PRESERVED HERE.
        // =========================================================

        private static IntPtr HwndHook(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            try
            {
                if (msg != WM_HOTKEY)
                    return IntPtr.Zero;


                int hotkeyId =
                    wParam.ToInt32();


                switch (hotkeyId)
                {
                    // =============================================
                    // CTRL + \
                    // =============================================

                    case TOGGLE_MAIN_WINDOW_HOTKEY_ID:

                        Execute(
                            _toggleMainWindow);

                        handled = true;

                        break;


                    // =============================================
                    // CTRL + LEFT
                    // =============================================

                    case MOVE_LEFT_HOTKEY_ID:

                        Execute(
                            _moveLeft);

                        handled = true;

                        break;


                    // =============================================
                    // CTRL + RIGHT
                    // =============================================

                    case MOVE_RIGHT_HOTKEY_ID:

                        Execute(
                            _moveRight);

                        handled = true;

                        break;


                    // =============================================
                    // CTRL + UP
                    // =============================================

                    case MOVE_UP_HOTKEY_ID:

                        Execute(
                            _moveUp);

                        handled = true;

                        break;


                    // =============================================
                    // CTRL + DOWN
                    // =============================================

                    case MOVE_DOWN_HOTKEY_ID:

                        Execute(
                            _moveDown);

                        handled = true;

                        break;


                    // =============================================
                    // ALT + UP
                    // =============================================

                    case SCROLL_UP_HOTKEY_ID:

                        Execute(
                            _scrollUp);

                        handled = true;

                        break;


                    // =============================================
                    // ALT + DOWN
                    // =============================================

                    case SCROLL_DOWN_HOTKEY_ID:

                        Execute(
                            _scrollDown);

                        handled = true;

                        break;


                    // =============================================
                    // ESC
                    // =============================================

                    case ESCAPE_HOTKEY_ID:

                        HandleEscapeDoublePress();

                        handled = true;

                        break;


                    // =============================================
                    // CTRL + SPACE
                    // =============================================

                    case VOICE_HOTKEY_ID:

                        Execute(
                            _voice);

                        handled = true;

                        break;


                    // =============================================
                    // ALT + ENTER
                    // =============================================

                    case VISION_HOTKEY_ID:

                        Execute(
                            _vision);

                        handled = true;

                        break;


                    // =============================================
                    // CTRL + ENTER
                    // =============================================

                    case SEND_HOTKEY_ID:

                        Execute(
                            _send);

                        handled = true;

                        break;


                    // =============================================
                    // CTRL + SHIFT + \
                    // =============================================

                    case SETTINGS_HOTKEY_ID:

                        Execute(
                            _settings);

                        handled = true;

                        break;


                    // =============================================
                    // CTRL + M
                    // =============================================

                    case MESSAGE_HOTKEY_ID:

                        Execute(
                            _message);

                        handled = true;

                        break;

                    // =============================================
                    // CTRL + SHIFT + S
                    // SMART ANSWER ON / OFF
                    // =============================================

                    case SMART_ANSWER_HOTKEY_ID:

                        Execute(
                            _smartAnswer);

                        handled = true;

                        break;

                    // =============================================
                    // CTRL + SHIFT + BACKSPACE
                    // CLEAR CHAT
                    // =============================================

                    case CLEAR_CHAT_HOTKEY_ID:

                        Execute(
                            _clearChat);

                        handled = true;

                        break;
                }
            }
            catch
            {
            }


            return IntPtr.Zero;
        }

        // =========================================================
        // HANDLE DOUBLE ESCAPE
        // =========================================================

        private static void HandleEscapeDoublePress()
        {
            try
            {
                DateTime now =
                    DateTime.UtcNow;

                if ((now - _lastEscapePressTime).TotalMilliseconds
                    <= DOUBLE_ESCAPE_INTERVAL_MS)
                {
                    _lastEscapePressTime =
                        DateTime.MinValue;

                    Execute(
                        _escape);

                    return;
                }

                _lastEscapePressTime =
                    now;
            }
            catch
            {
            }
        }

        // =========================================================
        // EXECUTE CALLBACK
        // =========================================================

        private static void Execute(
            Action action)
        {
            try
            {
                if (action == null)
                    return;


                action();
            }
            catch
            {
            }
        }


        // =========================================================
        // UNREGISTER
        // =========================================================

        public static void Unregister()
        {
            try
            {
                // =================================================
                // STOP REPEAT
                // =================================================

                StopRepeatTimer();

                _repeatKeyDown =
                    false;

                _activeModifier =
                    0;


                // =================================================
                // REMOVE KEYBOARD HOOK
                // =================================================

                if (_keyboardHook != IntPtr.Zero)
                {
                    try
                    {
                        UnhookWindowsHookEx(
                            _keyboardHook);
                    }
                    catch
                    {
                    }
                }


                _keyboardHook =
                    IntPtr.Zero;

                _keyboardProc =
                    null;


                // =================================================
                // UNREGISTER HOTKEYS
                // =================================================

                if (_hotkeyHandle != IntPtr.Zero)
                {
                    foreach (
                        int id
                        in _registeredHotkeyIds)
                    {
                        try
                        {
                            UnregisterHotKey(
                                _hotkeyHandle,
                                id);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                _registeredHotkeyIds.Clear();


                // =================================================
                // REMOVE HWND HOOK
                // =================================================

                if (_hwndSource != null)
                {
                    try
                    {
                        _hwndSource.RemoveHook(
                            HwndHook);
                    }
                    catch
                    {
                    }
                }


                _hwndSource =
                    null;


                _hotkeyHandle =
                    IntPtr.Zero;


                // =================================================
                // CLEAR CALLBACKS
                // =================================================

                _toggleMainWindow =
                    null;

                _moveLeft =
                    null;

                _moveRight =
                    null;

                _moveUp =
                    null;

                _moveDown =
                    null;

                _scrollUp =
                    null;

                _scrollDown =
                    null;

                _escape =
                    null;

                _voice =
                    null;

                _vision =
                    null;

                _send =
                    null;

                _settings =
                    null;

                _message =
                    null;

                _smartAnswer =
                    null;

                _clearChat =
                    null;


                // =================================================
                // CLEAR STATE
                // =================================================

                _activeRepeatKey =
                    0;

                _activeModifier =
                    0;

                _repeatKeyDown =
                    false;

                _lastEscapePressTime =
                   DateTime.MinValue;


                _registered =
                    false;
            }
        }


        // =========================================================
        // REGISTERED
        // =========================================================

        public static bool IsRegistered
        {
            get
            {
                return _registered;
            }
        }
    }
}