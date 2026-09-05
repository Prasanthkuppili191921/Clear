using System.Windows;
using System.Windows.Controls;

namespace AiInterviewAssistant.Settings.Appearance
{
    public partial class AppearanceTab : UserControl
    {
        private readonly AppearanceBusiness business;

        private readonly AppSettings settings;

        private bool isLoading;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public AppearanceTab(
            AppSettings settings)
        {
            InitializeComponent();

            this.settings =
                settings;

            business =
                new AppearanceBusiness(
                    settings);

            LoadSettings();
        }


        // =====================================================
        // LOAD SETTINGS
        // =====================================================

        public void LoadSettings()
        {
            isLoading = true;

            try
            {
                business.LoadSettings();


                // -------------------------------------------------
                // OPACITY
                // -------------------------------------------------

                double opacity =
                    settings.Opacity;


                if (double.IsNaN(opacity) ||
                    double.IsInfinity(opacity))
                {
                    opacity = 0.85;
                }


                if (opacity <
                    OpacitySlider.Minimum)
                {
                    opacity =
                        OpacitySlider.Minimum;
                }


                if (opacity >
                    OpacitySlider.Maximum)
                {
                    opacity =
                        OpacitySlider.Maximum;
                }


                OpacitySlider.Value =
                    opacity;
            }
            finally
            {
                isLoading = false;
            }
        }


        // =====================================================
        // OPACITY LIVE PREVIEW
        // =====================================================

        private void OpacitySlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (isLoading)
                return;


            if (OpacitySlider == null)
                return;


            Window settingsWindow =
                Window.GetWindow(this);


            if (settingsWindow == null)
                return;


            double opacity =
                e.NewValue;


            // -------------------------------------------------
            // SAFETY
            // -------------------------------------------------

            if (double.IsNaN(opacity) ||
                double.IsInfinity(opacity))
            {
                return;
            }


            if (opacity < 0.5)
                opacity = 0.5;


            if (opacity > 1.0)
                opacity = 1.0;


            // -------------------------------------------------
            // LIVE PREVIEW
            //
            // Only SettingsWindow changes here.
            // AppSettings is NOT changed.
            // -------------------------------------------------

            business.ApplyOpacity(
                settingsWindow,
                opacity);
        }


        // =====================================================
        // SAVE SETTINGS
        // =====================================================

        public void SaveSettings()
        {
            double opacity =
                OpacitySlider.Value;


            business.SaveSettings(
                opacity);
        }
    }
}