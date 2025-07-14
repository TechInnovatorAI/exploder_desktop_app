using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Exploder.Views
{
    public partial class SplashWindow : Window
    {
        private DispatcherTimer? loadTimer;
        private DispatcherTimer? imageTimer;
        private double loadProgress = 0;
        private bool isLoading = true;
        private List<string> splashImages = new List<string>();
        private int currentImageIndex = 0;

        public SplashWindow()
        {
            InitializeComponent();
            LoadSplashImages();
            StartLoading();
        }

        private void LoadSplashImages()
        {
            try
            {
                // Add splash images in order using resource URIs
                for (int i = 1; i <= 10; i++)
                {
                    string resourcePath = $"/Infrastructure/Assets/Splash{i}.png";
                    splashImages.Add(resourcePath);
                }

                // Add the istockphoto image
                splashImages.Add("/Infrastructure/Assets/istockphoto-1009436554-612x612.png");

                // Start image rotation timer
                if (splashImages.Count > 0)
                {
                    imageTimer = new DispatcherTimer();
                    imageTimer.Interval = TimeSpan.FromSeconds(1.5); // Change image every 1.5 seconds
                    imageTimer.Tick += ImageTimer_Tick;
                    imageTimer.Start();
                    
                    // Load first image
                    LoadCurrentImage();
                }
            }
            catch (Exception ex)
            {
                // If there's an error loading images, continue without them
                System.Diagnostics.Debug.WriteLine($"Error loading splash images: {ex.Message}");
            }
        }

        private void ImageTimer_Tick(object? sender, EventArgs e)
        {
            if (splashImages.Count > 0)
            {
                // Fade out current image
                var fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                
                fadeOut.Completed += (s, e) =>
                {
                    // Change to next image
                    currentImageIndex = (currentImageIndex + 1) % splashImages.Count;
                    LoadCurrentImage();
                    
                    // Fade in new image
                    var fadeIn = new DoubleAnimation
                    {
                        From = 0.0,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(300)
                    };
                    SplashImage.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                };
                
                SplashImage.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
        }

        private void LoadCurrentImage()
        {
            try
            {
                if (currentImageIndex < splashImages.Count)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(splashImages[currentImageIndex], UriKind.Relative);
                    bitmap.EndInit();
                    
                    SplashImage.Source = bitmap;
                    
                    // For the first image, fade in immediately
                    if (currentImageIndex == 0)
                    {
                        var fadeIn = new DoubleAnimation
                        {
                            From = 0.0,
                            To = 1.0,
                            Duration = TimeSpan.FromMilliseconds(300)
                        };
                        SplashImage.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image {currentImageIndex}: {ex.Message}");
            }
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
            imageTimer?.Stop();
            base.OnClosed(e);
        }
    }
}

