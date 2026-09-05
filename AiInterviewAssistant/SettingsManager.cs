using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AiInterviewAssistant
{
    public class SettingsManager
    {
        private readonly MainWindow _mainWindow;

        private SettingsWindow _settingsWindow;

        private bool _isClosing;

        private bool _isAnimating;


        // =========================================================
        // SETTINGS POSITION
        // =========================================================

        private double _settingsLastLeft;

        private double _settingsLastTop;


        // =========================================================
        // POSITION VALID
        // =========================================================

        private bool _hasSettingsPosition;


        // =========================================================
        // CURRENT SETTINGS WINDOW
        // =========================================================

        public SettingsWindow CurrentSettingsWindow
        {
            get
            {
                return _settingsWindow;
            }
        }


        // =========================================================
        // SETTINGS VISIBLE
        // =========================================================

        public bool IsSettingsVisible
        {
            get
            {
                return _settingsWindow != null &&
                       _settingsWindow.IsVisible;
            }
        }


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public SettingsManager(
            MainWindow mainWindow)
        {
            _mainWindow =
                mainWindow;
        }


        // =========================================================
        // TOGGLE
        // =========================================================

        public void ToggleSettings()
        {
            try
            {
                if (_mainWindow == null)
                    return;


                if (IsSettingsVisible)
                {
                    CloseSettings();
                }
                else
                {
                    ShowSettings();
                }
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "Settings toggle error:\n\n" +
                    ex.Message);
            }
        }


        // =========================================================
        // SHOW SETTINGS
        // =========================================================

        public void ShowSettings()
        {
            try
            {
                if (_mainWindow == null)
                    return;


                // =================================================
                // ALREADY OPEN
                // =================================================

                if (_settingsWindow != null &&
                    _settingsWindow.IsVisible)
                {
                    PositionSettingsWindow();

                    _settingsWindow.Activate();

                    _settingsWindow.Focus();

                    return;
                }


                // =================================================
                // CREATE SETTINGS WINDOW
                // =================================================

                if (_settingsWindow == null)
                {
                    _settingsWindow =
                        new SettingsWindow(
                            _mainWindow);


                    // =================================================
                    // CLOSE REQUEST
                    // =================================================

                    _settingsWindow.CloseRequested +=
                        SettingsWindow_CloseRequested;


                    // =================================================
                    // SAVE REQUEST
                    // =================================================

                    _settingsWindow.SaveRequested +=
                        SettingsWindow_SaveRequested;


                    // =================================================
                    // CLOSED
                    // =================================================

                    _settingsWindow.Closed +=
                        SettingsWindow_Closed;


                    // =================================================
                    // REGISTER
                    // =================================================

                    _mainWindow.RegisterSettingsWindow(
                        _settingsWindow);
                }


                // =================================================
                // POSITION
                // =================================================

                PositionSettingsWindow();


                // =================================================
                // PRIVACY
                // =================================================

                _settingsWindow
                    .ApplyScreenCaptureProtection(
                        _mainWindow
                            .IsHideFromCaptureEnabled);


                // =================================================
                // HIDE MAIN
                // =================================================

                _mainWindow.Hide();


                // =================================================
                // SHOW SETTINGS
                // =================================================

                _settingsWindow.Show();

                _settingsWindow.WindowState =
                    WindowState.Normal;

                _settingsWindow.Activate();

                _settingsWindow.Focus();


                // =================================================
                // POSITION AGAIN AFTER SHOW
                // =================================================

                PositionSettingsWindow();
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "Unable to open Settings:\n\n" +
                    ex.Message);
            }
        }


        // =========================================================
        // POSITION SETTINGS WINDOW
        // =========================================================

        private void PositionSettingsWindow()
        {
            try
            {
                if (_mainWindow == null ||
                    _settingsWindow == null)
                {
                    return;
                }


                _settingsWindow.WindowStartupLocation =
                    WindowStartupLocation.Manual;


                // =================================================
                // RESTORE LAST SETTINGS POSITION
                // =================================================

                if (_hasSettingsPosition &&
                    !double.IsNaN(_settingsLastLeft) &&
                    !double.IsNaN(_settingsLastTop))
                {
                    _settingsWindow.Left =
                        _settingsLastLeft;

                    _settingsWindow.Top =
                        _settingsLastTop;
                }
                else
                {
                    // =================================================
                    // FIRST OPEN = MAIN WINDOW POSITION
                    // =================================================

                    double mainLeft =
                        _mainWindow.Left;

                    double mainTop =
                        _mainWindow.Top;


                    if (double.IsNaN(mainLeft))
                        mainLeft = 0;

                    if (double.IsNaN(mainTop))
                        mainTop = 0;


                    _settingsWindow.Left =
                        mainLeft;

                    _settingsWindow.Top =
                        mainTop;
                }


                // =================================================
                // SAME SIZE AS MAIN WINDOW
                // =================================================

                if (_mainWindow.Width > 0)
                {
                    _settingsWindow.Width =
                        _mainWindow.Width;
                }


                if (_mainWindow.Height > 0)
                {
                    _settingsWindow.Height =
                        _mainWindow.Height;
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // MOVE SETTINGS WINDOW
        //
        // Ctrl + Arrow
        // =========================================================

        public void MoveSettingsWindow(
            int hotkeyId)
        {
            try
            {
                if (_settingsWindow == null)
                    return;


                if (!_settingsWindow.IsVisible)
                    return;


                if (_settingsWindow.WindowState ==
                    WindowState.Minimized)
                {
                    return;
                }


                // =================================================
                // CURRENT POSITION
                // =================================================

                double currentLeft =
                    _settingsWindow.Left;

                double currentTop =
                    _settingsWindow.Top;


                if (double.IsNaN(currentLeft))
                    currentLeft = 0;


                if (double.IsNaN(currentTop))
                    currentTop = 0;


                // =================================================
                // SIZE
                // =================================================

                double width =
                    _settingsWindow.ActualWidth;

                double height =
                    _settingsWindow.ActualHeight;


                if (double.IsNaN(width) ||
                    width <= 0)
                {
                    width =
                        _settingsWindow.Width;
                }


                if (double.IsNaN(height) ||
                    height <= 0)
                {
                    height =
                        _settingsWindow.Height;
                }


                // =================================================
                // WORK AREA
                // =================================================

                double screenLeft =
                    SystemParameters.WorkArea.Left;

                double screenTop =
                    SystemParameters.WorkArea.Top;

                double screenRight =
                    SystemParameters.WorkArea.Right;

                double screenBottom =
                    SystemParameters.WorkArea.Bottom;


                double maxLeft =
                    screenRight -
                    width;

                double maxTop =
                    screenBottom -
                    height;


                if (maxLeft < screenLeft)
                    maxLeft = screenLeft;


                if (maxTop < screenTop)
                    maxTop = screenTop;


                // =================================================
                // TARGET
                // =================================================

                double targetLeft =
                    currentLeft;

                double targetTop =
                    currentTop;


                const double distance = 50.0;


                switch (hotkeyId)
                {
                    case HotKeysRegister.MOVE_LEFT_HOTKEY_ID:

                        targetLeft -= distance;

                        break;


                    case HotKeysRegister.MOVE_RIGHT_HOTKEY_ID:

                        targetLeft += distance;

                        break;


                    case HotKeysRegister.MOVE_UP_HOTKEY_ID:

                        targetTop -= distance;

                        break;


                    case HotKeysRegister.MOVE_DOWN_HOTKEY_ID:

                        targetTop += distance;

                        break;


                    default:

                        return;
                }


                // =================================================
                // LIMIT TO SCREEN
                // =================================================

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
                // APPLY
                //
                // Settings window is small and direct movement
                // is more reliable than sharing MainWindow's
                // CompositionTarget animation state.
                // =================================================

                _settingsWindow.Left =
                    targetLeft;

                _settingsWindow.Top =
                    targetTop;


                // =================================================
                // SAVE POSITION
                // =================================================

                SaveSettingsPosition();
            }
            catch
            {
            }
        }


        // =========================================================
        // SAVE SETTINGS WINDOW POSITION
        // =========================================================

        private void SaveSettingsPosition()
        {
            try
            {
                if (_settingsWindow == null)
                    return;


                if (double.IsNaN(
                        _settingsWindow.Left))
                {
                    return;
                }


                if (double.IsNaN(
                        _settingsWindow.Top))
                {
                    return;
                }


                _settingsLastLeft =
                    _settingsWindow.Left;

                _settingsLastTop =
                    _settingsWindow.Top;

                _hasSettingsPosition =
                    true;
            }
            catch
            {
            }
        }


        // =========================================================
        // CLOSE SETTINGS
        // =========================================================

        public void CloseSettings()
        {
            try
            {
                if (_settingsWindow == null)
                {
                    ShowMainWindow();

                    return;
                }


                if (_settingsWindow.IsVisible)
                {
                    if (_isClosing)
                        return;


                    // =================================================
                    // VERY IMPORTANT
                    //
                    // Capture SettingsWindow position BEFORE Close()
                    // =================================================

                    SaveSettingsPosition();


                    // =================================================
                    // COPY SETTINGS POSITION TO MAIN WINDOW
                    //
                    // This guarantees that when Settings closes,
                    // MainWindow comes back EXACTLY here.
                    // =================================================

                    ApplySettingsPositionToMainWindow();


                    _isClosing = true;

                    _isAnimating = true;


                    // =================================================
                    // STOP MAIN WINDOW MOVEMENT
                    // =================================================

                    if (_mainWindow != null)
                    {
                        _mainWindow
                            .StopWindowMoveAnimation();
                    }


                    // =================================================
                    // CLOSE SETTINGS
                    // =================================================

                    _settingsWindow.Close();


                    // =================================================
                    // SHOW MAIN WINDOW
                    // =================================================

                    FadeMainWindowIn();
                }
                else
                {
                    ShowMainWindow();
                }
            }
            catch
            {
                _isClosing = false;
                _isAnimating = false;

                ShowMainWindow();
            }
        }


        // =========================================================
        // APPLY SETTINGS POSITION TO MAIN WINDOW
        // =========================================================

        private void ApplySettingsPositionToMainWindow()
        {
            try
            {
                if (_mainWindow == null)
                    return;


                if (!_hasSettingsPosition)
                    return;


                if (double.IsNaN(_settingsLastLeft) ||
                    double.IsNaN(_settingsLastTop))
                {
                    return;
                }


                _mainWindow.Left =
                    _settingsLastLeft;

                _mainWindow.Top =
                    _settingsLastTop;
            }
            catch
            {
            }
        }


        // =========================================================
        // SAVE REQUESTED
        // =========================================================

        private void SettingsWindow_SaveRequested(
            object sender,
            EventArgs e)
        {
            try
            {
                if (_mainWindow != null)
                {
                    _mainWindow.RefreshAppearanceSettings();

                    _mainWindow.RefreshPrivacyProtection();
                }


                AppSettings latestSettings =
                    SettingsService.Load()
                    ?? new AppSettings();


                AppearanceManager.RefreshAllWindows(
                    latestSettings);


                // =================================================
                // SAVE POSITION BEFORE CLOSE
                // =================================================

                SaveSettingsPosition();


                // =================================================
                // CLOSE
                // =================================================

                CloseSettings();
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "Settings apply error:\n\n" +
                    ex.Message);

                _isClosing = false;
                _isAnimating = false;
            }
        }


        // =========================================================
        // CLOSE REQUESTED
        // =========================================================

        private void SettingsWindow_CloseRequested(
            object sender,
            EventArgs e)
        {
            CloseSettings();
        }


        // =========================================================
        // SETTINGS CLOSED
        // =========================================================

        private void SettingsWindow_Closed(
            object sender,
            EventArgs e)
        {
            try
            {
                SettingsWindow closedWindow =
                    sender as SettingsWindow;


                // =================================================
                // SAVE FINAL POSITION
                // =================================================

                if (closedWindow != null)
                {
                    if (!double.IsNaN(
                            closedWindow.Left) &&
                        !double.IsNaN(
                            closedWindow.Top))
                    {
                        _settingsLastLeft =
                            closedWindow.Left;

                        _settingsLastTop =
                            closedWindow.Top;

                        _hasSettingsPosition =
                            true;
                    }
                }


                // =================================================
                // UNREGISTER
                // =================================================

                if (_mainWindow != null &&
                    closedWindow != null)
                {
                    _mainWindow
                        .UnregisterSettingsWindow(
                            closedWindow);
                }


                // =================================================
                // CLEAR
                // =================================================

                if (sender == _settingsWindow)
                {
                    _settingsWindow = null;
                }


                // =================================================
                // RESTORE MAIN
                // =================================================

                if (!_isAnimating)
                {
                    ApplySettingsPositionToMainWindow();

                    ShowMainWindow();
                }
            }
            catch
            {
                ShowMainWindow();
            }
        }


        // =========================================================
        // FADE MAIN WINDOW IN
        // =========================================================

        private void FadeMainWindowIn()
        {
            try
            {
                if (_mainWindow == null)
                    return;


                AppSettings settings =
                    SettingsService.Load()
                    ?? new AppSettings();


                double targetOpacity =
                    settings.Opacity;


                if (double.IsNaN(targetOpacity) ||
                    double.IsInfinity(targetOpacity))
                {
                    targetOpacity = 0.85;
                }


                if (targetOpacity < 0.5)
                    targetOpacity = 0.5;


                if (targetOpacity > 1.0)
                    targetOpacity = 1.0;


                // =================================================
                // MAKE SURE POSITION IS CORRECT
                // =================================================

                ApplySettingsPositionToMainWindow();


                // =================================================
                // STOP OLD ANIMATION
                // =================================================

                _mainWindow.BeginAnimation(
                    Window.OpacityProperty,
                    null);


                _mainWindow.Opacity =
                    0.0;


                _mainWindow.WindowState =
                    WindowState.Normal;


                _mainWindow.Show();

                _mainWindow.Activate();

                _mainWindow.Focus();


                // =================================================
                // RENDER FIRST
                // =================================================

                _mainWindow.Dispatcher.BeginInvoke(
                    new Action(
                        () =>
                        {
                            try
                            {
                                double duration =
                                    AppearanceManager
                                        .GetAnimationDurationMilliseconds(
                                            600);


                                if (double.IsNaN(duration) ||
                                    double.IsInfinity(duration) ||
                                    duration < 0)
                                {
                                    duration = 0;
                                }


                                if (duration <= 1)
                                {
                                    _mainWindow.Opacity =
                                        targetOpacity;

                                    _isClosing = false;
                                    _isAnimating = false;

                                    return;
                                }


                                DoubleAnimation fadeIn =
                                    new DoubleAnimation
                                    {
                                        From = 0.0,

                                        To = targetOpacity,

                                        Duration =
                                            new Duration(
                                                TimeSpan.FromMilliseconds(
                                                    duration)),

                                        EasingFunction =
                                            new CubicEase
                                            {
                                                EasingMode =
                                                    EasingMode.EaseOut
                                            },

                                        FillBehavior =
                                            FillBehavior.Stop
                                    };


                                fadeIn.Completed +=
                                    (s, e) =>
                                    {
                                        try
                                        {
                                            _mainWindow
                                                .BeginAnimation(
                                                    Window.OpacityProperty,
                                                    null);

                                            _mainWindow.Opacity =
                                                targetOpacity;
                                        }
                                        finally
                                        {
                                            _isClosing = false;
                                            _isAnimating = false;
                                        }
                                    };


                                _mainWindow.BeginAnimation(
                                    Window.OpacityProperty,
                                    fadeIn);
                            }
                            catch
                            {
                                _isClosing = false;
                                _isAnimating = false;

                                ShowMainWindow();
                            }
                        }),
                    DispatcherPriority.Render);
            }
            catch
            {
                _isClosing = false;
                _isAnimating = false;

                ShowMainWindow();
            }
        }


        // =========================================================
        // SHOW MAIN WINDOW
        // =========================================================

        private void ShowMainWindow()
        {
            try
            {
                if (_mainWindow == null)
                    return;


                // =================================================
                // APPLY LAST SETTINGS POSITION
                // =================================================

                ApplySettingsPositionToMainWindow();


                _mainWindow.BeginAnimation(
                    Window.OpacityProperty,
                    null);


                _mainWindow.Show();


                _mainWindow.WindowState =
                    WindowState.Normal;


                AppSettings settings =
                    SettingsService.Load()
                    ?? new AppSettings();


                double opacity =
                    settings.Opacity;


                if (double.IsNaN(opacity) ||
                    double.IsInfinity(opacity))
                {
                    opacity = 0.85;
                }


                if (opacity < 0.5)
                    opacity = 0.5;


                if (opacity > 1.0)
                    opacity = 1.0;


                _mainWindow.Opacity =
                    opacity;


                _mainWindow.Activate();

                _mainWindow.Focus();


                _isClosing = false;

                _isAnimating = false;
            }
            catch
            {
            }
        }

    }
}