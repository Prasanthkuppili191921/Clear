using System;
using System.Windows;
using System.Windows.Controls;

namespace AiInterviewAssistant.Settings.HotKeysReadonly
{
    public partial class HotKeysReadonly : UserControl
    {
        public HotKeysReadonly()
        {
            InitializeComponent();

            Loaded += HotKeysReadonly_Loaded;
        }


        private void HotKeysReadonly_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            LoadConfiguredHotkeys();
        }


        // =========================================================
        // LOAD HOTKEYS
        // =========================================================

        private void LoadConfiguredHotkeys()
        {
            try
            {
                // =================================================
                // SEND MESSAGE
                // =================================================
                //
                // Actual application hotkey:
                // Ctrl + Enter
                //
                // Do NOT read settings.MessageHotkey here because
                // Ctrl + M is now used for Message Mode.
                //

                MessageHotkeyText.Text =
                    "Ctrl + Enter";


                // =================================================
                // VOICE
                // =================================================
                //
                // Actual application registration:
                // Ctrl + Space
                //

                VoiceHotkeyText.Text =
                    "Ctrl + Space";


                // =================================================
                // VISION
                // =================================================
                //
                // Actual application registration:
                // Alt + Enter
                //

                VisionHotkeyText.Text =
                    "Alt + Enter";
            }
            catch
            {
                // =================================================
                // SAFE FALLBACKS
                // =================================================

                MessageHotkeyText.Text =
                    "Ctrl + Enter";

                VoiceHotkeyText.Text =
                    "Ctrl + Space";

                VisionHotkeyText.Text =
                    "Alt + Enter";
            }
        }
    }
}