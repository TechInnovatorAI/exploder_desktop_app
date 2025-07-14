using Exploder.Views;
using System.Windows;

namespace Exploder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var splash = new SplashWindow();
            splash.Show();

            splash.Closed += (s, args) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    var projectWindow = new ProjectOpenWindow();
                    projectWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    projectWindow.Topmost = true;
                    bool? result = projectWindow.ShowDialog();

                    if (result == true)
                    {
                        var main = new MainWindow();
                        if (projectWindow.ProjectData != null)
                            main.InitializeWithProject(projectWindow.ProjectData);
                        else if (!string.IsNullOrEmpty(projectWindow.SelectedProjectPath))
                            main.LoadProjectFromFile(projectWindow.SelectedProjectPath);

                        Application.Current.MainWindow = main;
                        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                        main.Show();
                    }
                    else
                    {
                        Application.Current.Shutdown();
                    }
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            };
        }
    }
}
