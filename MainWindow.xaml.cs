using System.Windows;
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
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                StatusTextBlock.Text = "Please enter both username and password.";
                return;
            }

            try
            {
                bool success = await vitapClient.AuthenticateAsync(username, password);
                if (success)
                {
                    StatusTextBlock.Text = "Authentication successful!";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;                    sessionManager.StartKeepAlive();
                }
                else
                {
                    StatusTextBlock.Text = "Authentication failed. Invalid credentials.";
                }
            }
            catch (System.Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
            }
        }
    }
}
