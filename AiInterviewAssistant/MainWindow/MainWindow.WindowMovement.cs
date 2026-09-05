using System;
using System.Windows;
using System.Windows.Media;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // SMOOTH WINDOW MOVEMENT
        // =========================================================

        private bool _windowMoveAnimating = false;

        private double _windowMoveStartLeft;
        private double _windowMoveStartTop;

        private double _windowMoveTargetLeft;
        private double _windowMoveTargetTop;

        private DateTime _windowMoveStartTime;

        private const double WINDOW_MOVE_DISTANCE = 50.0;

        private const double WINDOW_MOVE_DURATION = 0.16;


        // =========================================================
        // MOVE CURRENT ACTIVE WINDOW
        //
        // Ctrl + Left / Right / Up / Down
        //
        // If SettingsWindow is visible:
        //     SettingsWindow moves
        //
        // Otherwise:
        //     MainWindow moves
        // =========================================================

        public void MoveCurrentWindow(
            int hotkeyId)
        {
            try
            {
                // =================================================
                // SETTINGS WINDOW VISIBLE
                // =================================================

                if (settingsManager != null &&
                    settingsManager.IsSettingsVisible)
                {
                    settingsManager.MoveSettingsWindow(
                        hotkeyId);

                    return;
                }


                // =================================================
                // MAIN WINDOW
                // =================================================

                MoveMainWindow(
                    hotkeyId);
            }
            catch
            {
            }
        }




        // =========================================================
        // MOVE MAIN WINDOW
        // =========================================================

        private void MoveMainWindow(
            int hotkeyId)
        {
            try
            {
                if (!IsVisible)
                    return;


                if (WindowState ==
                    System.Windows.WindowState.Minimized)
                {
                    return;
                }


                double windowWidth =
                    ActualWidth;

                double windowHeight =
                    ActualHeight;


                if (double.IsNaN(windowWidth) ||
                    windowWidth <= 0)
                {
                    windowWidth = Width;
                }


                if (double.IsNaN(windowHeight) ||
                    windowHeight <= 0)
                {
                    windowHeight = Height;
                }


                double currentLeft =
                    Left;

                double currentTop =
                    Top;


                if (double.IsNaN(currentLeft))
                    currentLeft = 0;


                if (double.IsNaN(currentTop))
                    currentTop = 0;


                double baseLeft =
                    _windowMoveAnimating
                        ? _windowMoveTargetLeft
                        : currentLeft;


                double baseTop =
                    _windowMoveAnimating
                        ? _windowMoveTargetTop
                        : currentTop;


                double screenLeft =
                    SystemParameters.WorkArea.Left;

                double screenTop =
                    SystemParameters.WorkArea.Top;

                double screenRight =
                    SystemParameters.WorkArea.Right;

                double screenBottom =
                    SystemParameters.WorkArea.Bottom;


                double targetLeft =
                    baseLeft;

                double targetTop =
                    baseTop;


                // =================================================
                // HOTKEY
                // =================================================

                switch (hotkeyId)
                {
                    case HotKeysRegister.MOVE_LEFT_HOTKEY_ID:

                        targetLeft -=
                            WINDOW_MOVE_DISTANCE;

                        break;


                    case HotKeysRegister.MOVE_RIGHT_HOTKEY_ID:

                        targetLeft +=
                            WINDOW_MOVE_DISTANCE;

                        break;


                    case HotKeysRegister.MOVE_UP_HOTKEY_ID:

                        targetTop -=
                            WINDOW_MOVE_DISTANCE;

                        break;


                    case HotKeysRegister.MOVE_DOWN_HOTKEY_ID:

                        targetTop +=
                            WINDOW_MOVE_DISTANCE;

                        break;


                    default:

                        return;
                }


                // =================================================
                // SCREEN BOUNDARIES
                // =================================================

                double maxLeft =
                    screenRight -
                    windowWidth;


                double maxTop =
                    screenBottom -
                    windowHeight;


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


                // =================================================
                // NOTHING TO MOVE
                // =================================================

                if (Math.Abs(
                        targetLeft -
                        currentLeft) < 0.5 &&
                    Math.Abs(
                        targetTop -
                        currentTop) < 0.5 &&
                    !_windowMoveAnimating)
                {
                    return;
                }


                // =================================================
                // START ANIMATION
                // =================================================

                StartWindowMoveAnimation(
                    targetLeft,
                    targetTop);
            }
            catch
            {
            }
        }


        // =========================================================
        // START MAIN WINDOW MOVE ANIMATION
        // =========================================================

        private void StartWindowMoveAnimation(
            double targetLeft,
            double targetTop)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(
                        new Action(
                            () =>
                            {
                                StartWindowMoveAnimation(
                                    targetLeft,
                                    targetTop);
                            }));

                    return;
                }


                double currentLeft =
                    Left;

                double currentTop =
                    Top;


                if (double.IsNaN(currentLeft))
                    currentLeft = targetLeft;


                if (double.IsNaN(currentTop))
                    currentTop = targetTop;


                // =================================================
                // START NEW ANIMATION
                // =================================================

                if (!_windowMoveAnimating)
                {
                    _windowMoveStartLeft =
                        currentLeft;

                    _windowMoveStartTop =
                        currentTop;

                    _windowMoveStartTime =
                        DateTime.UtcNow;

                    _windowMoveAnimating =
                        true;


                    CompositionTarget.Rendering +=
                        WindowMoveRendering;
                }


                // =================================================
                // UPDATE TARGET
                // =================================================

                _windowMoveTargetLeft =
                    targetLeft;

                _windowMoveTargetTop =
                    targetTop;
            }
            catch
            {
            }
        }


        // =========================================================
        // RENDERING
        // =========================================================

        private void WindowMoveRendering(
            object sender,
            EventArgs e)
        {
            try
            {
                if (!_windowMoveAnimating)
                    return;


                double elapsed =
                    (
                        DateTime.UtcNow -
                        _windowMoveStartTime
                    ).TotalSeconds;


                double progress =
                    elapsed /
                    WINDOW_MOVE_DURATION;


                // =================================================
                // COMPLETE
                // =================================================

                if (progress >= 1.0)
                {
                    Left =
                        _windowMoveTargetLeft;

                    Top =
                        _windowMoveTargetTop;


                    StopWindowMoveAnimation();

                    return;
                }


                if (progress < 0)
                    progress = 0;


                // =================================================
                // EASE OUT
                // =================================================

                double easedProgress =
                    1.0 -
                    Math.Pow(
                        1.0 - progress,
                        3.0);


                double newLeft =
                    _windowMoveStartLeft +
                    (
                        _windowMoveTargetLeft -
                        _windowMoveStartLeft
                    ) *
                    easedProgress;


                double newTop =
                    _windowMoveStartTop +
                    (
                        _windowMoveTargetTop -
                        _windowMoveStartTop
                    ) *
                    easedProgress;


                Left =
                    newLeft;

                Top =
                    newTop;
            }
            catch
            {
                StopWindowMoveAnimation();
            }
        }


        // =========================================================
        // STOP MAIN WINDOW MOVEMENT
        //
        // PUBLIC because SettingsManager/MainWindow lifecycle
        // may need to stop the animation.
        // =========================================================

        public void StopWindowMoveAnimation()
        {
            try
            {
                if (!_windowMoveAnimating)
                    return;


                CompositionTarget.Rendering -=
                    WindowMoveRendering;


                _windowMoveAnimating =
                    false;
            }
            catch
            {
                _windowMoveAnimating =
                    false;
            }
        }
    }
}