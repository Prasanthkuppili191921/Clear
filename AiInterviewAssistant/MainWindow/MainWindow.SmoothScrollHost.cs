using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // SMOOTH SCROLL STATE
        // =========================================================

        private double _scrollAnimationStart;
        private double _scrollAnimationTarget;
        private double _scrollAnimationCurrent;

        private DateTime _scrollAnimationStartTime;

        private bool _isScrollAnimating;

        private const double SCROLL_DISTANCE = 140.0;

        private const double SCROLL_DURATION_MS = 280.0;


        // =========================================================
        // START SMOOTH SCROLL
        // =========================================================

        private void StartSmoothChatScroll(
            double targetOffset)
        {
            try
            {
                if (ChatScrollViewer == null)
                    return;

                if (!ChatScrollViewer.IsLoaded)
                    return;


                double currentOffset =
                    ChatScrollViewer.VerticalOffset;


                double maxOffset =
                    ChatScrollViewer.ScrollableHeight;


                // -------------------------------------------------
                // Clamp target
                // -------------------------------------------------

                if (targetOffset < 0)
                {
                    targetOffset = 0;
                }

                if (targetOffset > maxOffset)
                {
                    targetOffset = maxOffset;
                }


                // -------------------------------------------------
                // Nothing to scroll
                // -------------------------------------------------

                if (
                    Math.Abs(
                        targetOffset -
                        currentOffset) < 0.5)
                {
                    return;
                }


                // -------------------------------------------------
                // If another animation is already running,
                // continue from current visual position.
                // -------------------------------------------------

                if (_isScrollAnimating)
                {
                    currentOffset =
                        _scrollAnimationCurrent;
                }


                _scrollAnimationStart =
                    currentOffset;

                _scrollAnimationCurrent =
                    currentOffset;

                _scrollAnimationTarget =
                    targetOffset;

                _scrollAnimationStartTime =
                    DateTime.UtcNow;


                // -------------------------------------------------
                // Stop previous animation
                // -------------------------------------------------

                CompositionTarget.Rendering -=
                    SmoothScrollRendering;


                _isScrollAnimating = true;


                // -------------------------------------------------
                // Start new animation
                // -------------------------------------------------

                CompositionTarget.Rendering +=
                    SmoothScrollRendering;
            }
            catch
            {
            }
        }


        // =========================================================
        // SMOOTH SCROLL RENDERING
        // =========================================================

        private void SmoothScrollRendering(
            object sender,
            EventArgs e)
        {
            try
            {
                if (ChatScrollViewer == null)
                {
                    StopSmoothScroll();
                    return;
                }


                if (!_isScrollAnimating)
                {
                    StopSmoothScroll();
                    return;
                }


                double elapsed =
                    (
                        DateTime.UtcNow -
                        _scrollAnimationStartTime
                    ).TotalMilliseconds;


                double progress =
                    elapsed /
                    SCROLL_DURATION_MS;


                // -------------------------------------------------
                // Animation completed
                // -------------------------------------------------

                if (progress >= 1.0)
                {
                    _scrollAnimationCurrent =
                        _scrollAnimationTarget;


                    ChatScrollViewer
                        .ScrollToVerticalOffset(
                            _scrollAnimationTarget);


                    StopSmoothScroll();

                    return;
                }


                // -------------------------------------------------
                // Ease-out cubic
                //
                // Fast initially -> soft stop
                // -------------------------------------------------

                double easedProgress =
                    1.0 -
                    Math.Pow(
                        1.0 - progress,
                        3.0);


                _scrollAnimationCurrent =
                    _scrollAnimationStart +
                    (
                        (
                            _scrollAnimationTarget -
                            _scrollAnimationStart
                        ) *
                        easedProgress
                    );


                ChatScrollViewer
                    .ScrollToVerticalOffset(
                        _scrollAnimationCurrent);
            }
            catch
            {
                StopSmoothScroll();
            }
        }


        // =========================================================
        // STOP SMOOTH SCROLL
        // =========================================================

        private void StopSmoothScroll()
        {
            _isScrollAnimating = false;

            CompositionTarget.Rendering -=
                SmoothScrollRendering;
        }


        // =========================================================
        // SCROLL UP
        // =========================================================

        private void SmoothScrollUp()
        {
            try
            {
                if (ChatScrollViewer == null)
                    return;


                double currentOffset =
                    _isScrollAnimating
                        ? _scrollAnimationCurrent
                        : ChatScrollViewer.VerticalOffset;


                double targetOffset =
                    currentOffset -
                    SCROLL_DISTANCE;


                StartSmoothChatScroll(
                    targetOffset);
            }
            catch
            {
            }
        }


        // =========================================================
        // SCROLL DOWN
        // =========================================================

        private void SmoothScrollDown()
        {
            try
            {
                if (ChatScrollViewer == null)
                    return;


                double currentOffset =
                    _isScrollAnimating
                        ? _scrollAnimationCurrent
                        : ChatScrollViewer.VerticalOffset;


                double targetOffset =
                    currentOffset +
                    SCROLL_DISTANCE;


                StartSmoothChatScroll(
                    targetOffset);
            }
            catch
            {
            }
        }


        // =========================================================
        // CLEANUP
        // =========================================================

        private void CleanupSmoothScrolling()
        {
            try
            {
                StopSmoothScroll();
            }
            catch
            {
            }
        }
    }
}