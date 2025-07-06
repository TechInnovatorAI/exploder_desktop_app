using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Exploder
{
    public partial class SplashWindow : Window
    {
        private readonly string[] allImages = {
            "pack://application:,,,/Assets/Splash1.png",
            "pack://application:,,,/Assets/Splash2.png",
            "pack://application:,,,/Assets/Splash3.png",
            "pack://application:,,,/Assets/Splash4.png",
            "pack://application:,,,/Assets/Splash5.png",
            "pack://application:,,,/Assets/Splash6.png",
            "pack://application:,,,/Assets/Splash7.png",
            "pack://application:,,,/Assets/Splash8.png",
            "pack://application:,,,/Assets/Splash9.png",
            "pack://application:,,,/Assets/Splash10.png"
        };

        private string[] randomizedImages;
        private int currentIndex = 0;
        private DispatcherTimer slideTimer;
        private DispatcherTimer loadTimer;
        private double loadProgress = 0;

        private const double fastIntervalSeconds = 1.0; // fast for first 7 images
        private const double slowIntervalSeconds = 3.0; // slow for last 3 images

        private static Random rng = new Random();

        public SplashWindow()
        {
            InitializeComponent();

            randomizedImages = Shuffle(allImages);
            ShowImage(randomizedImages[currentIndex]);
            SetupSlideTimer();
            StartLoading();
        }

        private static string[] Shuffle(string[] array)
        {
            var list = new List<string>(array);
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var temp = list[k];
                list[k] = list[n];
                list[n] = temp;
            }
            return list.ToArray();
        }

        private void SetupSlideTimer()
        {
            slideTimer = new DispatcherTimer();
            slideTimer.Tick += SlideTimer_Tick;
            slideTimer.Interval = TimeSpan.FromSeconds(fastIntervalSeconds);
            slideTimer.Start();
        }

        private void SlideTimer_Tick(object sender, EventArgs e)
        {
            currentIndex++;
            if (currentIndex >= randomizedImages.Length)
            {
                slideTimer.Stop();
                return;
            }

            ShowImage(randomizedImages[currentIndex]);

            // Adjust timer interval dynamically
            slideTimer.Interval = currentIndex < 7
                ? TimeSpan.FromSeconds(fastIntervalSeconds)
                : TimeSpan.FromSeconds(slowIntervalSeconds);
        }

        private void ShowImage(string imageUri)
        {
            try
            {
                SlideImage.Source = new BitmapImage(new Uri(imageUri));
                FadeInImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading image: " + ex.Message);
            }
        }

        private void FadeInImage()
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            SlideImage.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void StartLoading()
        {
            loadTimer = new DispatcherTimer();
            loadTimer.Interval = TimeSpan.FromMilliseconds(50);
            loadTimer.Tick += LoadTimer_Tick;
            loadTimer.Start();
        }

        private void LoadTimer_Tick(object sender, EventArgs e)
        {
            loadProgress += 2;
            LoadingBar.Value = loadProgress;

            if (loadProgress >= 100)
            {
                loadTimer.Stop();
                slideTimer.Stop();

                var main = new MainWindow();
                main.Show();
                Close();
            }
        }
        private void SlideImage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateImageClip();
        }

        private void SlideImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateImageClip();
        }

        private void UpdateImageClip()
        {
            if (SlideImage.ActualWidth > 0 && SlideImage.ActualHeight > 0)
            {
                double radiusX = SlideImage.ActualWidth * 0.003;
                double radiusY = SlideImage.ActualHeight * 0.003;

                SlideImage.Clip = new RectangleGeometry
                {
                    Rect = new Rect(0, 0, SlideImage.ActualWidth, SlideImage.ActualHeight),
                    RadiusX = radiusX,
                    RadiusY = radiusY
                };
            }
        }

    }
}
