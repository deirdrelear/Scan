using LocalScan.Helpers;
using LocalScan.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LocalScan
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string CharScanFilePath { get; private set; }
        public static string ExecuteDirectory => System.IO.Path.GetDirectoryName(new Uri(Current.GetType().Assembly.GetName().CodeBase).LocalPath);

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string error = string.Empty;
            Exception exc = e.Exception;
            while (exc != null)
            {
                error += $"\r\n{exc.Message}";
                exc = exc.InnerException;
            }
            MessageBox.Show($"Возникло исключение:{error}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            CharScanFilePath = System.IO.Path.Combine(ExecuteDirectory, "scan_char.xml");

            StartupUri = new Uri("Views/MainWindow.xaml", UriKind.Relative);
        }
    }
}
