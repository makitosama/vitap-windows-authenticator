using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace VitapAuthenticator
{
    public static class LoggingService
    {
        private static TextBox logTextBox = null;
        private static List<string> logBuffer = new List<string>();
        private static readonly object lockObject = new object();
        private const int MAX_LOG_LINES = 1000;

        public static void Initialize(TextBox logBox)
        {
            logTextBox = logBox;
            lock (lockObject)
            {
                if (logTextBox != null)
                {
                    logTextBox.Text = string.Empty;
                }
            }
        }

        public static void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] {message}";

            lock (lockObject)
            {
                logBuffer.Add(logEntry);

                if (logBuffer.Count > MAX_LOG_LINES)
                {
                    logBuffer.RemoveRange(0, logBuffer.Count - MAX_LOG_LINES);
                }

                if (logTextBox != null)
                {
                    try
                    {
                        logTextBox.Dispatcher.Invoke(() =>
                        {
                            logTextBox.AppendText(logEntry + Environment.NewLine);
                            logTextBox.ScrollToEnd();
                        });
                    }
                    catch
                    {
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine(logEntry);
        }

        public static void LogError(string message)
        {
            Log($"[ERROR] {message}");
        }

        public static void LogSuccess(string message)
        {
            Log($"[SUCCESS] {message}");
        }

        public static void LogWarning(string message)
        {
            Log($"[WARNING] {message}");
        }

        public static void LogInfo(string message)
        {
            Log($"[INFO] {message}");
        }

        public static void LogStep(int step, string message)
        {
            Log($"[Step {step}] {message}");
        }

        public static void LogHttpRequest(string method, string url, string parameters = "")
        {
            Log($"[HTTP] {method} {url}");
            if (!string.IsNullOrEmpty(parameters))
                Log($"[HTTP] Parameters: {parameters}");
        }

        public static void LogHttpResponse(int statusCode, int contentLength, string contentPreview = "")
        {
            Log($"[HTTP] Response: {statusCode} ({contentLength} bytes)");
            if (!string.IsNullOrEmpty(contentPreview))
                Log($"[HTTP] Content: {contentPreview.Substring(0, Math.Min(200, contentPreview.Length))}");
        }

        public static void ClearLog()
        {
            lock (lockObject)
            {
                logBuffer.Clear();
                if (logTextBox != null)
                {
                    try
                    {
                        logTextBox.Dispatcher.Invoke(() =>
                        {
                            logTextBox.Clear();
                        });
                    }
                    catch
                    {
                    }
                }
            }
            Log("=== Log Cleared ===");
        }

        public static string GetAllLogs()
        {
            lock (lockObject)
            {
                return string.Join(Environment.NewLine, logBuffer);
            }
        }
    }
}
