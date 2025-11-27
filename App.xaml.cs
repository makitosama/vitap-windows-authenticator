using System;
using System.Windows;

namespace VitapAuthenticator
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal Error: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}", "Application Startup Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }
    }
}
