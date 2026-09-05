using AiInterviewAssistant.Privacy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Tesseract;
using MessageBox = System.Windows.MessageBox;

namespace AiInterviewAssistant
{
    public partial class MainWindow : Window
    {

        // =========================================================
        // SCREENSHOT
        // =========================================================

        private Bitmap _currentScreenshot;


        // =========================================================
        // CONVERSATION
        // =========================================================

        private List<object> conversationHistory =
            new List<object>();


        // =========================================================
        // AI GENERATION
        // =========================================================

        private bool isGenerating = false;

        private CancellationTokenSource cancellationTokenSource;

        private string latestAiText = "";


        // =========================================================
        // AI TYPING ANIMATION
        // =========================================================

        private System.Windows.Threading.DispatcherTimer aiTypingTimer;

        private Border aiTypingBubble;

        private string aiTargetText = "";

        private int aiDisplayedLength = 0;


        // =========================================================
        // OCR
        // =========================================================

        private TesseractEngine ocrEngine;


        // =========================================================
        // ONLINE TEST
        // =========================================================

        private bool isOnlineTestMode = false;

        private bool isCapturingOnlineTestQuestion = false;

        private readonly object onlineCaptureLock =
            new object();

        private int onlineTestCaptureRunning = 0;


        // =========================================================
        // VISION REQUEST STATE
        // =========================================================

        private int visionRequestRunning = 0;


        // =========================================================
        // SETTINGS
        // =========================================================

        private SettingsManager settingsManager;

        private AppSettings currentSettings;

        private bool _chatGPTView;


        // =========================================================
        // PRIVACY
        // =========================================================

        private PrivacyManager _privacyManager;


        // =========================================================
        // SCREEN CAPTURE HIDE STATE
        // =========================================================

        private bool _hideFromCapture = true;


        // =========================================================
        // CURRENT SETTINGS WINDOW
        // =========================================================

        private SettingsWindow _currentSettingsWindow;


        // =========================================================
        // PRIVACY INITIALIZATION
        // =========================================================

        private bool _initializingPrivacy = true;

        // =========================================================
        // SMART ANSWER
        // =========================================================

        private bool _smartAnswerEnabled = true;

        public bool IsSmartAnswerEnabled
        {
            get
            {
                return _smartAnswerEnabled;
            }
        }


        private bool IsLocalVoiceEnabled()
        {
            try
            {
                string value =
                    System.Configuration.ConfigurationManager
                        .AppSettings["IncludeLocalVoice"];

                return string.Equals(
                    value,
                    "true",
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public MainWindow()
        {
            InitializeComponent();

            ChatScrollViewer.PreviewMouseWheel +=
                ChatScrollViewer_PreviewMouseWheel;


            // =====================================================
            // PRIVACY MANAGER
            // =====================================================

            _privacyManager =
                new PrivacyManager(this);


            // =====================================================
            // REGISTER MAIN WINDOW
            // =====================================================

            AppearanceManager.RegisterWindow(this);


            // =====================================================
            // SETTINGS MANAGER
            // =====================================================

            settingsManager =
                new SettingsManager(this);


            // =====================================================
            // LOAD SETTINGS
            // =====================================================

            currentSettings =
                SettingsService.Load()
                ?? new AppSettings();

            // =====================================================
            // CHATGPT VIEW MODE
            // =====================================================

            _chatGPTView =
                string.Equals(
                    System.Configuration.ConfigurationManager
                        .AppSettings["ChatGPTView"],
                    "true",
                    StringComparison.OrdinalIgnoreCase);

            // =====================================================
            // SMART ANSWER STATE
            // =====================================================

            if (SmartAnswerButton != null)
            {
                SmartAnswerButton.IsChecked =
                    currentSettings.SmartAnswerEnabled;
            }


            // =====================================================
            // APPLY APPEARANCE
            // =====================================================

            AppearanceManager.Apply(
                this,
                currentSettings);


            // =====================================================
            // INITIAL STEALTH STATE
            // =====================================================

            _hideFromCapture =
                ScreenCaptureProtection.IsEnabled;

            if (HideToggleButton != null)
            {
                HideToggleButton.IsChecked =
                    _hideFromCapture;
            }


            // =====================================================
            // CLEAR CONVERSATION
            // =====================================================

            ClearConversation();


            // =====================================================
            // WINDOW EVENTS
            // =====================================================

            MouseLeftButtonDown +=
                MainWindow_MouseLeftButtonDown;

            SourceInitialized +=
                MainWindow_SourceInitialized;

            Closed +=
                MainWindow_Closed;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(
     object sender,
     RoutedEventArgs e)
        {
            try
            {
                if (_chatGPTView)
                {
                    ChatScrollViewer.Visibility =
                        Visibility.Collapsed;

                    ChatGPTWebViewHost.Visibility =
                        Visibility.Visible;

                    ChatGPTWebViewHost.ChatGPTReady +=
                        ChatGPTWebViewHost_ChatGPTReady;

                    ChatGPTWebViewHost.FocusChatGPT();
                }
                else
                {
                    ChatGPTWebViewHost.Visibility =
                        Visibility.Collapsed;

                    ChatScrollViewer.Visibility =
                        Visibility.Visible;
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // TEMPORARY CHATGPT INJECTION TEST
        // =========================================================

        private async void ChatGPTWebViewHost_ChatGPTReady(
            object sender,
            EventArgs e)
        {
            await ChatGPTWebViewHost.InjectQuestionAsync(
                "Hello, this is a test question");
        }


        // =========================================================
        // CHAT MOUSE WHEEL SCROLL
        // =========================================================

        private void ChatScrollViewer_PreviewMouseWheel(
            object sender,
            MouseWheelEventArgs e)
        {
            try
            {
                if (ChatScrollViewer == null ||
                    !ChatScrollViewer.IsLoaded)
                    return;

                const double SCROLL_DISTANCE = 70.0;

                double currentOffset =
                    ChatScrollViewer.VerticalOffset;

                double targetOffset =
                    currentOffset +
                    (e.Delta > 0
                        ? -SCROLL_DISTANCE
                        : SCROLL_DISTANCE);

                targetOffset =
                    Math.Max(
                        0,
                        Math.Min(
                            targetOffset,
                            ChatScrollViewer.ScrollableHeight));

                StartSmoothChatScroll(
                    targetOffset);

                e.Handled = true;
            }
            catch
            {
            }
        }


        // =========================================================
        // SOURCE INITIALIZED
        // =========================================================

        private void MainWindow_SourceInitialized(
            object sender,
            EventArgs e)
        {
            WindowBehaviorManager.Attach(this);

            try
            {
                HotKeysRegister.Register(
                    this,

                    // =================================================
                    // CTRL + \
                    // =================================================

                    () =>
                    {
                        try
                        {
                            // =============================================
                            // SETTINGS WINDOW OPEN
                            // =============================================

                            if (settingsManager != null &&
                                settingsManager.IsSettingsVisible)
                            {
                                return;
                            }


                            // =============================================
                            // TOGGLE MAIN WINDOW
                            // =============================================

                            if (IsVisible)
                            {
                                Hide();

                                try
                                {
                                    StopWindowMoveAnimation();
                                }
                                catch
                                {
                                }

                                return;
                            }


                            // =============================================
                            // SHOW MAIN WINDOW
                            // =============================================

                            Show();

                            WindowState =
                                WindowState.Normal;

                            Topmost = true;

                            Activate();

                            Focus();

                            ChatGPTWebViewHost.FocusChatGPT();
                        }
                        catch
                        {
                        }
                    },


                   // =================================================
                   // CTRL + LEFT
                   // =================================================

                   () =>
                   {
                       try
                       {
                           if (settingsManager != null &&
                                settingsManager.IsSettingsVisible)
                           {
                               SettingsWindow settingsWindow =
                                   settingsManager.CurrentSettingsWindow;

                               if (settingsWindow != null &&
                                   settingsWindow.IsVisible)
                               {
                                   WindowBehaviorManager.MoveSettingsWindow(
                                       settingsWindow,
                                       HotKeysRegister.MOVE_LEFT_HOTKEY_ID);

                                   return;
                               }
                           }

                           WindowBehaviorManager.MoveMainWindow(
                               this,
                               HotKeysRegister.MOVE_LEFT_HOTKEY_ID);
                       }
                       catch
                       {
                       }
                   },


                    // =================================================
                    // CTRL + RIGHT
                    // =================================================

                    () =>
                    {
                        try
                        {

                            if (settingsManager != null &&
                                    settingsManager.IsSettingsVisible)
                            {
                                SettingsWindow settingsWindow =
                                    settingsManager.CurrentSettingsWindow;

                                if (settingsWindow != null &&
                                    settingsWindow.IsVisible)
                                {
                                    WindowBehaviorManager.MoveSettingsWindow(
                                        settingsWindow,
                                        HotKeysRegister.MOVE_RIGHT_HOTKEY_ID);

                                    return;
                                }
                            }

                            WindowBehaviorManager.MoveMainWindow(
                               this,
                               HotKeysRegister.MOVE_RIGHT_HOTKEY_ID);
                        }
                        catch
                        {
                        }
                    },


                    // =================================================
                    // CTRL + UP
                    // =================================================

                    () =>
                    {
                        try
                        {

                            if (settingsManager != null &&
                                    settingsManager.IsSettingsVisible)
                            {
                                SettingsWindow settingsWindow =
                                    settingsManager.CurrentSettingsWindow;

                                if (settingsWindow != null &&
                                    settingsWindow.IsVisible)
                                {
                                    WindowBehaviorManager.MoveSettingsWindow(
                                        settingsWindow,
                                        HotKeysRegister.MOVE_UP_HOTKEY_ID);

                                    return;
                                }
                            }

                            WindowBehaviorManager.MoveMainWindow(
                                this,
                                HotKeysRegister.MOVE_UP_HOTKEY_ID);
                        }
                        catch
                        {
                        }
                    },


                   // =================================================
                   // CTRL + DOWN
                   // =================================================

                   () =>
                   {
                       try
                       {

                           if (settingsManager != null &&
                                settingsManager.IsSettingsVisible)
                           {
                               SettingsWindow settingsWindow =
                                   settingsManager.CurrentSettingsWindow;

                               if (settingsWindow != null &&
                                   settingsWindow.IsVisible)
                               {
                                   WindowBehaviorManager.MoveSettingsWindow(
                                       settingsWindow,
                                       HotKeysRegister.MOVE_DOWN_HOTKEY_ID);

                                   return;
                               }
                           }

                           WindowBehaviorManager.MoveMainWindow(
                               this,
                               HotKeysRegister.MOVE_DOWN_HOTKEY_ID);
                       }
                       catch
                       {
                       }
                   },


                    // =================================================
                    // ALT + UP
                    // =================================================

                    () =>
                    {
                        try
                        {
                            ScrollChatWindow(
                                HotKeysRegister
                                    .SCROLL_UP_HOTKEY_ID);
                        }
                        catch
                        {
                        }
                    },


                    // =================================================
                    // ALT + DOWN
                    // =================================================

                    () =>
                    {
                        try
                        {
                            ScrollChatWindow(
                                HotKeysRegister
                                    .SCROLL_DOWN_HOTKEY_ID);
                        }
                        catch
                        {
                        }
                    },


                    // =================================================
                    // ESC
                    // =================================================

                    () =>
                    {
                        try
                        {
                            if (settingsManager != null &&
                                settingsManager.IsSettingsVisible)
                            {
                                settingsManager.CloseSettings();
                                return;
                            }

                            if (IsVisible)
                            {
                                Close();
                            }
                        }
                        catch
                        {
                        }
                    },


                    // =================================================
                    // SPACE
                    // =================================================

                    () =>
                    {
                        try
                        {
                            if (!IsVoiceInputEnabled()) 
                                return; 
                            
                            if (voiceRecorder == null) 
                            { 
                                StartVoiceRecording();
                            } 
                            else 
                            { 
                                StopVoiceRecording(); 
                            }
                        }
                        catch
                        {
                        }
                    },


                    // =================================================
                    // ALT + ENTER
                    // VISION AI
                    // =================================================

                    () =>
                    {
                        try
                        {
                            RunVisionAiFromHotkey();
                        }
                        catch
                        {
                        }
                    },


                    // =================================================
                    // CTRL + ENTER
                    // =================================================

                    () =>
                    {
                        try
                        {
                            _ = SendQuestion();
                        }
                        catch
                        {
                        }
                    },


                    // =================================================
                    // CTRL + SHIFT + \
                    // SETTINGS
                    // =================================================

                    () =>
                    {
                        try
                        {
                            ToggleSettingsWindow();
                        }
                        catch
                        {
                        }
                    },


                    // =================================================
                    // MESSAGE MODE
                    // =================================================

                    () =>
                    {
                        try
                        {
                            if (MessageModeButton == null)
                                return;

                            MessageModeButton.IsChecked =
                                !MessageModeButton.IsChecked;
                        }
                        catch
                        {
                        }
                    },

                    // =================================================
                    // SMART ANSWER ON / OFF
                    // CTRL + SHIFT + S
                    // =================================================

                    () =>
                    {
                        try
                        {
                            if (SmartAnswerButton == null)
                                return;

                            SmartAnswerButton.IsChecked =
                                !SmartAnswerButton.IsChecked;
                        }
                        catch
                        {
                        }
                    },

                     // =================================================
                     // CLEAR CHAT
                     // CTRL + SHIFT + BACKSPACE
                     // =================================================

                     () =>
                     {
                         try
                         {
                             if (isGenerating &&
                                 cancellationTokenSource != null)
                             {
                                 cancellationTokenSource.Cancel();
                             }

                             ClearConversation();
                         }
                         catch
                         {
                         }
                     }
                );


                // =====================================================
                // PRIVACY INITIALIZATION
                // =====================================================

                if (_privacyManager != null)
                {
                    // =================================================
                    // READ STEALTH STATE FROM APP.CONFIG
                    // =================================================

                    bool stealthEnabled =
                        ScreenCaptureProtection.IsEnabled;


                    _hideFromCapture =
                        stealthEnabled;


                    if (HideToggleButton != null)
                    {
                        HideToggleButton.IsChecked =
                            stealthEnabled;
                    }


                    // =================================================
                    // STEALTH MODE ON
                    // =================================================

                    if (stealthEnabled)
                    {
                        bool result =
                            _privacyManager
                                .EnableScreenCaptureProtection();


                        if (!result)
                        {
                            _hideFromCapture = false;

                            if (HideToggleButton != null)
                            {
                                HideToggleButton.IsChecked =
                                    false;
                            }

                            ScreenCaptureProtection
                                .ApplyToAllWindows(false);
                        }
                        else
                        {
                            ScreenCaptureProtection
                                .ApplyToAllWindows(true);

                            ApplyPrivacyToSettingsWindow();
                        }
                    }


                    // =================================================
                    // STEALTH MODE OFF
                    // =================================================

                    else
                    {
                        _privacyManager
                            .DisableScreenCaptureProtection();


                        ScreenCaptureProtection
                            .ApplyToAllWindows(false);


                        ApplyPrivacyToSettingsWindow();
                    }
                }
            }
            catch
            {
                _hideFromCapture = false;

                if (HideToggleButton != null)
                {
                    HideToggleButton.IsChecked =
                        false;
                }
            }
            finally
            {
                _initializingPrivacy = false;
            }
        }


        // =========================================================
        // ALT + ENTER ENTRY POINT
        // =========================================================

        private async void RunVisionAiFromHotkey()
        {
            try
            {
                if (!TryStartVisionRequest())
                {
                    return;
                }

                await RunVisionAiFromScreenAsync();
            }
            catch
            {
            }
            finally
            {
                FinishVisionRequest();
            }
        }


        // =========================================================
        // ACTIVE WINDOW MOVEMENT
        // =========================================================

        private void MoveActiveWindow1(
            int hotkeyId)
        {
            try
            {
                if (settingsManager != null &&
                    settingsManager.IsSettingsVisible)
                {
                    SettingsWindow settingsWindow =
                        settingsManager.CurrentSettingsWindow;

                    if (settingsWindow != null &&
                        settingsWindow.IsVisible)
                    {
                        MoveSettingsWindow(
                            settingsWindow,
                            hotkeyId);

                        return;
                    }
                }

                MoveMainWindow(
                    hotkeyId);
            }
            catch
            {
            }
        }


        // =========================================================
        // MOVE SETTINGS WINDOW
        // =========================================================

        private void MoveSettingsWindow(
            SettingsWindow settingsWindow,
            int hotkeyId)
        {
            try
            {
                if (settingsWindow == null ||
                    !settingsWindow.IsVisible)
                    return;

                if (settingsWindow.WindowState ==
                    WindowState.Minimized)
                    return;

                double windowWidth =
                    settingsWindow.ActualWidth;

                double windowHeight =
                    settingsWindow.ActualHeight;

                if (double.IsNaN(windowWidth) ||
                    windowWidth <= 0)
                {
                    windowWidth =
                        settingsWindow.Width;
                }

                if (double.IsNaN(windowHeight) ||
                    windowHeight <= 0)
                {
                    windowHeight =
                        settingsWindow.Height;
                }

                double currentLeft =
                    settingsWindow.Left;

                double currentTop =
                    settingsWindow.Top;

                if (double.IsNaN(currentLeft))
                    currentLeft = 0;

                if (double.IsNaN(currentTop))
                    currentTop = 0;

                double targetLeft =
                    currentLeft;

                double targetTop =
                    currentTop;


                switch (hotkeyId)
                {
                    case HotKeysRegister
                        .MOVE_LEFT_HOTKEY_ID:

                        targetLeft -=
                            WINDOW_MOVE_DISTANCE;

                        break;


                    case HotKeysRegister
                        .MOVE_RIGHT_HOTKEY_ID:

                        targetLeft +=
                            WINDOW_MOVE_DISTANCE;

                        break;


                    case HotKeysRegister
                        .MOVE_UP_HOTKEY_ID:

                        targetTop -=
                            WINDOW_MOVE_DISTANCE;

                        break;


                    case HotKeysRegister
                        .MOVE_DOWN_HOTKEY_ID:

                        targetTop +=
                            WINDOW_MOVE_DISTANCE;

                        break;


                    default:
                        return;
                }


                System.Windows.Forms.Screen screen =
                    System.Windows.Forms.Screen
                        .FromHandle(
                            new WindowInteropHelper(
                                settingsWindow).Handle);


                System.Drawing.Rectangle workArea =
                    screen.WorkingArea;


                double screenLeft =
                    workArea.Left;

                double screenTop =
                    workArea.Top;

                double screenRight =
                    workArea.Right;

                double screenBottom =
                    workArea.Bottom;


                double maxLeft =
                    screenRight - windowWidth;

                double maxTop =
                    screenBottom - windowHeight;


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


                settingsWindow.Left =
                    targetLeft;

                settingsWindow.Top =
                    targetTop;
            }
            catch
            {
            }
        }


        // =========================================================
        // OCR INITIALIZATION
        // =========================================================

        private void InitializeOCR()
        {
            try
            {
                string tessDataPath =
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "tessdata");

                ocrEngine =
                    new TesseractEngine(
                        tessDataPath,
                        "eng",
                        EngineMode.Default);
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "OCR initialization error:\n\n" +
                    ex.Message);
            }
        }


        // =========================================================
        // HIDE
        // =========================================================

        private void HideToggleButton_Checked(
            object sender,
            RoutedEventArgs e)
        {
            if (_initializingPrivacy)
                return;

            try
            {
                // =====================================================
                // STEALTH MODE IS CONFIGURED IN APP.CONFIG
                // =====================================================

                if (!ScreenCaptureProtection.IsEnabled)
                {
                    if (HideToggleButton != null)
                    {
                        HideToggleButton.IsChecked =
                            false;
                    }

                    return;
                }


                if (_privacyManager == null)
                    return;


                bool result =
                    _privacyManager
                        .EnableScreenCaptureProtection();


                if (!result)
                {
                    _hideFromCapture = false;

                    if (HideToggleButton != null)
                    {
                        HideToggleButton.IsChecked =
                            false;
                    }

                    AppMessage.Show(
                        "Unable to enable screen capture protection.");

                    return;
                }


                _hideFromCapture = true;


                // =====================================================
                // APPLY PRIVACY TO COMPLETE APPLICATION
                // =====================================================

                ScreenCaptureProtection
                    .ApplyToAllWindows(true);

                ApplyPrivacyToSettingsWindow();
            }
            catch (Exception ex)
            {
                _hideFromCapture = false;

                if (HideToggleButton != null)
                {
                    HideToggleButton.IsChecked =
                        false;
                }

                AppMessage.Show(
                    "Hide mode error:\n\n" +
                    ex.Message);
            }
        }


        // =========================================================
        // SHOW
        // =========================================================

        private void HideToggleButton_Unchecked(
            object sender,
            RoutedEventArgs e)
        {
            if (_initializingPrivacy)
                return;

            try
            {
                _hideFromCapture = false;


                if (_privacyManager != null)
                {
                    _privacyManager
                        .DisableScreenCaptureProtection();
                }


                // =====================================================
                // RESTORE PRIVACY/UI FOR COMPLETE APPLICATION
                // =====================================================

                ScreenCaptureProtection
                    .ApplyToAllWindows(false);

                ApplyPrivacyToSettingsWindow();
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "Show mode error:\n\n" +
                    ex.Message);
            }
        }


        // =========================================================
        // PUBLIC HIDE STATE
        // =========================================================

        public bool IsHideFromCaptureEnabled
        {
            get
            {
                return _hideFromCapture;
            }
        }


        // =========================================================
        // REGISTER SETTINGS WINDOW
        // =========================================================

        public void RegisterSettingsWindow(
            SettingsWindow settingsWindow)
        {
            try
            {
                _currentSettingsWindow =
                    settingsWindow;

                ApplyPrivacyToSettingsWindow();
            }
            catch
            {
            }
        }


        // =========================================================
        // UNREGISTER SETTINGS WINDOW
        // =========================================================

        public void UnregisterSettingsWindow(
            SettingsWindow settingsWindow)
        {
            try
            {
                if (_currentSettingsWindow ==
                    settingsWindow)
                {
                    _currentSettingsWindow =
                        null;
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // APPLY PRIVACY
        // =========================================================

        private void ApplyPrivacyToSettingsWindow()
        {
            try
            {
                if (_currentSettingsWindow == null)
                    return;

                if (!_currentSettingsWindow.IsVisible)
                    return;

                _currentSettingsWindow
                    .ApplyScreenCaptureProtection(
                        _hideFromCapture);
            }
            catch
            {
            }
        }


        // =========================================================
        // REFRESH PRIVACY
        // =========================================================

        public void RefreshPrivacyProtection()
        {
            try
            {
                if (_privacyManager == null)
                    return;


                if (_hideFromCapture)
                {
                    _privacyManager
                        .EnableScreenCaptureProtection();


                    ScreenCaptureProtection
                        .ApplyToAllWindows(true);
                }
                else
                {
                    _privacyManager
                        .DisableScreenCaptureProtection();


                    ScreenCaptureProtection
                        .ApplyToAllWindows(false);
                }


                ApplyPrivacyToSettingsWindow();
            }
            catch
            {
            }
        }


        // =========================================================
        // SETTINGS BUTTON
        // =========================================================

        private void SettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                settingsManager.ShowSettings();

                Dispatcher.BeginInvoke(
                    new Action(
                        ApplyPrivacyToSettingsWindow));
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "Settings window error:\n\n" +
                    ex.Message);
            }
        }


        // =========================================================
        // TOGGLE SETTINGS
        // =========================================================

        private void ToggleSettingsWindow()
        {
            try
            {
                if (settingsManager == null)
                    return;


                if (settingsManager.IsSettingsVisible)
                {
                    settingsManager.CloseSettings();
                    return;
                }


                if (!IsVisible)
                    return;


                settingsManager.ShowSettings();


                Dispatcher.BeginInvoke(
                    new Action(
                        ApplyPrivacyToSettingsWindow));
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "Settings window error:\n\n" +
                    ex.Message);
            }
        }


        // =========================================================
        // REFRESH GENERAL SETTINGS
        // =========================================================

        public void RefreshGeneralSettings()
        {
            try
            {
                currentSettings =
                    SettingsService.Load()
                    ?? new AppSettings();
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "General settings refresh error:\n\n" +
                    ex.Message);
            }
        }


        // =========================================================
        // WINDOW DRAG
        // =========================================================

        private void MainWindow_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton ==
                    MouseButton.Left)
                {
                    DragMove();
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // EXIT
        // =========================================================

        private void ExitButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                System.Windows.Application.Current
                    ?.Shutdown();
            }
            catch
            {
            }
        }


        // =========================================================
        // MESSAGE MODE ON
        // =========================================================

        private void MessageModeButton_Checked(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (TextInputPanel != null)
                {
                    TextInputPanel.Visibility =
                        Visibility.Visible;
                }


                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        try
                        {
                            if (QuestionTextBox == null)
                                return;


                            Activate();


                            QuestionTextBox.Focus();


                            Keyboard.Focus(
                                QuestionTextBox);


                            QuestionTextBox.CaretIndex =
                                QuestionTextBox.Text.Length;
                        }
                        catch
                        {
                        }
                    }),
                    DispatcherPriority.Input);
            }
            catch
            {
            }
        }


        // =========================================================
        // MESSAGE MODE OFF
        // =========================================================

        private void MessageModeButton_Unchecked(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (TextInputPanel != null)
                {
                    TextInputPanel.Visibility =
                        Visibility.Collapsed;
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // SMART ANSWER ON
        // =========================================================

        private void SmartAnswerButton_Checked(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                _smartAnswerEnabled = true;
            }
            catch
            {
            }
        }


        // =========================================================
        // SMART ANSWER OFF
        // =========================================================

        private void SmartAnswerButton_Unchecked(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                _smartAnswerEnabled = false;
            }
            catch
            {
            }
        }

        // =========================================================
        // SMART ANSWER ON
        // =========================================================

        private void SmartAnswerToggleButton_Checked(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                AppSettings settings =
                    SettingsService.Load()
                    ?? new AppSettings();

                settings.SmartAnswerEnabled = true;

                SettingsService.Save(settings);

                currentSettings = settings;

                Debug.WriteLine(
                    "SMART ANSWER: ON");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "SMART ANSWER ON ERROR: " +
                    ex.ToString());
            }
        }


        // =========================================================
        // SMART ANSWER OFF
        // =========================================================

        private void SmartAnswerToggleButton_Unchecked(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                AppSettings settings =
                    SettingsService.Load()
                    ?? new AppSettings();

                settings.SmartAnswerEnabled = false;

                SettingsService.Save(settings);

                currentSettings = settings;

                Debug.WriteLine(
                    "SMART ANSWER: OFF");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "SMART ANSWER OFF ERROR: " +
                    ex.ToString());
            }
        }

        // =========================================================
        // CLEAR CHAT HOTKEY
        // =========================================================

        private void ClearChatFromHotkey()
        {
            try
            {
                if (isGenerating &&
                    cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                }

                ClearConversation();
            }
            catch
            {
            }
        }
    }
}