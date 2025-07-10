using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Exploder.Views
{
    public partial class SplashWindow : Window
    {
        private DispatcherTimer? loadTimer;
        private double loadProgress = 0;
        private bool isLoading = true;

        public SplashWindow()
        {
            InitializeComponent();
            StartLoading();
        }

        private async void StartLoading()
        {
            try
            {
                // Simulate loading process with realistic steps
                await SimulateLoadingProcess();
                
                // Loading completed successfully
                await Task.Delay(500); // Brief pause to show completion
                
                // Close splash; App.xaml.cs will handle the next window
                await Dispatcher.InvokeAsync(() => this.Close());
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Error during startup: {ex.Message}", "Startup Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                });
            }
        }

        private async Task SimulateLoadingProcess()
        {
            // Initialize loading timer
            loadTimer = new DispatcherTimer();
            loadTimer.Interval = TimeSpan.FromMilliseconds(30);
            loadTimer.Tick += LoadTimer_Tick;
            loadTimer.Start();

            // Simulate different loading phases
            await SimulatePhase("Initializing...", 0, 20, 100);
            await SimulatePhase("Loading components...", 20, 50, 150);
            await SimulatePhase("Preparing interface...", 50, 80, 200);
            await SimulatePhase("Ready...", 80, 100, 100);

            loadTimer.Stop();
        }

        private async Task SimulatePhase(string status, double startProgress, double endProgress, int duration)
        {
            await Dispatcher.InvokeAsync(() => UpdateStatus(status));
            
            double progressStep = (endProgress - startProgress) / (duration / 30.0);
            double currentProgress = startProgress;
            
            while (currentProgress < endProgress && isLoading)
            {
                currentProgress += progressStep;
                loadProgress = Math.Min(currentProgress, endProgress);
                LoadingBar.Value = loadProgress;
                await Task.Delay(30);
            }
        }

        private void LoadTimer_Tick(object? sender, EventArgs e)
        {
            // Timer is now just for smooth progress bar animation
            // Actual loading logic is handled in SimulateLoadingProcess
        }

        private void UpdateStatus(string status)
        {
            if (StatusText != null)
            {
                StatusText.Text = status;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            isLoading = false;
            loadTimer?.Stop();
            base.OnClosed(e);
        }
    }
}

