using N3Imager.Model;
using N3Imager.View;
using N3Imager.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace N3Imager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (GlobalVariables.globalSettings.Load())
            {
                var window = new MainWindow();
                var viewModel = new MainWindowViewModel();
                window.DataContext = viewModel;
                window.Closing += viewModel.OnWindowClosing;

                window.Show();
            }
            else
            {
                MessageBox.Show("An error occurred before the application could start.\n\nCould not load settings. ",
                    "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                Shutdown();
            }
        }

        private void Application_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Windows.MessageBox.Show(e.Exception.ToString(), "Fatal Error, exiting...");
            e.Handled = true;
            App.Current.Shutdown();
        }
    }

    public class GlobalVariables
    {
        public static GlobalSettings globalSettings = new GlobalSettings();
    }
}
