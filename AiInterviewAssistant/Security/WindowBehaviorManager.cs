using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace AiInterviewAssistant
{
    public static class WindowBehaviorManager
    {
        // =========================================================
        // WINDOWS STYLE
        // =========================================================

        private const int GWL_STYLE = -16;

        private const long WS_MAXIMIZEBOX = 0x00010000L;
        private const long WS_THICKFRAME = 0x00040000L;

        // =========================================================
        // WINDOW MOVING
        // =========================================================

        private const int WM_MOVING = 0x0216;

        // =========================================================
        // VIRTUAL SCREEN
        // =========================================================

        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        // =========================================================
        // MOVE SETTINGS
        // =========================================================

        private const double WINDOW_MOVE_DISTANCE = 50.0;

        private const double WINDOW_MOVE_DURATION = 0.30;

        // =========================================================
        // SMOOTH MOVE STATE
        // =========================================================

        private static bool _moveAnimating;

        private static MainWindow _movingWindow;

        private static double _moveStartLeft;
        private static double _moveStartTop;

        private static double _moveTargetLeft;
        private static double _moveTargetTop;

        private static DateTime _moveStartTime;

        // =========================================================
        // RECT
        // =========================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // =========================================================
        // NATIVE METHODS
        // =========================================================

        [DllImport(
            "user32.dll",
            EntryPoint = "GetWindowLongPtr",
            SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(
            IntPtr hWnd,
            int nIndex);

        [DllImport(
            "user32.dll",
            EntryPoint = "SetWindowLongPtr",
            SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(
            IntPtr hWnd,
            int nIndex,
            IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(
            int nIndex);

        // =========================================================
        // WINDOW POSITION FLAGS
        // =========================================================

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        // =========================================================
        // ATTACH
        // =========================================================

        public static void Attach(Window window)
        {
            if (window == null)
                return;

            try
            {
                HwndSource source =
                    PresentationSource.FromVisual(window)
                    as HwndSource;

                if (source == null)
                    return;

                IntPtr hwnd =
                    source.Handle;

                if (hwnd == IntPtr.Zero)
                    return;

                DisableSnap(hwnd);

                source.AddHook(
                    WindowHook);
            }
            catch
            {
                // Window behavior must never
                // break the application.
            }
        }

        // =========================================================
        // DISABLE SNAP / MAXIMIZE
        // =========================================================

        private static void DisableSnap(
            IntPtr hwnd)
        {
            try
            {
                long style =
                    GetWindowLongPtr(
                        hwnd,
                        GWL_STYLE).ToInt64();

                style &= ~WS_MAXIMIZEBOX;
                style &= ~WS_THICKFRAME;

                SetWindowLongPtr(
                    hwnd,
                    GWL_STYLE,
                    new IntPtr(style));

                SetWindowPos(
                    hwnd,
                    IntPtr.Zero,
                    0,
                    0,
                    0,
                    0,
                    SWP_NOSIZE |
                    SWP_NOMOVE |
                    SWP_NOZORDER |
                    SWP_NOACTIVATE |
                    SWP_FRAMECHANGED);
            }
            catch
            {
            }
        }

        // =========================================================
        // WINDOW HOOK
        // =========================================================

        private static IntPtr WindowHook(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            try
            {
                if (msg == WM_MOVING &&
                    lParam != IntPtr.Zero)
                {
                    ClampWindowToScreen(lParam);

                    handled = false;
                }
            }
            catch
            {
            }

            return IntPtr.Zero;
        }

        // =========================================================
        // KEEP COMPLETE WINDOW INSIDE SCREEN
        // =========================================================

        private static void ClampWindowToScreen(
            IntPtr lParam)
        {
            RECT rect =
                Marshal.PtrToStructure<RECT>(
                    lParam);

            int screenLeft =
                GetSystemMetrics(
                    SM_XVIRTUALSCREEN);

            int screenTop =
                GetSystemMetrics(
                    SM_YVIRTUALSCREEN);

            int screenWidth =
                GetSystemMetrics(
                    SM_CXVIRTUALSCREEN);

            int screenHeight =
                GetSystemMetrics(
                    SM_CYVIRTUALSCREEN);

            int screenRight =
                screenLeft +
                screenWidth;

            int screenBottom =
                screenTop +
                screenHeight;

            int width =
                rect.Right -
                rect.Left;

            int height =
                rect.Bottom -
                rect.Top;

            if (width <= 0 ||
                height <= 0)
                return;

            if (rect.Left < screenLeft)
            {
                rect.Left =
                    screenLeft;

                rect.Right =
                    rect.Left +
                    width;
            }

            if (rect.Right > screenRight)
            {
                rect.Right =
                    screenRight;

                rect.Left =
                    rect.Right -
                    width;
            }

            if (rect.Top < screenTop)
            {
                rect.Top =
                    screenTop;

                rect.Bottom =
                    rect.Top +
                    height;
            }

            if (rect.Bottom > screenBottom)
            {
                rect.Bottom =
                    screenBottom;

                rect.Top =
                    rect.Bottom -
                    height;
            }

            if (rect.Left < screenLeft)
            {
                rect.Left =
                    screenLeft;

                rect.Right =
                    screenLeft +
                    width;
            }

            if (rect.Right > screenRight)
            {
                rect.Right =
                    screenRight;

                rect.Left =
                    screenRight -
                    width;
            }

            if (rect.Top < screenTop)
            {
                rect.Top =
                    screenTop;

                rect.Bottom =
                    screenTop +
                    height;
            }

            if (rect.Bottom > screenBottom)
            {
                rect.Bottom =
                    screenBottom;

                rect.Top =
                    screenBottom -
                    height;
            }

            Marshal.StructureToPtr(
                rect,
                lParam,
                false);
        }

        // =========================================================
        // MOVE MAIN WINDOW
        //
        // Ctrl + Left / Right / Up / Down
        //
        // MainWindow ONLY
        // =========================================================

        public static void MoveMainWindow(
            MainWindow window,
            int hotkeyId)
        {
            try
            {
                if (window == null ||
                    !window.IsVisible)
                    return;

                if (window.WindowState ==
                    WindowState.Minimized)
                    return;

                double windowWidth =
                    window.ActualWidth;

                double windowHeight =
                    window.ActualHeight;

                if (double.IsNaN(windowWidth) ||
                    windowWidth <= 0)
                {
                    windowWidth =
                        window.Width;
                }

                if (double.IsNaN(windowHeight) ||
                    windowHeight <= 0)
                {
                    windowHeight =
                        window.Height;
                }

                double currentLeft =
                    window.Left;

                double currentTop =
                    window.Top;

                if (double.IsNaN(currentLeft))
                    currentLeft = 0;

                if (double.IsNaN(currentTop))
                    currentTop = 0;

                // =================================================
                // IMPORTANT:
                // If an animation is already running, use the
                // previous target as the starting point.
                //
                // This makes holding Ctrl + Arrow smooth.
                // =================================================

                double baseLeft =
                    (_moveAnimating &&
                     _movingWindow == window)
                    ? _moveTargetLeft
                    : currentLeft;

                double baseTop =
                    (_moveAnimating &&
                     _movingWindow == window)
                    ? _moveTargetTop
                    : currentTop;

                double screenLeft =
                    SystemParameters.WorkArea.Left;

                double screenTop =
                    SystemParameters.WorkArea.Top;

                double screenRight =
                    SystemParameters.WorkArea.Right;

                double screenBottom =
                    SystemParameters.WorkArea.Bottom;

                double targetLeft =
                    baseLeft;

                double targetTop =
                    baseTop;

                // =================================================
                // HOTKEY
                // =================================================

                switch (hotkeyId)
                {
                    case HotKeysRegister.MOVE_LEFT_HOTKEY_ID:

                        targetLeft -=
                            WINDOW_MOVE_DISTANCE;

                        break;

                    case HotKeysRegister.MOVE_RIGHT_HOTKEY_ID:

                        targetLeft +=
                            WINDOW_MOVE_DISTANCE;

                        break;

                    case HotKeysRegister.MOVE_UP_HOTKEY_ID:

                        targetTop -=
                            WINDOW_MOVE_DISTANCE;

                        break;

                    case HotKeysRegister.MOVE_DOWN_HOTKEY_ID:

                        targetTop +=
                            WINDOW_MOVE_DISTANCE;

                        break;

                    default:

                        return;
                }

                // =================================================
                // SCREEN BOUNDARIES
                // =================================================

                double maxLeft =
                    screenRight -
                    windowWidth;

                double maxTop =
                    screenBottom -
                    windowHeight;

                if (maxLeft < screenLeft)
                    maxLeft = screenLeft;

                if (maxTop < screenTop)
                    maxTop = screenTop;

                targetLeft =
                    Math.Max(
                        screenLeft,
                        Math.Min(
                            targetLeft,
                            maxLeft));

                targetTop =
                    Math.Max(
                        screenTop,
                        Math.Min(
                            targetTop,
                            maxTop));

                // =================================================
                // NOTHING TO MOVE
                // =================================================

                if (Math.Abs(
                        targetLeft -
                        currentLeft) < 0.5 &&
                    Math.Abs(
                        targetTop -
                        currentTop) < 0.5 &&
                    !_moveAnimating)
                {
                    return;
                }

                // =================================================
                // START / UPDATE SMOOTH MOVE
                // =================================================

                StartSmoothMove(
                    window,
                    targetLeft,
                    targetTop);
            }
            catch
            {
            }
        }

        // =========================================================
        // START SMOOTH MOVE
        // =========================================================

        private static void StartSmoothMove(
            MainWindow window,
            double targetLeft,
            double targetTop)
        {
            try
            {
                if (window == null)
                    return;

                if (!window.Dispatcher.CheckAccess())
                {
                    window.Dispatcher.BeginInvoke(
                        new Action(
                            () =>
                            {
                                StartSmoothMove(
                                    window,
                                    targetLeft,
                                    targetTop);
                            }));

                    return;
                }

                double currentLeft =
                    window.Left;

                double currentTop =
                    window.Top;

                if (double.IsNaN(currentLeft))
                    currentLeft =
                        targetLeft;

                if (double.IsNaN(currentTop))
                    currentTop =
                        targetTop;

                // =================================================
                // NEW ANIMATION
                // =================================================

                if (!_moveAnimating ||
                    _movingWindow != window)
                {
                    _moveStartLeft =
                        currentLeft;

                    _moveStartTop =
                        currentTop;

                    _moveStartTime =
                        DateTime.UtcNow;

                    _movingWindow =
                        window;

                    _moveAnimating =
                        true;

                    CompositionTarget.Rendering +=
                        MoveRendering;
                }

                // =================================================
                // UPDATE TARGET
                // =================================================

                _moveTargetLeft =
                    targetLeft;

                _moveTargetTop =
                    targetTop;
            }
            catch
            {
            }
        }

        // =========================================================
        // SMOOTH MOVE RENDERING
        // =========================================================

        private static void MoveRendering(
            object sender,
            EventArgs e)
        {
            try
            {
                if (!_moveAnimating ||
                    _movingWindow == null)
                {
                    StopSmoothMove();
                    return;
                }

                MainWindow window =
                    _movingWindow;

                if (!window.IsVisible)
                {
                    StopSmoothMove();
                    return;
                }

                double elapsed =
                    (
                        DateTime.UtcNow -
                        _moveStartTime
                    ).TotalSeconds;

                double progress =
                    elapsed /
                    WINDOW_MOVE_DURATION;

                // =================================================
                // COMPLETE
                // =================================================

                if (progress >= 1.0)
                {
                    window.Left =
                        _moveTargetLeft;

                    window.Top =
                        _moveTargetTop;

                    StopSmoothMove();

                    return;
                }

                if (progress < 0)
                    progress = 0;

                // =================================================
                // EASE OUT
                // =================================================

                double easedProgress =
                     progress * progress *
                     (3.0 - 2.0 * progress);

                double newLeft =
                    _moveStartLeft +
                    (
                        _moveTargetLeft -
                        _moveStartLeft
                    ) *
                    easedProgress;

                double newTop =
                    _moveStartTop +
                    (
                        _moveTargetTop -
                        _moveStartTop
                    ) *
                    easedProgress;

                window.Left =
                    newLeft;

                window.Top =
                    newTop;
            }
            catch
            {
                StopSmoothMove();
            }
        }

        // =========================================================
        // STOP SMOOTH MOVE
        // =========================================================

        public static void StopSmoothMove()
        {
            try
            {
                CompositionTarget.Rendering -=
                    MoveRendering;
            }
            catch
            {
            }

            _moveAnimating =
                false;

            _movingWindow =
                null;

            _moveStartLeft =
                0;

            _moveStartTop =
                0;

            _moveTargetLeft =
                0;

            _moveTargetTop =
                0;
        }

        // =========================================================
        // SETTINGS WINDOW SMOOTH MOVEMENT
        // =========================================================

        private static bool _settingsMoveAnimating;

        private static double _settingsMoveStartLeft;
        private static double _settingsMoveStartTop;

        private static double _settingsMoveTargetLeft;
        private static double _settingsMoveTargetTop;

        private static DateTime _settingsMoveStartTime;

        private const double SETTINGS_MOVE_DISTANCE = 50.0;
        private const double SETTINGS_MOVE_DURATION = 0.12;


        // =========================================================
        // MOVE SETTINGS WINDOW
        // =========================================================

        public static void MoveSettingsWindow(
            SettingsWindow window,
            int hotkeyId)
        {
            try
            {
                if (window == null ||
                    !window.IsVisible)
                    return;

                if (window.WindowState ==
                    WindowState.Minimized)
                    return;

                double width = window.ActualWidth;
                double height = window.ActualHeight;

                if (double.IsNaN(width) || width <= 0)
                    width = window.Width;

                if (double.IsNaN(height) || height <= 0)
                    height = window.Height;

                double currentLeft = window.Left;
                double currentTop = window.Top;

                if (double.IsNaN(currentLeft))
                    currentLeft = 0;

                if (double.IsNaN(currentTop))
                    currentTop = 0;

                // If already animating, continue from previous target
                double baseLeft =
                    _settingsMoveAnimating
                        ? _settingsMoveTargetLeft
                        : currentLeft;

                double baseTop =
                    _settingsMoveAnimating
                        ? _settingsMoveTargetTop
                        : currentTop;

                double targetLeft = baseLeft;
                double targetTop = baseTop;

                switch (hotkeyId)
                {
                    case HotKeysRegister.MOVE_LEFT_HOTKEY_ID:
                        targetLeft -= SETTINGS_MOVE_DISTANCE;
                        break;

                    case HotKeysRegister.MOVE_RIGHT_HOTKEY_ID:
                        targetLeft += SETTINGS_MOVE_DISTANCE;
                        break;

                    case HotKeysRegister.MOVE_UP_HOTKEY_ID:
                        targetTop -= SETTINGS_MOVE_DISTANCE;
                        break;

                    case HotKeysRegister.MOVE_DOWN_HOTKEY_ID:
                        targetTop += SETTINGS_MOVE_DISTANCE;
                        break;

                    default:
                        return;
                }

                // =====================================================
                // SCREEN BOUNDARY
                // =====================================================

                double screenLeft =
                    SystemParameters.WorkArea.Left;

                double screenTop =
                    SystemParameters.WorkArea.Top;

                double screenRight =
                    SystemParameters.WorkArea.Right;

                double screenBottom =
                    SystemParameters.WorkArea.Bottom;

                double maxLeft =
                    screenRight - width;

                double maxTop =
                    screenBottom - height;

                if (maxLeft < screenLeft)
                    maxLeft = screenLeft;

                if (maxTop < screenTop)
                    maxTop = screenTop;

                targetLeft =
                    Math.Max(
                        screenLeft,
                        Math.Min(
                            targetLeft,
                            maxLeft));

                targetTop =
                    Math.Max(
                        screenTop,
                        Math.Min(
                            targetTop,
                            maxTop));

                StartSettingsWindowAnimation(
                    window,
                    targetLeft,
                    targetTop);
            }
            catch
            {
            }
        }


        // =========================================================
        // START SETTINGS WINDOW ANIMATION
        // =========================================================

        private static void StartSettingsWindowAnimation(
            SettingsWindow window,
            double targetLeft,
            double targetTop)
        {
            try
            {
                if (window == null)
                    return;

                if (!window.Dispatcher.CheckAccess())
                {
                    window.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            StartSettingsWindowAnimation(
                                window,
                                targetLeft,
                                targetTop);
                        }));

                    return;
                }

                double currentLeft = window.Left;
                double currentTop = window.Top;

                if (double.IsNaN(currentLeft))
                    currentLeft = targetLeft;

                if (double.IsNaN(currentTop))
                    currentTop = targetTop;

                // =====================================================
                // START NEW ANIMATION
                // =====================================================

                if (!_settingsMoveAnimating)
                {
                    _settingsMoveStartLeft =
                        currentLeft;

                    _settingsMoveStartTop =
                        currentTop;

                    _settingsMoveStartTime =
                        DateTime.UtcNow;

                    _settingsMoveAnimating = true;

                    CompositionTarget.Rendering +=
                        SettingsWindowMoveRendering;
                }

                // =====================================================
                // UPDATE TARGET
                // =====================================================

                _settingsMoveTargetLeft =
                    targetLeft;

                _settingsMoveTargetTop =
                    targetTop;
            }
            catch
            {
                _settingsMoveAnimating = false;
            }
        }


        // =========================================================
        // SETTINGS WINDOW RENDERING
        // =========================================================

        private static void SettingsWindowMoveRendering(
            object sender,
            EventArgs e)
        {
            try
            {
                if (!_settingsMoveAnimating)
                    return;

                double elapsed =
                    (
                        DateTime.UtcNow -
                        _settingsMoveStartTime
                    ).TotalSeconds;

                double progress =
                    elapsed /
                    SETTINGS_MOVE_DURATION;

                if (progress >= 1.0)
                {
                    SettingsWindow window =
                        FindVisibleSettingsWindow();

                    if (window != null)
                    {
                        window.Left =
                            _settingsMoveTargetLeft;

                        window.Top =
                            _settingsMoveTargetTop;
                    }

                    StopSettingsWindowAnimation();
                    return;
                }

                if (progress < 0)
                    progress = 0;

                // =====================================================
                // SMOOTH EASE OUT
                // =====================================================

                double easedProgress =
                    1.0 -
                    Math.Pow(
                        1.0 - progress,
                        3.0);

                double newLeft =
                    _settingsMoveStartLeft +
                    (
                        _settingsMoveTargetLeft -
                        _settingsMoveStartLeft
                    ) *
                    easedProgress;

                double newTop =
                    _settingsMoveStartTop +
                    (
                        _settingsMoveTargetTop -
                        _settingsMoveStartTop
                    ) *
                    easedProgress;

                SettingsWindow settingsWindow =
                    FindVisibleSettingsWindow();

                if (settingsWindow == null)
                {
                    StopSettingsWindowAnimation();
                    return;
                }

                settingsWindow.Left = newLeft;
                settingsWindow.Top = newTop;
            }
            catch
            {
                StopSettingsWindowAnimation();
            }
        }


        // =========================================================
        // FIND SETTINGS WINDOW
        // =========================================================

        private static SettingsWindow
            FindVisibleSettingsWindow()
        {
            try
            {
                if (Application.Current == null)
                    return null;

                foreach (Window window
                         in Application.Current.Windows)
                {
                    SettingsWindow settings =
                        window as SettingsWindow;

                    if (settings != null &&
                        settings.IsVisible)
                    {
                        return settings;
                    }
                }
            }
            catch
            {
            }

            return null;
        }


        // =========================================================
        // STOP SETTINGS WINDOW ANIMATION
        // =========================================================

        public static void StopSettingsWindowAnimation()
        {
            try
            {
                if (!_settingsMoveAnimating)
                    return;

                CompositionTarget.Rendering -=
                    SettingsWindowMoveRendering;

                _settingsMoveAnimating = false;
            }
            catch
            {
                _settingsMoveAnimating = false;
            }
        }
    }
}