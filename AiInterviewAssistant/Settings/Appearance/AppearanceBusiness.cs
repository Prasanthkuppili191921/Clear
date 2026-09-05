using System;
using System.Windows;

namespace AiInterviewAssistant.Settings.Appearance
{
    public class AppearanceBusiness
    {
        private readonly AppSettings settings;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public AppearanceBusiness(
            AppSettings settings)
        {
            this.settings = settings;
        }


        // =====================================================
        // LOAD
        // =====================================================

        public void LoadSettings()
        {
            // AppSettings already contains
            // persisted values.
        }


        // =====================================================
        // SAVE
        // =====================================================

        public void SaveSettings(
            double opacity)
        {
            settings.Opacity =
                ClampOpacity(opacity);
        }


        // =====================================================
        // APPLY OPACITY
        // =====================================================

        public void ApplyOpacity(
            Window window,
            double opacity)
        {
            if (window == null)
                return;


            window.Opacity =
                ClampOpacity(opacity);
        }


        // =====================================================
        // APPLY CURRENT SETTINGS OPACITY
        // =====================================================

        public void ApplyOpacity(
            Window window)
        {
            if (window == null)
                return;


            ApplyOpacity(
                window,
                settings.Opacity);
        }


        // =====================================================
        // CLAMP OPACITY
        // =====================================================

        private double ClampOpacity(
            double opacity)
        {
            if (double.IsNaN(opacity) ||
                double.IsInfinity(opacity))
            {
                return 0.85;
            }


            if (opacity < 0.5)
                return 0.5;


            if (opacity > 1.0)
                return 1.0;


            return opacity;
        }


        // =====================================================
        // GET ANIMATION DURATION
        // =====================================================
        //
        // Animation Speed UI removed.
        // Application uses fixed Slow animation.
        //

        public TimeSpan GetAnimationDuration()
        {
            return TimeSpan.FromMilliseconds(
                AppearanceManager.GetAnimationDurationMilliseconds(
                    600,
                    settings));
        }


        // =====================================================
        // GET THEME
        // =====================================================
        //
        // Theme UI removed.
        // Application uses fixed Dark theme.
        //

        public string GetTheme()
        {
            return "Dark";
        }
    }
}