using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace AiInterviewAssistant.Privacy
{
    public static class ScreenCaptureProtection
    {
        // =========================================================
        // STEALTH MODE
        // =========================================================

        private static bool _stealthMode =
            true;


        // =========================================================
        // WINDOWS DISPLAY AFFINITY
        // =========================================================

        private const uint WDA_NONE =
            0x00000000;

        private const uint WDA_EXCLUDEFROMCAPTURE =
            0x00000011;


        [System.Runtime.InteropServices.DllImport(
            "user32.dll",
            SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(
            IntPtr hWnd,
            uint dwAffinity);


        // =========================================================
        // UI STATE STORAGE
        // =========================================================

        private static readonly Dictionary<
            DependencyObject,
            Cursor> OriginalCursors =
            new Dictionary<
                DependencyObject,
                Cursor>();


        private static readonly HashSet<
            DependencyObject> CursorModified =
            new HashSet<
                DependencyObject>();


        private static readonly Dictionary<
            DependencyObject,
            bool> OriginalToolTipStates =
            new Dictionary<
                DependencyObject,
                bool>();


        private static readonly HashSet<
            DependencyObject> ToolTipModified =
            new HashSet<
                DependencyObject>();


        // =========================================================
        // LOAD STEALTH MODE FROM APP.CONFIG
        // =========================================================

        public static void LoadStealthMode()
        {
            try
            {
                string value =
                    ConfigurationManager
                        .AppSettings[
                            "StealthMode"];

                if (string.IsNullOrWhiteSpace(value))
                {
                    _stealthMode = true;
                    return;
                }

                if (bool.TryParse(
                    value,
                    out bool enabled))
                {
                    _stealthMode = enabled;
                }
                else
                {
                    _stealthMode = true;
                }
            }
            catch
            {
                // Safe default:
                // Stealth Mode ON
                _stealthMode = true;
            }
        }


        // =========================================================
        // CURRENT STEALTH MODE
        // =========================================================

        public static bool IsEnabled
        {
            get
            {
                return _stealthMode;
            }
        }


        // =========================================================
        // ENABLE
        // =========================================================

        public static bool Enable(
            Window window)
        {
            if (window == null)
                return false;

            if (!_stealthMode)
                return false;

            try
            {
                IntPtr hwnd =
                    EnsureWindowHandle(
                        window);

                if (hwnd == IntPtr.Zero)
                    return false;


                bool result =
                    SetWindowDisplayAffinity(
                        hwnd,
                        WDA_EXCLUDEFROMCAPTURE);


                if (result)
                {
                    ApplyUiPrivacy(
                        window,
                        true);
                }


                return result;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // DISABLE
        // =========================================================

        public static bool Disable(
            Window window)
        {
            if (window == null)
                return false;

            try
            {
                IntPtr hwnd =
                    new WindowInteropHelper(
                        window).Handle;


                if (hwnd == IntPtr.Zero)
                    return false;


                bool result =
                    SetWindowDisplayAffinity(
                        hwnd,
                        WDA_NONE);


                ApplyUiPrivacy(
                    window,
                    false);


                return result;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // GLOBAL WINDOW PROTECTION
        // =========================================================

        private static bool
            _globalWindowProtectionRegistered =
            false;


        // =========================================================
        // REGISTER GLOBAL WINDOW PROTECTION
        // =========================================================

        public static void RegisterGlobalWindowProtection()
        {
            if (_globalWindowProtectionRegistered)
                return;

            try
            {
                _globalWindowProtectionRegistered =
                    true;


                EventManager.RegisterClassHandler(
                    typeof(Window),
                    FrameworkElement.LoadedEvent,
                    new RoutedEventHandler(
                        GlobalWindow_Loaded));
            }
            catch
            {
            }
        }


        // =========================================================
        // GLOBAL WINDOW LOADED
        // =========================================================

        private static void GlobalWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                Window window =
                    sender as Window;

                if (window == null)
                    return;


                // -------------------------------------------------
                // STEALTH MODE OFF
                // -------------------------------------------------

                if (!_stealthMode)
                    return;


                // -------------------------------------------------
                // STEALTH MODE ON
                // -------------------------------------------------

                Enable(window);
            }
            catch
            {
            }
        }


        // =========================================================
        // APPLY TO ALL APPLICATION WINDOWS
        // =========================================================

        public static void ApplyToAllWindows(
            bool enabled)
        {
            try
            {
                Application application =
                    Application.Current;


                if (application == null)
                    return;


                Window[] windows =
                    new Window[
                        application.Windows.Count];


                application.Windows.CopyTo(
                    windows,
                    0);


                foreach (Window window in windows)
                {
                    if (window == null)
                        continue;


                    try
                    {
                        if (enabled)
                        {
                            Enable(window);
                        }
                        else
                        {
                            Disable(window);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // ENSURE WINDOW HANDLE
        // =========================================================

        private static IntPtr EnsureWindowHandle(
            Window window)
        {
            try
            {
                if (window == null)
                    return IntPtr.Zero;


                WindowInteropHelper helper =
                    new WindowInteropHelper(
                        window);


                IntPtr hwnd =
                    helper.Handle;


                if (hwnd != IntPtr.Zero)
                    return hwnd;


                HwndSource source =
                    PresentationSource.FromVisual(
                        window) as HwndSource;


                if (source != null)
                {
                    hwnd =
                        source.Handle;


                    if (hwnd != IntPtr.Zero)
                        return hwnd;
                }


                return new WindowInteropHelper(
                    window).Handle;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }


        // =========================================================
        // APPLY UI PRIVACY
        // =========================================================

        private static void ApplyUiPrivacy(
            Window window,
            bool enabled)
        {
            try
            {
                if (window == null)
                    return;


                ApplyElementPrivacy(
                    window,
                    enabled);


                if (enabled)
                {
                    window.Cursor =
                        Cursors.Arrow;
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // APPLY TO VISUAL TREE
        // =========================================================

        private static void ApplyElementPrivacy(
            DependencyObject element,
            bool enabled)
        {
            if (element == null)
                return;


            try
            {
                if (element is FrameworkElement
                    frameworkElement)
                {
                    ApplyCursorPrivacy(
                        frameworkElement,
                        enabled);


                    ApplyToolTipPrivacy(
                        frameworkElement,
                        enabled);
                }
            }
            catch
            {
            }


            try
            {
                int count =
                    VisualTreeHelper.GetChildrenCount(
                        element);


                for (int i = 0;
                     i < count;
                     i++)
                {
                    DependencyObject child =
                        VisualTreeHelper.GetChild(
                            element,
                            i);


                    ApplyElementPrivacy(
                        child,
                        enabled);
                }
            }
            catch
            {
            }


            try
            {
                if (element is FrameworkContentElement
                    contentElement)
                {
                    ApplyContentElementPrivacy(
                        contentElement,
                        enabled);
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // CONTENT ELEMENT PRIVACY
        // =========================================================

        private static void ApplyContentElementPrivacy(
            FrameworkContentElement element,
            bool enabled)
        {
            if (element == null)
                return;


            try
            {
                if (enabled)
                {
                    if (!OriginalToolTipStates.ContainsKey(
                            element))
                    {
                        bool current =
                            ToolTipService.GetIsEnabled(
                                element);


                        OriginalToolTipStates[
                            element] =
                            current;
                    }


                    ToolTipService.SetIsEnabled(
                        element,
                        false);


                    ToolTipModified.Add(
                        element);
                }
                else
                {
                    if (OriginalToolTipStates.TryGetValue(
                        element,
                        out bool original))
                    {
                        ToolTipService.SetIsEnabled(
                            element,
                            original);
                    }


                    ToolTipModified.Remove(
                        element);


                    OriginalToolTipStates.Remove(
                        element);
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // CURSOR PRIVACY
        // =========================================================

        private static void ApplyCursorPrivacy(
            FrameworkElement element,
            bool enabled)
        {
            if (element == null)
                return;


            try
            {
                if (enabled)
                {
                    if (!CursorModified.Contains(
                            element))
                    {
                        Cursor original =
                            element.Cursor;


                        OriginalCursors[
                            element] =
                            original;


                        CursorModified.Add(
                            element);
                    }


                    element.Cursor =
                        Cursors.Arrow;
                }
                else
                {
                    if (OriginalCursors.TryGetValue(
                        element,
                        out Cursor original))
                    {
                        element.Cursor =
                            original;
                    }


                    CursorModified.Remove(
                        element);


                    OriginalCursors.Remove(
                        element);
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // TOOLTIP PRIVACY
        // =========================================================

        private static void ApplyToolTipPrivacy(
            FrameworkElement element,
            bool enabled)
        {
            if (element == null)
                return;


            try
            {
                if (enabled)
                {
                    if (!OriginalToolTipStates.ContainsKey(
                            element))
                    {
                        bool current =
                            ToolTipService.GetIsEnabled(
                                element);


                        OriginalToolTipStates[
                            element] =
                            current;
                    }


                    ToolTipService.SetIsEnabled(
                        element,
                        false);


                    ToolTipModified.Add(
                        element);
                }
                else
                {
                    if (OriginalToolTipStates.TryGetValue(
                        element,
                        out bool original))
                    {
                        ToolTipService.SetIsEnabled(
                            element,
                            original);
                    }


                    ToolTipModified.Remove(
                        element);


                    OriginalToolTipStates.Remove(
                        element);
                }
            }
            catch
            {
            }
        }
    }
}