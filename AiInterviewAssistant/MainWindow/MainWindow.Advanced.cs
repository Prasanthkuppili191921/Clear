using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // DEFAULT VALUES
        // =========================================================

        private const int DefaultAiTimeoutSeconds = 30;

        private const int DefaultRetryCount = 2;


        // =========================================================
        // REFRESH ADVANCED SETTINGS
        // =========================================================

        public void RefreshAdvancedSettings()
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
                    "Advanced settings refresh error:\n\n" +
                    ex.Message);
            }
        }


        // =========================================================
        // GET AI TIMEOUT
        // =========================================================

        private int GetAiTimeoutSeconds()
        {
            try
            {
                if (currentSettings == null)
                {
                    currentSettings =
                        SettingsService.Load()
                        ?? new AppSettings();
                }

                if (currentSettings.AiTimeout > 0)
                {
                    return currentSettings.AiTimeout;
                }
            }
            catch
            {
            }

            return DefaultAiTimeoutSeconds;
        }


        // =========================================================
        // GET RETRY COUNT
        // =========================================================

        private int GetAiRetryCount()
        {
            try
            {
                if (currentSettings == null)
                {
                    currentSettings =
                        SettingsService.Load()
                        ?? new AppSettings();
                }

                if (currentSettings.RetryCount >= 0)
                {
                    return currentSettings.RetryCount;
                }
            }
            catch
            {
            }

            return DefaultRetryCount;
        }


        // =========================================================
        // DEBUG LOGGING ENABLED
        // =========================================================

        private bool IsDebugLoggingEnabled()
        {
            try
            {
                if (currentSettings == null)
                {
                    currentSettings =
                        SettingsService.Load()
                        ?? new AppSettings();
                }

                return currentSettings.DebugLogging;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // DEBUG LOG
        // =========================================================

        private void DebugLog(
            string message)
        {
            if (!IsDebugLoggingEnabled())
            {
                return;
            }

            try
            {
                string logDirectory =
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Logs");


                Directory.CreateDirectory(
                    logDirectory);


                string logFile =
                    Path.Combine(
                        logDirectory,
                        "debug.log");


                string logMessage =
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss.fff")
                    + " - "
                    + message
                    + Environment.NewLine;


                File.AppendAllText(
                    logFile,
                    logMessage);
            }
            catch
            {
                // Logging must never break the application.
            }
        }


        // =========================================================
        // RETRYABLE HTTP STATUS
        // =========================================================

        private bool IsRetryableStatusCode(
            HttpStatusCode statusCode)
        {
            int code =
                (int)statusCode;

            return
                code == 408 ||
                code == 429 ||
                code >= 500;
        }


        // =========================================================
        // GET RETRY DELAY
        // =========================================================

        private TimeSpan GetAdvancedRetryDelay(
            int attempt)
        {
            int seconds =
                Math.Min(
                    2 * (attempt + 1),
                    10);

            return TimeSpan.FromSeconds(
                seconds);
        }


        // =========================================================
        // WAIT BEFORE RETRY
        // =========================================================

        private async Task WaitBeforeAdvancedRetryAsync(
            int attempt)
        {
            TimeSpan delay =
                GetAdvancedRetryDelay(
                    attempt);


            DebugLog(
                "Waiting " +
                delay.TotalSeconds +
                " seconds before retry.");


            await Task.Delay(
                delay);
        }


        // =========================================================
        // CREATE AI TIMEOUT TOKEN
        // =========================================================

        private CancellationTokenSource
            CreateAiTimeoutCancellationTokenSource()
        {
            int timeoutSeconds =
                GetAiTimeoutSeconds();


            DebugLog(
                "AI timeout configured: " +
                timeoutSeconds +
                " seconds.");


            return new CancellationTokenSource(
                TimeSpan.FromSeconds(
                    timeoutSeconds));
        }


        // =========================================================
        // LOG ADVANCED SETTINGS
        // =========================================================

        private void LogAdvancedSettings()
        {
            DebugLog(
                "Advanced settings - " +
                "Timeout: " +
                GetAiTimeoutSeconds() +
                "s, " +
                "RetryCount: " +
                GetAiRetryCount() +
                ", " +
                "DebugLogging: " +
                IsDebugLoggingEnabled());
        }
    }
}