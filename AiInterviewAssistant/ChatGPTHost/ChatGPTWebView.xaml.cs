using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;

namespace AiInterviewAssistant.ChatGPTHost
{
    public partial class ChatGPTWebView : UserControl
    {
        // =========================================================
        // CHATGPT READY EVENT
        // =========================================================

        public event EventHandler ChatGPTReady;


        public ChatGPTWebView()
        {
            InitializeComponent();

            Background = Brushes.Transparent;

            Loaded += ChatGPTWebView_Loaded;
        }


        // =========================================================
        // FOCUS CHATGPT
        // =========================================================

        public void FocusChatGPT()
        {
            try
            {
                ChatGPTBrowser.Focus();
            }
            catch
            {
                // Ignore focus errors
            }
        }


        // =========================================================
        // INJECT QUESTION INTO CHATGPT
        //
        // IMPORTANT:
        // This ONLY enters the question.
        // It does NOT press Send.
        // =========================================================

        public async Task InjectQuestionAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return;


            try
            {
                if (ChatGPTBrowser.CoreWebView2 == null)
                {
                    await ChatGPTBrowser.EnsureCoreWebView2Async();
                }


                string jsonQuestion =
                    JsonConvert.SerializeObject(question);


                string script =
                    "window.aiInterviewAssistant && " +
                    "window.aiInterviewAssistant.setQuestion(" +
                    jsonQuestion +
                    ");";


                await ChatGPTBrowser.ExecuteScriptAsync(
                    script);
            }
            catch
            {
                // Ignore injection errors for now.
            }
        }

        public async Task SendQuestionAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return;

            try
            {
                if (ChatGPTBrowser.CoreWebView2 == null)
                {
                    await ChatGPTBrowser.EnsureCoreWebView2Async();
                }

                await InjectQuestionAsync(question);

                string script =
                    "window.aiInterviewAssistant && " +
                    "window.aiInterviewAssistant.sendQuestion();";

                await ChatGPTBrowser.ExecuteScriptAsync(script);
            }
            catch
            {
                // Ignore ChatGPT send errors for now.
            }
        }

        // =========================================================
        // SCROLL CHATGPT WEBVIEW
        // direction:
        //   -1 = UP
        //    1 = DOWN
        // =========================================================

        public async Task ScrollChatGPTAsync(int direction)
        {
            try
            {
                if (direction != -1 && direction != 1)
                    return;

                if (ChatGPTBrowser.CoreWebView2 == null)
                    return;

                string script =
                    "window.aiInterviewAssistant && " +
                    "window.aiInterviewAssistant.scrollChat(" +
                    direction.ToString() +
                    ");";

                await ChatGPTBrowser.ExecuteScriptAsync(script);
            }
            catch
            {
                // Ignore ChatGPT scroll errors.
            }
        }


        // =========================================================
        // LOAD CHATGPT
        // =========================================================

        private async void ChatGPTWebView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (Visibility != Visibility.Visible)
                    return;

                await ChatGPTBrowser.EnsureCoreWebView2Async();

                ChatGPTBrowser.CoreWebView2.OpenDevToolsWindow();


                // =================================================
                // TRANSPARENT WEBVIEW BACKGROUND
                // =================================================

                ChatGPTBrowser.DefaultBackgroundColor =
                    System.Drawing.Color.Transparent;


                // =================================================
                // LOAD JAVASCRIPT FILES
                // =================================================

                string scriptsFolder =
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "ChatGPTHost\\Scripts");

                string[] scriptFiles =
                {
                    "ChatGPTStyles.js",
                    "ChatGPTInput.js",
                    "ChatGPTScroll.js",
                    "ChatGPTSend.js",
                    "ChatGPTCleanup.js",
                    "ChatGPTUI.js"
                };

                foreach (string scriptFile in scriptFiles)
                {
                    string scriptPath =
                        Path.Combine(
                            scriptsFolder,
                            scriptFile);

                    if (!File.Exists(scriptPath))
                    {
                        MessageBox.Show(
                            scriptFile +
                            " file was not found.\n\n" +
                            scriptPath,
                            "ChatGPT",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                        return;
                    }

                    string script =
                        File.ReadAllText(scriptPath);

                    // =================================================
                    // REGISTER JAVASCRIPT
                    // =================================================

                    await ChatGPTBrowser.CoreWebView2
                        .AddScriptToExecuteOnDocumentCreatedAsync(
                            script);
                }


                // =================================================
                // WAIT FOR CHATGPT NAVIGATION
                // =================================================

                ChatGPTBrowser.NavigationCompleted +=
                    ChatGPTBrowser_NavigationCompleted;


                // =================================================
                // NAVIGATE TO CHATGPT
                // =================================================

                ChatGPTBrowser.CoreWebView2.Navigate(
                    "https://chatgpt.com/");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "ChatGPT WebView2 initialization failed.\n\n" +
                    ex.Message,
                    "ChatGPT",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // =========================================================
        // CHATGPT NAVIGATION COMPLETED
        // =========================================================

        private async void ChatGPTBrowser_NavigationCompleted(
            object sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                if (!e.IsSuccess)
                    return;


                // Give ChatGPT React UI time to create
                // the textbox.
                await Task.Delay(1500);

                // No automatic question injection here.
            }
            catch
            {
            }
        }

        // =========================================================
        // TOGGLE CHATGPT VOICE
        //
        // ChatGPTView only.
        //
        // First call:
        //     Start Voice
        //
        // Second call:
        //     Stop Voice
        // =========================================================

        public async Task ToggleVoiceAsync()
        {
            try
            {
                if (ChatGPTBrowser.CoreWebView2 == null)
                {
                    await ChatGPTBrowser.EnsureCoreWebView2Async();
                }

                string script =
                    "window.aiInterviewAssistant && " +
                    "window.aiInterviewAssistant.toggleVoice();";

                await ChatGPTBrowser.ExecuteScriptAsync(script);
            }
            catch
            {
                // Ignore ChatGPT voice errors.
            }
        }
    }
}