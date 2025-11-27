using System;
using System.Windows;
using System.Windows.Media;
using VitapAuthenticator;

namespace VitapAuthenticator
{
    public partial class MainWindow : Window
    {
        private VitapClient vitapClient;
        private SessionManager sessionManager;

        public MainWindow()
        {
            InitializeComponent();
            vitapClient = new VitapClient();
            sessionManager = new SessionManager();
            
            // Initialize logging service with the TextBox
            LoggingService.Initialize(LogTextBox);
            LoggingService.Log("=== Application Started ===");
            LoggingService.LogInfo("VIT-AP WiFi Authenticator initialized");
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            LoggingService.LogInfo($"Login attempt for user: {username}");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                LoggingService.LogError("Username and password are required");
                StatusTextBlock.Text = "Please enter credentials";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            LoginButton.IsEnabled = false;
            StatusTextBlock.Text = "Authenticating...";
            StatusTextBlock.Foreground = new SolidColorBrush(Colors.Yellow);

            try
            {
                LoggingService.LogStep(0, "Starting authentication workflow");
                bool result = await vitapClient.AuthenticateAsync(username, password);

                if (result)
                {
                    LoggingService.LogSuccess("Authentication workflow completed successfully");
                    StatusTextBlock.Text = "Authentication successful";
                    StatusTextBlock.Foreground = new SolidColorBrush(Colors.LimeGreen);
                }
                else
                {
                    LoggingService.LogError("Authentication workflow failed");
                    StatusTextBlock.Text = "Authentication failed - check logs";
                    StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Exception during authentication: {ex.GetType().Name} - {ex.Message}");
                LoggingService.LogWarning($"StackTrace: {ex.StackTrace}");
                StatusTextBlock.Text = $"Error: {ex.Message}";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LoggingService.ClearLog();
            LoggingService.LogInfo("Log display cleared by user");
        }

        private void ExportLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logContent = LoggingService.GetAllLogs();
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"vitap_debug_log_{timestamp}.txt"
                );
                System.IO.File.WriteAllText(filePath, logContent);
                LoggingService.LogSuccess($"Log exported to: {filePath}");
                StatusTextBlock.Text = $"Log exported to Desktop";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.LimeGreen);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to export log: {ex.Message}");
                StatusTextBlock.Text = "Export failed";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }
    }
}
