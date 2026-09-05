using System.Windows;
using System.Windows.Controls;

namespace AiInterviewAssistant.Settings.General
{
    public partial class GeneralTab : UserControl
    {
        private readonly GeneralBusiness generalBusiness;

        public GeneralTab(AppSettings settings)
        {
            InitializeComponent();

            generalBusiness =
                new GeneralBusiness(settings);

            LoadSettings();
        }

        // =====================================================
        // LOAD UI
        // =====================================================

        public void LoadSettings()
        {
            string answerMode =
                generalBusiness.GetAnswerMode();

            switch (
                answerMode
                    .Trim()
                    .ToLowerInvariant())
            {
                case "short":
                    AnswerModeComboBox.SelectedIndex = 0;
                    break;

                case "normal":
                    AnswerModeComboBox.SelectedIndex = 1;
                    break;

                case "detailed":
                case "detail":
                    AnswerModeComboBox.SelectedIndex = 2;
                    break;

                default:
                    AnswerModeComboBox.SelectedIndex = 0;
                    break;
            }
        }

        // =====================================================
        // UPDATE SETTINGS
        // =====================================================

        public void SaveSettings()
        {
            if (
                AnswerModeComboBox.SelectedItem
                is ComboBoxItem answerItem)
            {
                string answerMode =
                    answerItem.Content?.ToString();

                generalBusiness.SetAnswerMode(
                    answerMode);
            }
        }

        // =====================================================
        // PERSIST
        // =====================================================

        public void PersistSettings()
        {
            generalBusiness.Save();
        }
    }
}