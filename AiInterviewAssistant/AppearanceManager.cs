using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace AiInterviewAssistant
{
    public static class AppearanceManager
    {
        // =========================================================
        // RESOURCE KEYS
        // =========================================================

        public const string WindowBackgroundBrush =
            "WindowBackgroundBrush";

        public const string HeaderBackgroundBrush =
            "HeaderBackgroundBrush";

        public const string PanelBackgroundBrush =
            "PanelBackgroundBrush";

        public const string MainBorderBrush =
            "MainBorderBrush";

        public const string DividerBrush =
            "DividerBrush";

        public const string PrimaryTextBrush =
            "PrimaryTextBrush";

        public const string SecondaryTextBrush =
            "SecondaryTextBrush";

        public const string MutedTextBrush =
            "MutedTextBrush";

        public const string AccentBrush =
            "AccentBrush";


        // =========================================================
        // SETTINGS TAB LEGACY RESOURCE ALIASES
        // =========================================================

        public const string TextBrush =
            "TextBrush";

        public const string SecondaryBrush =
            "SecondaryBrush";

        public const string MutedBrush =
            "MutedBrush";


        // =========================================================
        // REGISTERED WINDOWS
        // =========================================================

        private static readonly List<Window> windows =
            new List<Window>();


        // =========================================================
        // REGISTER WINDOW
        // =========================================================

        public static void RegisterWindow(
            Window window)
        {
            if (window == null)
                return;

            if (windows.Contains(window))
                return;

            windows.Add(window);

            window.Closed +=
                Window_Closed;


            // -----------------------------------------------------
            // APPLY CURRENT APPEARANCE
            //
            // Theme = fixed Dark
            // Opacity = settings value
            // -----------------------------------------------------

            AppSettings settings =
                SettingsService.Load()
                ?? new AppSettings();

            Apply(
                window,
                settings);
        }


        // =========================================================
        // WINDOW CLOSED
        // =========================================================

        private static void Window_Closed(
            object sender,
            EventArgs e)
        {
            Window window =
                sender as Window;

            if (window == null)
                return;

            windows.Remove(
                window);
        }


        // =========================================================
        // UNREGISTER WINDOW
        // =========================================================

        public static void UnregisterWindow(
            Window window)
        {
            if (window == null)
                return;

            windows.Remove(
                window);
        }


        // =========================================================
        // APPLY
        // =========================================================
        //
        // FIXED:
        //     Theme = Dark
        //
        // CONFIGURABLE:
        //     Opacity
        //
        // =========================================================

        public static void Apply(
            Window window,
            AppSettings settings)
        {
            if (window == null)
                return;


            if (settings == null)
            {
                settings =
                    SettingsService.Load()
                    ?? new AppSettings();
            }


            // =====================================================
            // FIXED DARK THEME
            // =====================================================

            ApplyDarkTheme(
                window);


            // =====================================================
            // OPACITY
            // =====================================================

            ApplyOpacity(
                window,
                settings.Opacity);
        }


        // =========================================================
        // REFRESH ALL REGISTERED WINDOWS
        // =========================================================

        public static void RefreshAllWindows(
            AppSettings settings)
        {
            if (settings == null)
            {
                settings =
                    SettingsService.Load()
                    ?? new AppSettings();
            }


            List<Window> copy =
                new List<Window>(
                    windows);


            foreach (Window window in copy)
            {
                if (window == null)
                    continue;


                try
                {
                    if (!window.Dispatcher.CheckAccess())
                    {
                        window.Dispatcher.Invoke(
                            new Action(
                                () =>
                                {
                                    Apply(
                                        window,
                                        settings);
                                }));

                        continue;
                    }


                    Apply(
                        window,
                        settings);
                }
                catch
                {
                    // Ignore windows that are already closing.
                }
            }
        }


        // =========================================================
        // DARK THEME
        // =========================================================
        //
        // Theme is intentionally fixed to Dark.
        //
        // Light/System theme functionality is no longer used.
        //
        // =========================================================

        private static void ApplyDarkTheme(
            Window window)
        {
            if (window == null)
                return;


            Color primaryText =
                Color.FromRgb(
                    244,
                    246,
                    250);

            Color secondaryText =
                Color.FromRgb(
                    133,
                    140,
                    153);

            Color mutedText =
                Color.FromRgb(
                    104,
                    113,
                    128);


            // -----------------------------------------------------
            // MAIN RESOURCES
            // -----------------------------------------------------

            SetResource(
                window,
                WindowBackgroundBrush,
                Color.FromArgb(
                    102,
                    12,
                    14,
                    18));


            SetResource(
                window,
                HeaderBackgroundBrush,
                Color.FromArgb(
                    34,
                    0,
                    0,
                    0));


            SetResource(
                window,
                PanelBackgroundBrush,
                Color.FromArgb(
                    34,
                    0,
                    0,
                    0));


            SetResource(
                window,
                MainBorderBrush,
                Color.FromRgb(
                    51,
                    60,
                    77));


            SetResource(
                window,
                DividerBrush,
                Color.FromRgb(
                    41,
                    49,
                    59));


            SetResource(
                window,
                PrimaryTextBrush,
                primaryText);


            SetResource(
                window,
                SecondaryTextBrush,
                secondaryText);


            SetResource(
                window,
                MutedTextBrush,
                mutedText);


            SetResource(
                window,
                AccentBrush,
                Color.FromRgb(
                    77,
                    141,
                    255));


            // -----------------------------------------------------
            // SETTINGS TAB ALIASES
            // -----------------------------------------------------

            SetResource(
                window,
                TextBrush,
                primaryText);


            SetResource(
                window,
                SecondaryBrush,
                secondaryText);


            SetResource(
                window,
                MutedBrush,
                mutedText);
        }


        // =========================================================
        // SET RESOURCE
        // =========================================================

        private static void SetResource(
            Window window,
            string key,
            Color color)
        {
            if (window == null)
                return;


            SolidColorBrush brush =
                new SolidColorBrush(
                    color);


            brush.Freeze();


            window.Resources[key] =
                brush;
        }


        // =========================================================
        // OPACITY
        // =========================================================

        private static void ApplyOpacity(
            Window window,
            double opacity)
        {
            if (window == null)
                return;


            if (double.IsNaN(opacity) ||
                double.IsInfinity(opacity))
            {
                opacity = 0.85;
            }


            if (opacity < 0.5)
                opacity = 0.5;


            if (opacity > 1.0)
                opacity = 1.0;


            window.Opacity =
                opacity;
        }


        // =========================================================
        // ANIMATION SPEED
        // =========================================================
        //
        // FIXED:
        //     Slow
        //
        // Base duration:
        //     600 ms
        //
        // Slow:
        //     1200 ms
        //
        // The settings value is intentionally ignored.
        //
        // =========================================================

        private static int CalculateAnimationDuration(
            int normalDuration,
            AppSettings settings)
        {
            if (normalDuration <= 0)
            {
                normalDuration = 600;
            }


            // -----------------------------------------------------
            // FIXED SLOW ANIMATION
            // -----------------------------------------------------

            return normalDuration * 2;
        }


        // =========================================================
        // ANIMATION DURATION - CURRENT SETTINGS
        // =========================================================
        //
        // Returns WPF Duration.
        //
        // =========================================================

        public static Duration GetAnimationDuration(
            int normalDuration)
        {
            AppSettings settings =
                SettingsService.Load()
                ?? new AppSettings();


            int milliseconds =
                CalculateAnimationDuration(
                    normalDuration,
                    settings);


            return new Duration(
                TimeSpan.FromMilliseconds(
                    milliseconds));
        }


        // =========================================================
        // ANIMATION DURATION - EXPLICIT SETTINGS
        // =========================================================
        //
        // Kept for compatibility with existing callers.
        //
        // Animation speed remains fixed to Slow.
        //
        // =========================================================

        public static Duration GetAnimationDuration(
            int normalDuration,
            AppSettings settings)
        {
            int milliseconds =
                CalculateAnimationDuration(
                    normalDuration,
                    settings);


            return new Duration(
                TimeSpan.FromMilliseconds(
                    milliseconds));
        }


        // =========================================================
        // ANIMATION DURATION - CURRENT SETTINGS
        // AS MILLISECONDS
        // =========================================================

        public static double GetAnimationDurationMilliseconds(
            int normalDuration)
        {
            AppSettings settings =
                SettingsService.Load()
                ?? new AppSettings();


            return CalculateAnimationDuration(
                normalDuration,
                settings);
        }


        // =========================================================
        // ANIMATION DURATION - EXPLICIT SETTINGS
        // AS MILLISECONDS
        // =========================================================
        //
        // Kept for compatibility with existing callers.
        //
        // =========================================================

        public static double GetAnimationDurationMilliseconds(
            int normalDuration,
            AppSettings settings)
        {
            return CalculateAnimationDuration(
                normalDuration,
                settings);
        }
    }
}