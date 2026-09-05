using System.Globalization;
using System.Windows.Controls;

namespace AiInterviewAssistant.Settings.AI
{
    public partial class AITab : UserControl
    {
        private readonly AIBusiness aiBusiness;

        public AITab(AppSettings settings)
        {
            InitializeComponent();

            aiBusiness =
                new AIBusiness(settings);

            LoadSettings();
        }


        // =====================================================
        // LOAD SETTINGS
        // =====================================================

        public void LoadSettings()
        {

            // -------------------------------------------------
            // TEMPERATURE
            // -------------------------------------------------

            double temperature =
                aiBusiness.GetTemperature();


            if (temperature < 0)
                temperature = 0;


            if (temperature > 2)
                temperature = 2;


            TemperatureTextBox.Text =
                temperature.ToString(
                    "0.##",
                    CultureInfo.InvariantCulture);


            // -------------------------------------------------
            // RESPONSE LENGTH
            // -------------------------------------------------

            string responseLength =
                aiBusiness.GetResponseLength();


            switch (
                responseLength?
                    .Trim()
                    .ToLowerInvariant())
            {
                case "short":

                    ResponseLengthComboBox.SelectedIndex =
                        0;

                    break;


                case "long":

                    ResponseLengthComboBox.SelectedIndex =
                        2;

                    break;


                case "medium":
                default:

                    ResponseLengthComboBox.SelectedIndex =
                        1;

                    break;
            }


            // -------------------------------------------------
            // SYSTEM PROMPT
            // -------------------------------------------------

            SystemPromptTextBox.Text =
                aiBusiness.GetSystemPrompt();
        }


        // =====================================================
        // SAVE SETTINGS TO APP SETTINGS OBJECT
        // =====================================================

        public void SaveSettings()
        {
            
            // -------------------------------------------------
            // TEMPERATURE
            // -------------------------------------------------

            double temperature;


            if (!double.TryParse(
                    TemperatureTextBox.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out temperature))
            {
                temperature = 0.2;
            }


            // -------------------------------------------------
            // CLAMP TEMPERATURE
            // -------------------------------------------------

            if (temperature < 0)
                temperature = 0;


            if (temperature > 2)
                temperature = 2;


            aiBusiness.SetTemperature(
                temperature);


            // -------------------------------------------------
            // RESPONSE LENGTH
            // -------------------------------------------------

            if (ResponseLengthComboBox.SelectedItem
                is ComboBoxItem responseItem)
            {
                string responseLength =
                    responseItem.Content?
                        .ToString();


                if (string.IsNullOrWhiteSpace(
                        responseLength))
                {
                    responseLength = "Medium";
                }


                aiBusiness.SetResponseLength(
                    responseLength);
            }
            else
            {
                aiBusiness.SetResponseLength(
                    "Medium");
            }


            // -------------------------------------------------
            // SYSTEM PROMPT
            // -------------------------------------------------

            aiBusiness.SetSystemPrompt(
                SystemPromptTextBox.Text);
        }


        // =====================================================
        // PERSIST TO JSON
        // =====================================================

        public void PersistSettings()
        {
            aiBusiness.Save();
        }
    }
}