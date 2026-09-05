// MainWindow.OnlineTests.Core.cs

using System;
using System.Threading;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // VISION RESULT
        // =========================================================

        public class VisionResult
        {
            public string Question { get; set; }

            public string Answer { get; set; }
        }


        // =========================================================
        // CAPTURE INTERVAL
        // =========================================================

        private int GetCaptureIntervalMilliseconds(
            string captureInterval)
        {
            if (string.IsNullOrWhiteSpace(
                    captureInterval))
            {
                return 1000;
            }

            switch (
                captureInterval.Trim())
            {
                case "Fast":
                    return 500;

                case "Normal":
                    return 1000;

                case "Slow":
                    return 2000;

                default:
                    return 1000;
            }
        }


        // =========================================================
        // VISION RESPONSE TOKENS
        // =========================================================

        private int GetVisionMaxResponseTokens(
            string responseLength)
        {
            if (string.IsNullOrWhiteSpace(
                    responseLength))
            {
                return 400;
            }

            switch (
                responseLength
                    .Trim()
                    .ToLowerInvariant())
            {
                case "short":
                    return 200;

                case "long":
                    return 700;

                case "medium":
                default:
                    return 400;
            }
        }


        // =========================================================
        // VISION REQUEST LOCK
        // =========================================================

        private bool TryStartVisionRequest()
        {
            return Interlocked.CompareExchange(
                ref visionRequestRunning,
                1,
                0) == 0;
        }


        private void FinishVisionRequest()
        {
            Interlocked.Exchange(
                ref visionRequestRunning,
                0);
        }
    }
}