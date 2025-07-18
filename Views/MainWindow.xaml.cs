using Exploder.Infrastructure.Setting;
using Exploder.Models;
using Exploder.Services;
using Microsoft.Win32;
using System.IO.Compression;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace Exploder.Views
{
    public partial class MainWindow : Window
    {
        private AppMode currentMode = AppMode.View;
        private ProjectData? currentProject;
        private PageData? currentPage;
        private Stack<PageData> pageHistory = new Stack<PageData>();
        
        // Drawing state
        private bool isDrawing = false;
        private System.Windows.Point startPoint;
        private UIElement? currentShape;
        private UIElement? selectedObject;
        
        // Tool states
        private bool circleToolActive = false;
        private bool rectangleToolActive = false;
        private bool roundedRectToolActive = false;
        private bool triangleToolActive = false;
        private bool lineToolActive = false;
        private bool textToolActive = false;
        private bool urlToolActive = false;
        private bool deleteToolActive = false;
        
        // Undo/Redo
        private Stack<PageData> undoStack = new Stack<PageData>();
        private Stack<PageData> redoStack = new Stack<PageData>();
        private const int MAX_UNDO_STEPS = 20;

        // Copy/Paste support
        private ExploderObject? clipboardObject;

        private Dictionary<UIElement, Canvas> linkedPages = new Dictionary<UIElement, Canvas>();

        private List<Button> viewModeButtons = new List<Button>();
        private readonly IPublishingService _publishingService;

        // Drag-and-drop state
        private bool isDraggingObject = false;
        private UIElement? draggingObject = null;
        private Point dragStartPoint;
        private Point objectStartPoint;

        public MainWindow()
        {
            InitializeComponent();
            _publishingService = new PublishingService();
            SetMode(AppMode.View);
            UpdateStatus("Ready");
            UpdateObjectCount();
            this.Loaded += (s, e) => drawingCanvas.Focus();
            this.SizeChanged += MainWindow_SizeChanged;
        }
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //foreach (UIElement child in drawingCanvas.Children)
            //{
            //    if (child is Image img)
            //    {
            //        double left = (drawingCanvas.ActualWidth - img.Width) / 2;
            //        double top = (drawingCanvas.ActualHeight - img.Height) / 2;
            //        Canvas.SetLeft(img, left);
            //        Canvas.SetTop(img, top);
            //    }
            //}
        }

        public void InitializeWithProject(ProjectData project)
        {
            currentProject = project;
            currentPage = project.Pages.FirstOrDefault();
            
            if (currentPage != null)
            {
                LoadPage(currentPage);
                UpdateProjectInfo();
                UpdateProjectTree();
            }
        }

        public void LoadProjectFromFile(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                currentProject = JsonSerializer.Deserialize<ProjectData>(json);

                if (currentProject == null)
                {
                                    System.Windows.MessageBox.Show("Failed to load project: Empty or invalid file.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                currentPage = currentProject.Pages.FirstOrDefault();
                if (currentPage != null)
                {
                    LoadPage(currentPage);
                    UpdateProjectInfo();
                    UpdateProjectTree();
                }

                // Save to recent projects
                SaveToRecentProjects(path);
                
                UpdateStatus($"Project loaded: {System.IO.Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load project:\n{ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPage(PageData page)
        {
            drawingCanvas.Children.Clear();
            viewModeButtons.Clear(); // Clear the button list when loading a new page
            currentPage = page;
            
            // Set background color
            try
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(page.PageSettings.BackgroundColor);
                drawingCanvas.Background = brush ?? System.Windows.Media.Brushes.White;
            }
            catch
            {
                drawingCanvas.Background = System.Windows.Media.Brushes.White;
            }

            // Load objects
            foreach (var obj in page.Objects.OrderBy(o => o.ZIndex))
            {
                CreateUIElementFromObject(obj);
            }

            UpdatePageInfo();
            UpdateObjectCount();
            UpdateProjectTree();
        }

        private void CreateUIElementFromObject(ExploderObject obj)
        {
            try
            {
                if (obj == null)
                {
                    MessageBox.Show("Cannot create UI element from null object.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Sanitize the object to prevent crashes from invalid data
                obj.Sanitize();

                UIElement? element = obj.ObjectType switch
                {
                    "Circle" => CreateCircle(obj),
                    "Rectangle" => CreateRectangle(obj),
                    "RoundedRectangle" => CreateRoundedRectangle(obj),
                    "Triangle" => CreateTriangle(obj),
                    "Line" => CreateLine(obj),
                    "Text" => CreateText(obj),
                    "Image" => CreateImage(obj),
                    "Button" => CreateButton(obj),
                    _ => null
                };

                if (element != null)
                {
                    Canvas.SetLeft(element, obj.Left);
                    Canvas.SetTop(element, obj.Top);
                    Canvas.SetZIndex(element, obj.ZIndex);
                    
                    // Store reference to the object data
                    if (element is FrameworkElement fe)
                    {
                        fe.Tag = obj;
                    }
                    
                    drawingCanvas.Children.Add(element);

                    // Add right-click handler for properties
                    if (element is FrameworkElement fe2)
                    {
                        fe2.MouseRightButtonDown += (s, e) =>
                        {
                            ShowObjectProperties(obj, fe2, currentProject, currentPage);
                            e.Handled = true;
                        };
                    }

                    // Add text overlay if the object has text and is not a Text object itself
                    if (!string.IsNullOrEmpty(obj.Text) && obj.ObjectType != "Text")
                    {
                        AddTextOverlay(obj, element);
                    }
                }
                else
                {
                    MessageBox.Show($"Unknown object type: {obj.ObjectType}", "Warning", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating UI element: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Error creating UI element: {ex.Message}");
            }
        }

        private void AddTextOverlay(ExploderObject obj, UIElement shape)
        {
            var textBlock = new TextBlock
            {
                Text = obj.Text,
                FontFamily = new FontFamily(obj.FontFamily),
                FontSize = obj.FontSize,
                FontWeight = GetFontWeight(obj.FontWeight),
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.TextColor) ?? Brushes.Black,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            // Position text in the center of the shape
            Canvas.SetLeft(textBlock, obj.Left + (obj.Width - textBlock.ActualWidth) / 2);
            Canvas.SetTop(textBlock, obj.Top + (obj.Height - textBlock.ActualHeight) / 2);
            Canvas.SetZIndex(textBlock, obj.ZIndex + 1); // Text should be above the shape

            // Store reference to the shape
            textBlock.Tag = new TextOverlayInfo { Shape = shape, ObjectData = obj };

            drawingCanvas.Children.Add(textBlock);

            // Add event handlers to the text block
            textBlock.MouseLeftButtonDown += Object_MouseLeftButtonDown;
            textBlock.MouseRightButtonDown += Object_MouseRightButtonDown;
        }

        private FontWeight GetFontWeight(string weight)
        {
            return weight?.ToLower() switch
            {
                "bold" => FontWeights.Bold,
                "light" => FontWeights.Light,
                "normal" => FontWeights.Normal,
                _ => FontWeights.Normal
            };
        }

        private UIElement CreateCircle(ExploderObject obj)
        {
            try
            {
                if (obj == null)
                {
                    throw new ArgumentNullException(nameof(obj));
                }

                var ellipse = new Ellipse
                {
                    Width = Math.Max(0, obj.Width),
                    Height = Math.Max(0, obj.Height),
                    Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.FillColor ?? "#FFFFFF") ?? new SolidColorBrush(Colors.Transparent),
                    Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.StrokeColor ?? "#000000") ?? new SolidColorBrush(Colors.Black),
                    StrokeThickness = Math.Max(0, obj.StrokeThickness),
                    Opacity = Math.Max(0, Math.Min(1, obj.Opacity))
                };

                ellipse.MouseLeftButtonDown += Object_MouseLeftButtonDown;
                ellipse.MouseRightButtonDown += Object_MouseRightButtonDown;
                
                return ellipse;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating circle: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                // Return a simple fallback ellipse
                return new Ellipse { Width = 50, Height = 50, Fill = Brushes.Red };
            }
        }

        private UIElement CreateRectangle(ExploderObject obj)
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = obj.Width,
                Height = obj.Height,
                Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.FillColor) ?? new SolidColorBrush(Colors.Transparent),
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.StrokeColor) ?? new SolidColorBrush(Colors.Black),
                StrokeThickness = obj.StrokeThickness,
                Opacity = obj.Opacity
            };

            rect.MouseLeftButtonDown += Object_MouseLeftButtonDown;
            rect.MouseRightButtonDown += Object_MouseRightButtonDown;
            
            return rect;
        }

        private UIElement CreateRoundedRectangle(ExploderObject obj)
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = obj.Width,
                Height = obj.Height,
                Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.FillColor) ?? new SolidColorBrush(Colors.Transparent),
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.StrokeColor) ?? new SolidColorBrush(Colors.Black),
                StrokeThickness = obj.StrokeThickness,
                Opacity = obj.Opacity,
                RadiusX = 10,
                RadiusY = 10
            };

            rect.MouseLeftButtonDown += Object_MouseLeftButtonDown;
            rect.MouseRightButtonDown += Object_MouseRightButtonDown;
            
            return rect;
        }

        private UIElement CreateTriangle(ExploderObject obj)
        {
            var triangle = new Polygon
            {
                Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.FillColor) ?? new SolidColorBrush(Colors.Transparent),
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.StrokeColor) ?? new SolidColorBrush(Colors.Black),
                StrokeThickness = obj.StrokeThickness,
                Opacity = obj.Opacity
            };

            // Create triangle points
            var points = new PointCollection
            {
                new System.Windows.Point(obj.Width / 2, 0),
                new System.Windows.Point(0, obj.Height),
                new System.Windows.Point(obj.Width, obj.Height)
            };
            triangle.Points = points;

            triangle.MouseLeftButtonDown += Object_MouseLeftButtonDown;
            triangle.MouseRightButtonDown += Object_MouseRightButtonDown;
            
            return triangle;
        }

        private UIElement CreateLine(ExploderObject obj)
        {
            var line = new Line
            {
                X1 = obj.X1,
                Y1 = obj.Y1,
                X2 = obj.X2,
                Y2 = obj.Y2,
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.StrokeColor) ?? System.Windows.Media.Brushes.Black,
                StrokeThickness = obj.StrokeThickness,
                Opacity = obj.Opacity
            };

            line.MouseLeftButtonDown += Object_MouseLeftButtonDown;
            line.MouseRightButtonDown += Object_MouseRightButtonDown;
            
            return line;
        }

        private UIElement CreateText(ExploderObject obj)
        {
            var textBlock = new TextBlock
            {
                Text = obj.Text,
                FontFamily = new FontFamily(obj.FontFamily),
                FontSize = obj.FontSize,
                FontWeight = obj.FontWeight == "Bold" ? FontWeights.Bold : FontWeights.Normal,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.TextColor ?? "#000000"),
                Opacity = obj.Opacity,
                TextWrapping = TextWrapping.Wrap
            };

            textBlock.MouseLeftButtonDown += Object_MouseLeftButtonDown;
            textBlock.MouseRightButtonDown += Object_MouseRightButtonDown;
            
            return textBlock;
        }

        private UIElement CreateImage(ExploderObject obj)
        {
            var image = new System.Windows.Controls.Image
            {
                Width = obj.Width,
                Height = obj.Height,
                Stretch = Stretch.Uniform,
                Opacity = obj.Opacity
            };

            try
            {
                if (!string.IsNullOrEmpty(obj.ImagePath))
                {
                    image.Source = new BitmapImage(new Uri(obj.ImagePath));
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading image: {ex.Message}");
            }

            image.MouseLeftButtonDown += Object_MouseLeftButtonDown;
            image.MouseRightButtonDown += Object_MouseRightButtonDown;
            
            // Add resize handles for images in edit mode
            if (currentMode == AppMode.Apply)
            {
                AddResizeHandles(image);
            }
            
            return image;
        }

        private UIElement CreateButton(ExploderObject obj)
        {
            try
            {
                var button = new Button
                {
                    Width = obj.Width,
                    Height = obj.Height,
                    Content = "", // No visible content for transparent overlay
                    Opacity = 0.0, // Completely transparent
                    Cursor = Cursors.Hand,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FontFamily = string.IsNullOrEmpty(obj.FontFamily) ? new FontFamily("Arial") : new FontFamily(obj.FontFamily),
                    FontSize = obj.FontSize,
                    FontWeight = GetFontWeight(obj.FontWeight),
                    Foreground = Brushes.Transparent
                };

                // Add event handlers
                button.Click += (s, e) =>
                {
                    HandleObjectClick(button);
                };

                button.MouseRightButtonDown += (s, e) =>
                {
                    // Only show properties window in edit mode
                    if (currentMode == AppMode.Apply)
                    {
                        ShowObjectProperties(obj, button, currentProject, currentPage);
                    }
                    e.Handled = true;
                };

                // Add to viewModeButtons list for tracking
                viewModeButtons.Add(button);
                
                return button;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating button: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                // Return a simple fallback button
                var fallbackButton = new Button
                {
                    Width = obj.Width,
                    Height = obj.Height,
                    Content = "Button",
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.DarkBlue,
                    BorderThickness = new Thickness(2)
                };
                fallbackButton.Click += (s, e) => HandleObjectClick(fallbackButton);
                fallbackButton.MouseRightButtonDown += (s, e) => 
                {
                    if (currentMode == AppMode.Apply)
                    {
                        ShowObjectProperties(obj, fallbackButton, currentProject, currentPage);
                    }
                    e.Handled = true;
                };
                viewModeButtons.Add(fallbackButton);
                return fallbackButton;
            }
        }

        private void AddResizeHandles(FrameworkElement element)
        {
            // Create resize handles (small rectangles at corners)
            var handles = new List<Rectangle>();
            var positions = new[] { 
                new Point(0, 0),           // Top-left
                new Point(1, 0),           // Top-right
                new Point(0, 1),           // Bottom-left
                new Point(1, 1)            // Bottom-right
            };

            foreach (var pos in positions)
            {
                var handle = new Rectangle
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.Blue,
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    Tag = new ResizeHandleInfo { Element = element, Position = pos }
                };

                Canvas.SetLeft(handle, Canvas.GetLeft(element) + (element.Width * pos.X) - 4);
                Canvas.SetTop(handle, Canvas.GetTop(element) + (element.Height * pos.Y) - 4);

                handle.MouseLeftButtonDown += ResizeHandle_MouseLeftButtonDown;
                handle.MouseMove += ResizeHandle_MouseMove;
                handle.MouseLeftButtonUp += ResizeHandle_MouseLeftButtonUp;

                drawingCanvas.Children.Add(handle);
                handles.Add(handle);
            }
        }

        private class ResizeHandleInfo
        {
            public FrameworkElement Element { get; set; } = null!;
            public Point Position { get; set; }
        }

        private class TextOverlayInfo
        {
            public UIElement Shape { get; set; } = null!;
            public ExploderObject ObjectData { get; set; } = null!;
        }

        private bool isResizing = false;
        private Rectangle? resizingHandle = null;
        private Point resizeStartPoint;
        private Size originalSize;

        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle handle && handle.Tag is ResizeHandleInfo tag)
            {
                isResizing = true;
                resizingHandle = handle;
                resizeStartPoint = e.GetPosition(drawingCanvas);
                originalSize = new Size(tag.Element.Width, tag.Element.Height);
                drawingCanvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing && resizingHandle != null && resizingHandle.Tag is ResizeHandleInfo tag)
            {
                var currentPoint = e.GetPosition(drawingCanvas);
                var delta = currentPoint - resizeStartPoint;
                var element = tag.Element;
                var position = tag.Position;

                // Calculate new size based on which corner is being dragged
                double newWidth = originalSize.Width;
                double newHeight = originalSize.Height;

                if (position.X == 0) // Left side
                {
                    newWidth = Math.Max(20, originalSize.Width - delta.X);
                    Canvas.SetLeft(element, Canvas.GetLeft(element) + (originalSize.Width - newWidth));
                }
                else // Right side
                {
                    newWidth = Math.Max(20, originalSize.Width + delta.X);
                }

                if (position.Y == 0) // Top side
                {
                    newHeight = Math.Max(20, originalSize.Height - delta.Y);
                    Canvas.SetTop(element, Canvas.GetTop(element) + (originalSize.Height - newHeight));
                }
                else // Bottom side
                {
                    newHeight = Math.Max(20, originalSize.Height + delta.Y);
                }

                element.Width = newWidth;
                element.Height = newHeight;

                // Update resize handle positions
                UpdateResizeHandlePositions(element);

                // Update the object data
                if (element.Tag is ExploderObject obj)
                {
                    obj.Width = newWidth;
                    obj.Height = newHeight;
                    obj.Left = Canvas.GetLeft(element);
                    obj.Top = Canvas.GetTop(element);
                }

                UpdateStatus($"Resizing to {newWidth:F0} x {newHeight:F0}");
            }
        }

        private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isResizing)
            {
                isResizing = false;
                resizingHandle = null;
                drawingCanvas.ReleaseMouseCapture();
                UpdateStatus("Resize completed");
            }
        }

        private void UpdateResizeHandlePositions(FrameworkElement element)
        {
            // Update positions of all resize handles for this element
            foreach (var child in drawingCanvas.Children.OfType<Rectangle>())
            {
                if (child.Tag is ResizeHandleInfo tag && tag.Element == element)
                {
                    var position = tag.Position;
                    Canvas.SetLeft(child, Canvas.GetLeft(element) + (element.Width * position.X) - 4);
                    Canvas.SetTop(child, Canvas.GetTop(element) + (element.Height * position.Y) - 4);
                }
            }
        }

        private void Object_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currentMode == AppMode.View)
            {
                HandleObjectClick(sender as UIElement);
            }
            else if (currentMode == AppMode.Apply)
            {
                // Check if delete tool is active
                if (deleteToolActive)
                {
                    // Delete tool is active - delete the clicked object
                    DeleteClickedObject(sender as UIElement);
                    e.Handled = true;
                    return;
                }
                
                // In edit mode, clicking on objects can either select them or start dragging
                if (e.ClickCount == 1)
                {
                    // Single click - start drag
                    if (sender is UIElement element)
                    {
                        isDraggingObject = true;
                        draggingObject = element;
                        dragStartPoint = e.GetPosition(drawingCanvas);
                        objectStartPoint = new Point(Canvas.GetLeft(element), Canvas.GetTop(element));
                        drawingCanvas.CaptureMouse();
                    }
                }
                else if (e.ClickCount == 2)
                {
                    // Double click - select for editing
                    SelectObject(sender as UIElement);
                }
            }
            e.Handled = true;
        }

        private void Object_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Only show properties window in edit mode
                if (currentMode == AppMode.Apply)
                {
                    if (sender is FrameworkElement element && element.Tag is ExploderObject obj)
                    {
                        ShowObjectProperties(obj, element, currentProject, currentPage);
                    }
                    else
                    {
                        UpdateStatus("No object data found for this element");
                    }
                }
                // In view mode, right-click does nothing (no properties window)
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in right-click handler: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private void HandleObjectClick(UIElement? element)
        {
            if (element is FrameworkElement fe && fe.Tag is ExploderObject obj)
            {
                UpdateStatus($"Clicked object: {obj.ObjectName}, LinkType: {obj.LinkType}, LinkPageId: {obj.LinkPageId}");
                
                switch (obj.LinkType)
                {
                    case LinkType.NewPage:
                        NavigateToPage(obj.LinkPageId);
                        break;
                    case LinkType.Document:
                        OpenDocument(obj.LinkDocumentPath, obj.LinkFileType);
                        break;
                    case LinkType.Url:
                        OpenUrl(obj.LinkUrl);
                        break;
                    case LinkType.ExcelData:
                        OpenExcelData(obj.ExcelRange);
                        break;
                    case LinkType.None:
                    default:
                        UpdateStatus("No link type set for this object");
                        break;
                }
            }
            else
            {
                UpdateStatus("Clicked element has no ExploderObject tag");
            }
        }

        private void NavigateToPage(string pageId)
        {
            if (currentProject == null) 
            {
                UpdateStatus("No current project");
                return;
            }

            if (string.IsNullOrEmpty(pageId))
            {
                UpdateStatus("Page ID is empty");
                return;
            }

            var targetPage = currentProject.Pages.FirstOrDefault(p => p.PageId == pageId);
            if (targetPage != null)
            {
                // Save current page to history
                if (currentPage != null)
                {
                    pageHistory.Push(currentPage);
                }

                LoadPage(targetPage);
                UpdateStatus($"Navigated to page: {targetPage.PageName}");
            }
            else
            {
                UpdateStatus($"Page with ID '{pageId}' not found. Available pages: {string.Join(", ", currentProject.Pages.Select(p => p.PageName))}");
            }
        }

        private void OpenDocument(string path, LinkFileType fileType)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show("Document not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string ext = System.IO.Path.GetExtension(path).ToLower();
            bool valid = true;
            string expected = "";
            switch (fileType)
            {
                case LinkFileType.Video:
                    valid = ext == ".mp4" || ext == ".avi" || ext == ".mov" || ext == ".wmv" || ext == ".flv";
                    expected = ".mp4, .avi, .mov, .wmv, .flv";
                    break;
                case LinkFileType.PDF:
                    valid = ext == ".pdf";
                    expected = ".pdf";
                    break;
                case LinkFileType.Excel:
                    valid = ext == ".xlsx" || ext == ".xls";
                    expected = ".xlsx, .xls";
                    break;
                case LinkFileType.Word:
                    valid = ext == ".docx" || ext == ".doc";
                    expected = ".docx, .doc";
                    break;
            }
            if (!valid)
            {
                MessageBox.Show($"File type does not match the expected type for this link.\nExpected: {expected}\nActual: {ext}", "File Type Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                UpdateStatus($"Opened document: {System.IO.Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening document: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("URL is empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                UpdateStatus($"Opened URL: {url}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening URL: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenExcelData(ExcelRange? excelRange)
        {
            if (excelRange == null || string.IsNullOrEmpty(excelRange.FilePath))
            {
                MessageBox.Show("Excel data not configured.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = excelRange.FilePath,
                    UseShellExecute = true
                });
                UpdateStatus($"Opened Excel file: {System.IO.Path.GetFileName(excelRange.FilePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Excel file: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectObject(UIElement? element)
        {
            // Clear previous selection
            if (selectedObject != null)
            {
                if (selectedObject is Shape shape)
                    shape.Effect = null;
                else if (selectedObject is TextBlock tb)
                    tb.Effect = null;
                else if (selectedObject is Image img)
                    img.Effect = null;
            }
            selectedObject = element;
            if (selectedObject != null)
            {
                var effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Blue,
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Opacity = 0.7
                };
                if (selectedObject is Shape shape)
                    shape.Effect = effect;
                else if (selectedObject is TextBlock tb)
                    tb.Effect = effect;
                else if (selectedObject is Image img)
                    img.Effect = effect;
                UpdateStatus($"Selected: {((selectedObject as FrameworkElement)?.Tag as ExploderObject)?.ObjectName ?? "Unknown"}");
            }
        }

        private void ShowObjectProperties(ExploderObject obj, FrameworkElement element, ProjectData project, PageData page)
        {
            try
            {
                if (obj == null || element == null || project == null || page == null)
                {
                    UpdateStatus("Cannot show properties: missing object, element, project, or page");
                    return;
                }
                
                // Sanitize the object to prevent crashes from invalid data
                obj.Sanitize();
                
                var propertiesWindow = new ObjectPropertiesWindow();
                propertiesWindow.SetObject(obj, element, project, page);
                propertiesWindow.Owner = this;
                
                if (propertiesWindow.ShowDialog() == true)
                {
                    // Update the object in the current page
                    var index = page.Objects.IndexOf(obj);
                    if (index >= 0)
                    {
                        page.Objects[index] = obj;
                        
                        // Remove existing text overlays
                        RemoveTextOverlays(obj);
                        
                        // Remove the old element
                        drawingCanvas.Children.Remove(element);
                        
                        // Create new element with updated properties
                        CreateUIElementFromObject(obj);
                    }
                    
                    // Check if a new page was created or an existing page was linked
                    if (propertiesWindow.NewlyCreatedPage != null)
                    {
                        // Navigate to the newly created or linked page
                        currentPage = propertiesWindow.NewlyCreatedPage;
                        LoadPage(currentPage);
                        UpdatePageInfo();
                        UpdateStatus($"Navigated to page: {currentPage.PageName}");
                    }
                    else
                    {
                        UpdateStatus("Object properties updated");
                    }
                }
                else
                {
                    UpdateStatus("Object properties dialog cancelled");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ShowObjectProperties: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private void RemoveTextOverlays(ExploderObject obj)
        {
            // Remove text overlays associated with this object
            var textOverlaysToRemove = drawingCanvas.Children.OfType<TextBlock>()
                .Where(tb => tb.Tag is TextOverlayInfo tag && tag.ObjectData == obj)
                .ToList();

            foreach (var textOverlay in textOverlaysToRemove)
            {
                drawingCanvas.Children.Remove(textOverlay);
            }
        }

        // Menu handlers
        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            var projectWindow = new ProjectOpenWindow();
            if (projectWindow.ShowDialog() == true && projectWindow.ProjectData != null)
            {
                currentProject = projectWindow.ProjectData;
                currentPage = currentProject.Pages.FirstOrDefault();
                
                if (currentPage != null)
                {
                    LoadPage(currentPage);
                    UpdateProjectInfo();
                }
                
                UpdateStatus("New project created");
            }
        }

        public void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Exploder Project",
                Filter = "Exploder Project (*.exp)|*.exp|All Files (*.*)|*.*",
                DefaultExt = ".exp"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadProjectFromFile(dialog.FileName);
            }
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject != null)
            {
                SaveProject();
            }
            else
            {
                MenuSaveAs_Click(sender, e);
            }
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Exploder Project",
                Filter = "Exploder Project (*.exp)|*.exp|All Files (*.*)|*.*",
                DefaultExt = ".exp"
            };

            if (dialog.ShowDialog() == true)
            {
                SaveProjectToFile(dialog.FileName);
            }
        }

        private void MenuPrint_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage == null)
            {
                MessageBox.Show("No page to print. Please create or open a project first.", "No Page", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Create a visual representation of the current page for printing
                    var printVisual = CreatePrintVisual();
                    printDialog.PrintVisual(printVisual, $"Exploder - {currentPage.PageName}");
                    UpdateStatus("Page printed successfully");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing: {ex.Message}", "Print Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FrameworkElement CreatePrintVisual()
        {
            // Create a canvas that represents the current page for printing
            var printCanvas = new Canvas
            {
                Width = currentPage?.PageSettings.Width ?? 800,
                Height = currentPage?.PageSettings.Height ?? 600,
                Background = drawingCanvas.Background
            };

            // Add project and page info
            var titleText = new TextBlock
            {
                Text = $"Project: {currentProject?.ProjectName ?? "Unknown"} - Page: {currentPage?.PageName ?? "Unknown"}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(20, 20, 20, 0)
            };
            printCanvas.Children.Add(titleText);

            // Clone all visible objects for printing
            foreach (var child in drawingCanvas.Children)
            {
                if (child is FrameworkElement element && element.Visibility == Visibility.Visible)
                {
                    var clone = CloneUIElement(element);
                    if (clone != null)
                    {
                        Canvas.SetLeft(clone, Canvas.GetLeft(element));
                        Canvas.SetTop(clone, Canvas.GetTop(element));
                        Canvas.SetZIndex(clone, Canvas.GetZIndex(element));
                        printCanvas.Children.Add(clone);
                    }
                }
            }

            return printCanvas;
        }

        private UIElement? CloneUIElement(UIElement element)
        {
            if (element is Shape shape)
            {
                if (shape is Ellipse ellipse)
                {
                    return new Ellipse
                    {
                        Width = ellipse.Width,
                        Height = ellipse.Height,
                        Fill = ellipse.Fill,
                        Stroke = ellipse.Stroke,
                        StrokeThickness = ellipse.StrokeThickness,
                        Opacity = ellipse.Opacity
                    };
                }
                else if (shape is Rectangle rect)
                {
                    return new Rectangle
                    {
                        Width = rect.Width,
                        Height = rect.Height,
                        Fill = rect.Fill,
                        Stroke = rect.Stroke,
                        StrokeThickness = rect.StrokeThickness,
                        Opacity = rect.Opacity,
                        RadiusX = rect.RadiusX,
                        RadiusY = rect.RadiusY
                    };
                }
                else if (shape is Polygon poly)
                {
                    return new Polygon
                    {
                        Points = poly.Points,
                        Fill = poly.Fill,
                        Stroke = poly.Stroke,
                        StrokeThickness = poly.StrokeThickness,
                        Opacity = poly.Opacity
                    };
                }
                else if (shape is Line line)
                {
                    return new Line
                    {
                        X1 = line.X1,
                        Y1 = line.Y1,
                        X2 = line.X2,
                        Y2 = line.Y2,
                        Stroke = line.Stroke,
                        StrokeThickness = line.StrokeThickness,
                        Opacity = line.Opacity
                    };
                }
            }
            else if (element is TextBlock textBlock)
            {
                return new TextBlock
                {
                    Text = textBlock.Text,
                    FontFamily = textBlock.FontFamily,
                    FontSize = textBlock.FontSize,
                    FontWeight = textBlock.FontWeight,
                    Foreground = textBlock.Foreground,
                    Opacity = textBlock.Opacity
                };
            }
            else if (element is Image image)
            {
                return new Image
                {
                    Source = image.Source,
                    Width = image.Width,
                    Height = image.Height,
                    Stretch = image.Stretch,
                    Opacity = image.Opacity
                };
            }

            return null;
        }

        private async void MenuPublish_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null)
            {
                MessageBox.Show("No project to publish. Please create or open a project first.", "No Project", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Publish Project",
                Filter = "Exploder Published Project (*.exp)|*.exp|ZIP Package (*.zip)|*.zip|All Files (*.*)|*.*",
                DefaultExt = ".exp",
                FileName = $"{currentProject.ProjectName}_Published"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    UpdateStatus("Publishing project...");
                    
                    bool success = false;
                    string resultPath = string.Empty;

                    if (dialog.FileName.EndsWith(".zip"))
                    {
                        // Create self-executing package
                        resultPath = await _publishingService.CreateSelfExecutingPackageAsync(currentProject, dialog.FileName);
                        success = !string.IsNullOrEmpty(resultPath);
                    }
                    else
                    {
                        // Create published project file
                        success = await _publishingService.PublishProjectAsync(currentProject, dialog.FileName);
                        resultPath = dialog.FileName;
                    }

                    if (success)
                    {
                        UpdateStatus($"Project published successfully: {System.IO.Path.GetFileName(resultPath)}");
                        MessageBox.Show($"Project published successfully!\n\nFile: {resultPath}", "Publishing Complete", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        UpdateStatus("Publishing failed");
                        MessageBox.Show("Failed to publish project. Please check that all referenced files exist.", "Publishing Failed", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("Publishing failed");
                    MessageBox.Show($"Error publishing project: {ex.Message}", "Publishing Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MenuObjects_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Objects menu functionality will be implemented in future versions.", 
                "Objects", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuCreatePage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentProject == null)
                {
                    MessageBox.Show("No project loaded. Please create or open a project first.", "No Project", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var pageCreationWindow = new PageCreationWindow(currentProject);
                pageCreationWindow.Owner = this;
                
                if (pageCreationWindow.ShowDialog() == true && pageCreationWindow.CreatedPage != null)
                {
                    // Add the new page to the project
                    currentProject.Pages.Add(pageCreationWindow.CreatedPage);
                    
                    // Switch to the new page
                    currentPage = pageCreationWindow.CreatedPage;
                    LoadPage(currentPage);
                    UpdateProjectInfo();
                    
                    UpdateStatus($"New page '{pageCreationWindow.CreatedPage.PageName}' created and loaded");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating page: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuDocuments_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Documents menu - coming soon!", "Documents", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuStandardPdf_Click(object sender, RoutedEventArgs e)
        {
            AddStandardDocument(LinkFileType.PDF, "PDF Document");
        }

        private void MenuStandardWord_Click(object sender, RoutedEventArgs e)
        {
            AddStandardDocument(LinkFileType.Word, "Word Document");
        }

        private void MenuStandardExcel_Click(object sender, RoutedEventArgs e)
        {
            AddStandardDocument(LinkFileType.Excel, "Excel Spreadsheet");
        }

        private void MenuStandardVideo_Click(object sender, RoutedEventArgs e)
        {
            AddStandardDocument(LinkFileType.Video, "Video File");
        }

        private void MenuStandardImage_Click(object sender, RoutedEventArgs e)
        {
            LoadImageFromFile();
        }

        private void AddStandardDocument(LinkFileType fileType, string title)
        {
            var dialog = new OpenFileDialog
            {
                Title = $"Select {title}",
                Filter = GetFileFilter(fileType)
            };

            if (dialog.ShowDialog() == true)
            {
                // Create a rectangle object that links to the selected document
                var obj = new ExploderObject
                {
                    ObjectId = Guid.NewGuid().ToString(),
                    ObjectName = $"{title}_{currentPage?.Objects.Count + 1}",
                    ObjectType = "Rectangle",
                    Left = 100,
                    Top = 100,
                    Width = 150,
                    Height = 50,
                    FillColor = "#00FFFFFF", // Transparent
                    StrokeColor = "#0000FF", // Blue border
                    StrokeThickness = 2,
                    Opacity = 1.0,
                    ZIndex = currentPage?.Objects.Count ?? 0,
                    LinkType = LinkType.Document,
                    LinkFileType = fileType,
                    LinkDocumentPath = dialog.FileName,
                    Text = System.IO.Path.GetFileName(dialog.FileName)
                };

                currentPage?.Objects.Add(obj);
                CreateUIElementFromObject(obj);
                UpdateStatus($"Added {title}: {System.IO.Path.GetFileName(dialog.FileName)}");
            }
        }

        private string GetFileFilter(LinkFileType fileType)
        {
            return fileType switch
            {
                LinkFileType.PDF => "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                LinkFileType.Word => "Word Files (*.docx;*.doc)|*.docx;*.doc|All Files (*.*)|*.*",
                LinkFileType.Excel => "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                LinkFileType.Video => "Video Files (*.mp4;*.avi;*.mov;*.wmv)|*.mp4;*.avi;*.mov;*.wmv|All Files (*.*)|*.*",
                _ => "All Files (*.*)|*.*"
            };
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
            
            MessageBox.Show(
                $"Exploder - Interactive Documentation System\n\n" +
                $"Version: {versionString}\n" +
                $"Build Date: {System.IO.File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location):yyyy-MM-dd}\n\n" +
                $"A top-down documentation program for creating interactive documentation with multiple layers.\n\n" +
                $"© 2025 Exploder Development Team\n" +
                $"All rights reserved.",
                "About Exploder",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void MenuHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var helpPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HELP.md");
                if (File.Exists(helpPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = helpPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("Help file not found. Please refer to the README.md file for documentation.", 
                        "Help", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening help: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Tool button handlers
        private void btnViewMode_Click(object sender, RoutedEventArgs e) => SetMode(AppMode.View);
        private void btnEditMode_Click(object sender, RoutedEventArgs e) => SetMode(AppMode.Apply);

        private void btnCircleTool_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            circleToolActive = true;
            UpdateToolButtons();
            UpdateStatus("Circle tool active - click and drag to draw");
        }

        private void btnSquareTool_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            rectangleToolActive = true;
            UpdateToolButtons();
            UpdateStatus("Rectangle tool active - click and drag to draw");
        }

        private void btnRoundedRectTool_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            roundedRectToolActive = true;
            UpdateToolButtons();
            UpdateStatus("Rounded rectangle tool active - click and drag to draw");
        }

        private void btnTriangleTool_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            triangleToolActive = true;
            UpdateToolButtons();
            UpdateStatus("Triangle tool active. Drag to draw a triangle.");
        }

        private void btnLineTool_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            lineToolActive = true;
            UpdateToolButtons();
            UpdateStatus("Line tool active - click and drag to draw");
        }

        private void btnTextTool_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            textToolActive = true;
            UpdateToolButtons();
            UpdateStatus("Text tool active - click to add text");
        }

        private void btnUrlTool_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            urlToolActive = true;
            UpdateToolButtons();
            UpdateStatus("URL tool active - click to add URL link");
        }

        private void btnImageTool_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            LoadImageFromFile();
        }

        private void btnDeleteTool_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            // Toggle delete tool
            deleteToolActive = !deleteToolActive;
            
            // Deactivate other tools
            if (deleteToolActive)
            {
                DeactivateAllTools();
                deleteToolActive = true;
                drawingCanvas.Cursor = Cursors.Cross;
                UpdateStatus("Delete tool active - click objects to delete them (D to toggle)");
            }
            else
            {
                DeactivateAllTools();
                drawingCanvas.Cursor = Cursors.Arrow;
                UpdateStatus("Delete tool deactivated");
            }
            
            UpdateToolButtons();
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            if (undoStack.Count > 0 && currentPage != null)
            {
                // Save current state to redo stack
                var currentState = ClonePageData(currentPage);
                redoStack.Push(currentState);
                
                // Restore previous state
                var previousState = undoStack.Pop();
                RestorePageState(previousState);
                UpdateStatus("Undo completed");
            }
            else
            {
                UpdateStatus("Nothing to undo");
            }
        }

        private void btnRedo_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            if (redoStack.Count > 0 && currentPage != null)
            {
                // Save current state to undo stack
                var currentState = ClonePageData(currentPage);
                undoStack.Push(currentState);
                
                // Restore next state
                var nextState = redoStack.Pop();
                RestorePageState(nextState);
                UpdateStatus("Redo completed");
            }
            else
            {
                UpdateStatus("Nothing to redo");
            }
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            if (selectedObject is FrameworkElement fe && fe.Tag is ExploderObject obj)
            {
                clipboardObject = CloneObject(obj);
                UpdateStatus("Object copied to clipboard");
            }
            else
            {
                UpdateStatus("No object selected to copy");
            }
        }

        private void btnCut_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            if (selectedObject is FrameworkElement fe && fe.Tag is ExploderObject obj)
            {
                // Save state for undo
                SaveStateForUndo();
                
                // Copy to clipboard first
                clipboardObject = CloneObject(obj);
                
                // Remove from canvas and page
                drawingCanvas.Children.Remove(selectedObject);
                currentPage?.Objects.Remove(obj);
                
                // Clear selection
                selectedObject = null;
                
                UpdateObjectCount();
                UpdateStatus("Object cut to clipboard");
            }
            else
            {
                UpdateStatus("No object selected to cut");
            }
        }

        private void btnPaste_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            if (clipboardObject != null && currentPage != null)
            {
                // Save state for undo
                SaveStateForUndo();
                
                var pastedObject = CloneObject(clipboardObject);
                pastedObject.ObjectId = Guid.NewGuid().ToString();
                pastedObject.Left += 20; // Offset slightly from original
                pastedObject.Top += 20;
                
                currentPage.Objects.Add(pastedObject);
                CreateUIElementFromObject(pastedObject);
                UpdateObjectCount();
                UpdateStatus("Object pasted");
            }
            else
            {
                UpdateStatus("No object in clipboard to paste");
            }
        }

        private ExploderObject CloneObject(ExploderObject original)
        {
            return new ExploderObject
            {
                ObjectId = Guid.NewGuid().ToString(),
                ObjectName = original.ObjectName + " (Copy)",
                ObjectType = original.ObjectType,
                Left = original.Left,
                Top = original.Top,
                Width = original.Width,
                Height = original.Height,
                FillColor = original.FillColor,
                StrokeColor = original.StrokeColor,
                StrokeThickness = original.StrokeThickness,
                Opacity = original.Opacity,
                Text = original.Text,
                FontFamily = original.FontFamily,
                FontSize = original.FontSize,
                FontWeight = original.FontWeight,
                TextColor = original.TextColor,
                X1 = original.X1,
                Y1 = original.Y1,
                X2 = original.X2,
                Y2 = original.Y2,
                ImagePath = original.ImagePath,
                ImageSource = original.ImageSource,
                LinkType = original.LinkType,
                LinkTarget = original.LinkTarget,
                LinkPageId = original.LinkPageId,
                LinkDocumentPath = original.LinkDocumentPath,
                LinkUrl = original.LinkUrl,
                ExcelRange = original.ExcelRange,
                ZIndex = original.ZIndex,
                GroupId = original.GroupId,
                IsGrouped = original.IsGrouped
            };
        }

        private PageData ClonePageData(PageData original)
        {
            var clonedPage = new PageData
            {
                PageId = original.PageId,
                PageName = original.PageName,
                Objects = new List<ExploderObject>()
            };

            foreach (var obj in original.Objects)
            {
                clonedPage.Objects.Add(CloneObject(obj));
            }

            return clonedPage;
        }

        private void RestorePageState(PageData pageState)
        {
            if (currentPage == null) return;

            // Clear current canvas
            drawingCanvas.Children.Clear();
            selectedObject = null;

            // Restore page data
            currentPage.Objects.Clear();
            foreach (var obj in pageState.Objects)
            {
                currentPage.Objects.Add(CloneObject(obj));
            }

            // Recreate UI elements
            foreach (var obj in currentPage.Objects)
            {
                CreateUIElementFromObject(obj);
            }

            UpdateObjectCount();
        }

        private void SaveStateForUndo()
        {
            if (currentPage == null) return;

            // Clear redo stack when new action is performed
            redoStack.Clear();

            // Save current state to undo stack
            var currentState = ClonePageData(currentPage);
            undoStack.Push(currentState);

            // Limit undo stack size
            if (undoStack.Count > MAX_UNDO_STEPS)
            {
                var tempStack = new Stack<PageData>();
                for (int i = 0; i < MAX_UNDO_STEPS; i++)
                {
                    tempStack.Push(undoStack.Pop());
                }
                undoStack = tempStack;
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            // Switch to EDIT mode if not already in it
            if (currentMode != AppMode.Apply)
            {
                SetMode(AppMode.Apply);
            }
            
            if (selectedObject is FrameworkElement fe && fe.Tag is ExploderObject obj)
            {
                // Show confirmation dialog
                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{obj.ObjectName}' ({obj.ObjectType})?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Save state for undo
                    SaveStateForUndo();
                    
                    // Remove the object from the page data
                    currentPage?.Objects.Remove(obj);
                    
                    // Remove the UI element from the canvas
                    drawingCanvas.Children.Remove(selectedObject);
                    
                    // Remove any associated text overlays
                    RemoveTextOverlays(obj);
                    
                    // Clear selection
                    selectedObject = null;
                    
                    // Update UI
                    UpdateObjectCount();
                    UpdateObjectsTree();
                    UpdateStatus($"Deleted: {obj.ObjectName} ({obj.ObjectType})");
                }
                else
                {
                    UpdateStatus("Delete cancelled");
                }
            }
            else
            {
                UpdateStatus("No object selected to delete. Use the delete tool (🗑️) or select an object first.");
            }
        }

        private void btnBackToPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (pageHistory.Count > 0)
            {
                var previousPage = pageHistory.Pop();
                LoadPage(previousPage);
                UpdateStatus($"Returned to: {previousPage.PageName}");
            }
        }

        private void btnBackToMain_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject != null)
            {
                var mainPage = currentProject.Pages.FirstOrDefault();
                if (mainPage != null && mainPage != currentPage)
                {
                    pageHistory.Clear();
                    LoadPage(mainPage);
                    UpdateStatus($"Returned to main page: {mainPage.PageName}");
                }
            }
        }

        // Drawing event handlers
        private void drawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currentMode != AppMode.Apply) return;

            // Don't start drawing if delete tool is active
            if (deleteToolActive) return;

            startPoint = e.GetPosition(drawingCanvas);
            isDrawing = true;

            if (textToolActive)
            {
                CreateTextAtPoint(startPoint);
            }
            else if (urlToolActive)
            {
                CreateUrlAtPoint(startPoint);
            }
        }

        private void drawingCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var currentPoint = e.GetPosition(drawingCanvas);
            UpdateMousePosition(currentPoint);

            // Handle object dragging in edit mode
            if (isDraggingObject && draggingObject != null && currentMode == AppMode.Apply)
            {
                var delta = currentPoint - dragStartPoint;
                var newLeft = objectStartPoint.X + delta.X;
                var newTop = objectStartPoint.Y + delta.Y;
                
                Canvas.SetLeft(draggingObject, newLeft);
                Canvas.SetTop(draggingObject, newTop);
                
                // Update the object data
                if (draggingObject is FrameworkElement fe && fe.Tag is ExploderObject obj)
                {
                    obj.Left = newLeft;
                    obj.Top = newTop;
                }
                
                UpdateStatus($"Dragging object to ({newLeft:F0}, {newTop:F0})");
                return;
            }

            // Handle shape drawing in edit mode
            if (!isDrawing || currentMode != AppMode.Apply) return;

            if (currentShape != null)
            {
                UpdateShape(currentPoint);
            }
            else
            {
                CreateShape(startPoint, currentPoint);
            }
        }

        private void drawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Handle object dragging completion
            if (isDraggingObject && draggingObject != null && currentMode == AppMode.Apply)
            {
                // Save state for undo after the move is completed
                SaveStateForUndo();
                
                isDraggingObject = false;
                draggingObject = null;
                drawingCanvas.ReleaseMouseCapture();
                UpdateStatus("Object moved");
                return;
            }

            // Handle shape drawing completion
            if (!isDrawing || currentMode != AppMode.Apply) return;

            isDrawing = false;
            if (currentShape != null)
            {
                // Save state for undo before creating the new object
                SaveStateForUndo();
                
                // Create an ExploderObject from the drawn shape with green color
                var obj = CreateExploderObjectFromShape(currentShape);
                if (obj != null && currentPage != null)
                {
                    // Add to current page
                    currentPage.Objects.Add(obj);
                    
                    // Create the permanent green shape
                    CreateUIElementFromObject(obj);
                    
                    UpdateObjectCount();
                    UpdateObjectsTree();
                    UpdateStatus($"Created {obj.ObjectType} - right-click to set properties");
                }
                
                // Remove the temporary drawn shape
                drawingCanvas.Children.Remove(currentShape);
            }
            currentShape = null;
            DeactivateAllTools();
            UpdateToolButtons();
        }

        private void RemoveAllResizeHandles()
        {
            // Remove all resize handles (small blue rectangles)
            var handlesToRemove = drawingCanvas.Children.OfType<Rectangle>()
                .Where(r => r.Width == 8 && r.Height == 8 && r.Fill == Brushes.Blue)
                .ToList();

            foreach (var handle in handlesToRemove)
            {
                drawingCanvas.Children.Remove(handle);
            }
        }

        private void drawingCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Handle right-click on canvas
            e.Handled = true;
        }

        // Helper methods
        private void SetMode(AppMode mode)
        {
            currentMode = mode;
            DeactivateAllTools();
            UpdateToolButtons();
            UpdateCurrentMode();
            UpdateModeToggle(mode);

            // Remove any existing view mode buttons
            foreach (var btn in viewModeButtons)
                drawingCanvas.Children.Remove(btn);
            viewModeButtons.Clear();

            // Remove resize handles when leaving edit mode
            if (mode != AppMode.Apply)
            {
                RemoveAllResizeHandles();
            }

            // Reset cursor when changing modes
            if (mode != AppMode.Apply || !deleteToolActive)
            {
                drawingCanvas.Cursor = Cursors.Arrow;
            }

            switch (mode)
            {
                case AppMode.View:
                    UpdateStatus("View mode - click objects to navigate");
                    // Remove all existing buttons when entering view mode
                    foreach (var btn in viewModeButtons.ToList())
                    {
                        drawingCanvas.Children.Remove(btn);
                    }
                    viewModeButtons.Clear();
                    
                    // Create transparent buttons for all objects in view mode
                    if (currentPage?.Objects != null)
                    {
                        foreach (var obj in currentPage.Objects)
                        {
                            CreateButtonFromShapeOnly(obj, obj.Left, obj.Top, obj.Width, obj.Height);
                        }
                    }
                    break;
                case AppMode.Apply:
                    UpdateStatus("Edit mode - use tools to add buttons, click to edit, drag to move");
                    // Remove all view mode buttons when entering edit mode
                    foreach (var btn in viewModeButtons.ToList())
                    {
                        drawingCanvas.Children.Remove(btn);
                    }
                    viewModeButtons.Clear();
                    
                    // Add resize handles to all images
                    foreach (var child in drawingCanvas.Children.OfType<Image>())
                    {
                        AddResizeHandles(child);
                    }
                    break;
            }
        }

        private void DeactivateAllTools()
        {
            circleToolActive = false;
            rectangleToolActive = false;
            roundedRectToolActive = false;
            triangleToolActive = false;
            lineToolActive = false;
            textToolActive = false;
            urlToolActive = false;
            deleteToolActive = false;
        }

        private void UpdateToolButtons()
        {
            // Drawing tools - always enabled but with opacity control for visual feedback
            btnCircleTool.IsEnabled = true;
            btnSquareTool.IsEnabled = true;
            btnRoundedRectTool.IsEnabled = true;
            btnTriangleTool.IsEnabled = true;
            btnLineTool.IsEnabled = true;
            btnTextTool.IsEnabled = true;
            btnUrlTool.IsEnabled = true;
            btnImageTool.IsEnabled = true;
            btnDeleteTool.IsEnabled = true;
            
            // Edit tools - always enabled but with opacity control for visual feedback
            btnUndo.IsEnabled = true;
            btnRedo.IsEnabled = true;
            btnCopy.IsEnabled = true;
            btnPaste.IsEnabled = true;
            btnDelete.IsEnabled = true;
            
            // Set opacity based on mode for visual feedback
            double enabledOpacity = 1.0;
            double disabledOpacity = 0.5;
            
            if (currentMode == AppMode.Apply)
            {
                // EDIT mode - full opacity
                btnCircleTool.Opacity = enabledOpacity;
                btnSquareTool.Opacity = enabledOpacity;
                btnRoundedRectTool.Opacity = enabledOpacity;
                btnTriangleTool.Opacity = enabledOpacity;
                btnLineTool.Opacity = enabledOpacity;
                btnTextTool.Opacity = enabledOpacity;
                btnUrlTool.Opacity = enabledOpacity;
                btnImageTool.Opacity = enabledOpacity;
                btnDeleteTool.Opacity = deleteToolActive ? 0.8 : enabledOpacity; // Highlight when active
                btnUndo.Opacity = enabledOpacity;
                btnRedo.Opacity = enabledOpacity;
                btnCopy.Opacity = enabledOpacity;
                btnPaste.Opacity = enabledOpacity;
                btnDelete.Opacity = enabledOpacity;
            }
            else
            {
                // VIEW mode - reduced opacity to show they're not active
                btnCircleTool.Opacity = disabledOpacity;
                btnSquareTool.Opacity = disabledOpacity;
                btnRoundedRectTool.Opacity = disabledOpacity;
                btnTriangleTool.Opacity = disabledOpacity;
                btnLineTool.Opacity = disabledOpacity;
                btnTextTool.Opacity = disabledOpacity;
                btnUrlTool.Opacity = disabledOpacity;
                btnImageTool.Opacity = disabledOpacity;
                btnDeleteTool.Opacity = disabledOpacity;
                btnUndo.Opacity = disabledOpacity;
                btnRedo.Opacity = disabledOpacity;
                btnCopy.Opacity = disabledOpacity;
                btnPaste.Opacity = disabledOpacity;
                btnDelete.Opacity = disabledOpacity;
            }
        }

        private void UpdateModeToggle(AppMode mode)
        {
            var slider = modeSlider;
            var viewButton = btnViewMode;
            var editButton = btnEditMode;

            if (mode == AppMode.View)
            {
                // Move slider to View position (left)
                slider.HorizontalAlignment = HorizontalAlignment.Left;
                slider.Margin = new Thickness(2, 2, 0, 0);
                
                // Update button colors
                viewButton.Foreground = (SolidColorBrush)FindResource("TextPrimary");
                editButton.Foreground = (SolidColorBrush)FindResource("TextSecondary");
            }
            else if (mode == AppMode.Apply)
            {
                // Move slider to Edit position (right)
                slider.HorizontalAlignment = HorizontalAlignment.Right;
                slider.Margin = new Thickness(0, 2, 2, 0);
                
                // Update button colors
                viewButton.Foreground = (SolidColorBrush)FindResource("TextSecondary");
                editButton.Foreground = (SolidColorBrush)FindResource("TextPrimary");
            }
        }

        private void CreateShape(System.Windows.Point start, System.Windows.Point current)
        {
            if (currentShape != null) return;

            if (circleToolActive)
            {
                var ellipse = new Ellipse
                {
                    Stroke = Brushes.Black,
                    Fill = new SolidColorBrush(Colors.LightGreen),
                    StrokeThickness = 2,
                    Opacity = 0.3 // 30% transparency during drawing
                };
                currentShape = ellipse;
                drawingCanvas.Children.Add(ellipse);
            }
            else if (rectangleToolActive)
            {
                var rect = new Rectangle
                {
                    Stroke = Brushes.Black,
                    Fill = new SolidColorBrush(Colors.LightGreen),
                    StrokeThickness = 2,
                    Opacity = 0.3 // 30% transparency during drawing
                };
                currentShape = rect;
                drawingCanvas.Children.Add(rect);
            }
            else if (roundedRectToolActive)
            {
                var rect = new Rectangle
                {
                    Stroke = Brushes.Black,
                    Fill = new SolidColorBrush(Colors.LightGreen),
                    StrokeThickness = 2,
                    RadiusX = 10,
                    RadiusY = 10,
                    Opacity = 0.3 // 30% transparency during drawing
                };
                currentShape = rect;
                drawingCanvas.Children.Add(rect);
            }
            else if (triangleToolActive)
            {
                var triangle = new Polygon
                {
                    Stroke = Brushes.Black,
                    Fill = new SolidColorBrush(Colors.LightGreen),
                    StrokeThickness = 2,
                    Opacity = 0.3 // 30% transparency during drawing
                };
                currentShape = triangle;
                drawingCanvas.Children.Add(triangle);
            }
            else if (lineToolActive)
            {
                var line = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    X1 = start.X,
                    Y1 = start.Y,
                    X2 = current.X,
                    Y2 = current.Y,
                    Opacity = 0.3 // 30% transparency during drawing
                };
                currentShape = line;
                drawingCanvas.Children.Add(line);
            }
        }

        private void UpdateShape(System.Windows.Point current)
        {
            if (currentShape is Ellipse ellipse)
            {
                var width = Math.Abs(current.X - startPoint.X);
                var height = Math.Abs(current.Y - startPoint.Y);
                ellipse.Width = width;
                ellipse.Height = height;
                Canvas.SetLeft(ellipse, Math.Min(startPoint.X, current.X));
                Canvas.SetTop(ellipse, Math.Min(startPoint.Y, current.Y));
            }
            else if (currentShape is Rectangle rect)
            {
                var width = Math.Abs(current.X - startPoint.X);
                var height = Math.Abs(current.Y - startPoint.Y);
                rect.Width = width;
                rect.Height = height;
                Canvas.SetLeft(rect, Math.Min(startPoint.X, current.X));
                Canvas.SetTop(rect, Math.Min(startPoint.Y, current.Y));
            }
            else if (currentShape is Polygon triangle)
            {
                var width = Math.Abs(current.X - startPoint.X);
                var height = Math.Abs(current.Y - startPoint.Y);
                var points = new PointCollection
                {
                    new Point(width / 2, 0),
                    new Point(0, height),
                    new Point(width, height)
                };
                triangle.Points = points;
                Canvas.SetLeft(triangle, Math.Min(startPoint.X, current.X));
                Canvas.SetTop(triangle, Math.Min(startPoint.Y, current.Y));
            }
            else if (currentShape is Line line)
            {
                line.X2 = current.X;
                line.Y2 = current.Y;
            }
        }

        private void CreateButtonFromShape(UIElement shape, ExploderObject obj)
        {
            try
            {
                double left = Canvas.GetLeft(shape);
                double top = Canvas.GetTop(shape);
                double width = (shape as FrameworkElement)?.Width ?? 100;
                double height = (shape as FrameworkElement)?.Height ?? 100;

                // Special handling for Polygon (triangle)
                if (shape is Polygon poly && poly.Points.Count > 0)
                {
                    double minX = poly.Points.Min(p => p.X);
                    double minY = poly.Points.Min(p => p.Y);
                    double maxX = poly.Points.Max(p => p.X);
                    double maxY = poly.Points.Max(p => p.Y);
                    width = maxX - minX;
                    height = maxY - minY;
                    left += minX;
                    top += minY;
                }

                // Create a button that matches the shape color exactly
                var button = new Button
                {
                    Opacity = 0.0, // Completely transparent (invisible)
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Background = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.FillColor) ?? Brushes.Transparent,
                    Width = width,
                    Height = height,
                    Tag = obj, // Store the ExploderObject
                    Content = "", // No visible content
                    Cursor = Cursors.Hand
                };

                Canvas.SetLeft(button, left);
                Canvas.SetTop(button, top);
                Canvas.SetZIndex(button, obj.ZIndex + 1); // Ensure button is above the shape

                // Add event handlers
                button.Click += (s, e) =>
                {
                    HandleObjectClick(button);
                };

                button.MouseRightButtonDown += (s, e) =>
                {
                    ShowObjectProperties(obj, button, currentProject, currentPage);
                    e.Handled = true;
                };

                // Add the button to the canvas
                drawingCanvas.Children.Add(button);
                viewModeButtons.Add(button);

                // Add event handlers to the original shape for right-click
                if (shape is FrameworkElement fe)
                {
                    fe.MouseRightButtonDown += (s, e) =>
                    {
                        ShowObjectProperties(obj, fe, currentProject, currentPage);
                        e.Handled = true;
                    };
                }

                UpdateStatus($"Created button for {obj.ObjectName} - click to navigate, right-click for properties");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating button from shape: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ExploderObject? CreateExploderObjectFromShape(UIElement shape)
        {
            try
            {
                if (shape == null) return null;

                double width = 100;
                double height = 100;

                // Calculate dimensions based on shape type
                if (shape is Ellipse ellipse)
                {
                    width = ellipse.Width;
                    height = ellipse.Height;
                }
                else if (shape is Rectangle rect)
                {
                    width = rect.Width;
                    height = rect.Height;
                }
                else if (shape is Polygon poly && poly.Points.Count > 0)
                {
                    // Calculate bounding box for triangle
                    double minX = poly.Points.Min(p => p.X);
                    double minY = poly.Points.Min(p => p.Y);
                    double maxX = poly.Points.Max(p => p.X);
                    double maxY = poly.Points.Max(p => p.Y);
                    width = maxX - minX;
                    height = maxY - minY;
                }
                else if (shape is Line line)
                {
                    width = Math.Abs(line.X2 - line.X1);
                    height = Math.Abs(line.Y2 - line.Y1);
                }

                var obj = new ExploderObject
                {
                    ObjectId = Guid.NewGuid().ToString(),
                    ObjectName = $"New {GetShapeTypeName(shape)}",
                    Left = Canvas.GetLeft(shape),
                    Top = Canvas.GetTop(shape),
                    Width = width,
                    Height = height,
                    FillColor = "#90EE90", // Set light green color for all drawn shapes
                    StrokeColor = "Black",
                    StrokeThickness = 2,
                    Opacity = 0.3, // 30% transparency for final shapes
                    ZIndex = 0
                };

                // Set object type and specific properties based on shape type
                if (shape is Ellipse)
                {
                    obj.ObjectType = "Circle";
                }
                else if (shape is Rectangle rect)
                {
                    if (rect.RadiusX > 0 || rect.RadiusY > 0)
                    {
                        obj.ObjectType = "RoundedRectangle";
                    }
                    else
                    {
                        obj.ObjectType = "Rectangle";
                    }
                }
                else if (shape is Polygon)
                {
                    obj.ObjectType = "Triangle";
                }
                else if (shape is Line line)
                {
                    obj.ObjectType = "Line";
                    obj.X1 = line.X1;
                    obj.Y1 = line.Y1;
                    obj.X2 = line.X2;
                    obj.Y2 = line.Y2;
                }

                return obj;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating ExploderObject from shape: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void CreateButtonFromShapeOnly(ExploderObject obj, double left, double top, double width, double height)
        {
            try
            {
                // Create a button that matches the shape color exactly
                var button = new Button
                {
                    Opacity = 0.0, // Completely transparent (invisible)
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Background = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.FillColor) ?? Brushes.Transparent,
                    Width = width,
                    Height = height,
                    Tag = obj, // Store the ExploderObject
                    Content = "", // No visible content
                    Cursor = Cursors.Hand
                };

                Canvas.SetLeft(button, left);
                Canvas.SetTop(button, top);
                Canvas.SetZIndex(button, obj.ZIndex);

                // Add event handlers
                button.Click += (s, e) =>
                {
                    HandleObjectClick(button);
                };

                button.MouseRightButtonDown += (s, e) =>
                {
                    // Only show properties window in edit mode
                    if (currentMode == AppMode.Apply)
                    {
                        ShowObjectProperties(obj, button, currentProject, currentPage);
                    }
                    e.Handled = true;
                };

                // Add the button to the canvas
                drawingCanvas.Children.Add(button);
                viewModeButtons.Add(button);

                UpdateStatus($"Created button '{obj.ObjectName}' - click to navigate, right-click for properties");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating button: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateTextAtPoint(System.Windows.Point point)
        {
            var textBox = new TextBox
            {
                Width = 100,
                Height = 20,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Black
            };

            Canvas.SetLeft(textBox, point.X);
            Canvas.SetTop(textBox, point.Y);
            drawingCanvas.Children.Add(textBox);
            textBox.Focus();

            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    var text = textBox.Text;
                    drawingCanvas.Children.Remove(textBox);
                    
                    var textBlock = new TextBlock
                    {
                        Text = text,
                        FontSize = 12,
                        Foreground = Brushes.Black
                    };

                    Canvas.SetLeft(textBlock, point.X);
                    Canvas.SetTop(textBlock, point.Y);
                    drawingCanvas.Children.Add(textBlock);

                    // Create ExploderObject for the text
                    var obj = new ExploderObject
                    {
                        ObjectId = Guid.NewGuid().ToString(),
                        ObjectName = $"Text_{currentPage?.Objects.Count + 1}",
                        ObjectType = "Text",
                        Left = point.X,
                        Top = point.Y,
                        Width = 100,
                        Height = 20,
                        Text = text,
                        FontSize = 12,
                        FontFamily = "Arial",
                        FontWeight = "Normal",
                        FillColor = "#FFFFFF",
                        StrokeColor = "#000000",
                        StrokeThickness = 0,
                        Opacity = 1.0,
                        ZIndex = currentPage?.Objects.Count ?? 0,
                        LinkType = LinkType.None
                    };

                    textBlock.Tag = obj;
                    textBlock.MouseLeftButtonDown += Object_MouseLeftButtonDown;
                    textBlock.MouseRightButtonDown += Object_MouseRightButtonDown;

                    currentPage?.Objects.Add(obj);
                }
            };
        }

        private void CreateUrlAtPoint(System.Windows.Point point)
        {
            var textBox = new TextBox
            {
                Width = 200,
                Height = 20,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Black
            };

            Canvas.SetLeft(textBox, point.X);
            Canvas.SetTop(textBox, point.Y);
            drawingCanvas.Children.Add(textBox);
            textBox.Focus();

            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    var url = textBox.Text;
                    drawingCanvas.Children.Remove(textBox);
                    
                    var textBlock = new TextBlock
                    {
                        Text = url,
                        FontSize = 12,
                        Foreground = Brushes.Blue,
                        TextDecorations = TextDecorations.Underline
                    };

                    Canvas.SetLeft(textBlock, point.X);
                    Canvas.SetTop(textBlock, point.Y);
                    drawingCanvas.Children.Add(textBlock);

                    // Create ExploderObject for the URL
                    var obj = new ExploderObject
                    {
                        ObjectId = Guid.NewGuid().ToString(),
                        ObjectName = $"URL_{currentPage?.Objects.Count + 1}",
                        ObjectType = "Text",
                        Left = point.X,
                        Top = point.Y,
                        Width = 200,
                        Height = 20,
                        Text = url,
                        FontSize = 12,
                        FontFamily = "Arial",
                        FontWeight = "Normal",
                        FillColor = "#FFFFFF",
                        StrokeColor = "#000000",
                        StrokeThickness = 0,
                        Opacity = 1.0,
                        ZIndex = currentPage?.Objects.Count ?? 0,
                        LinkType = LinkType.Url,
                        LinkUrl = url
                    };

                    textBlock.Tag = obj;
                    textBlock.MouseLeftButtonDown += Object_MouseLeftButtonDown;
                    textBlock.MouseRightButtonDown += Object_MouseRightButtonDown;

                    currentPage?.Objects.Add(obj);
                }
            };
        }

        private void LoadImageFromFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Image",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Determine large size and center position
                    double canvasWidth = drawingCanvas.ActualWidth > 0 ? drawingCanvas.ActualWidth : 1200;
                    double canvasHeight = drawingCanvas.ActualHeight > 0 ? drawingCanvas.ActualHeight : 800;
                    double imgWidth = canvasWidth * 0.6;
                    double imgHeight = canvasHeight * 0.6;
                    double left = (canvasWidth - imgWidth) / 2;
                    double top = (canvasHeight - imgHeight) / 2;

                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri(dialog.FileName)),
                        Width = imgWidth,
                        Height = imgHeight,
                        Stretch = Stretch.Uniform
                    };

                    Canvas.SetLeft(image, left);
                    Canvas.SetTop(image, top);
                    drawingCanvas.Children.Add(image);

                    // Create ExploderObject for the image
                    var obj = new ExploderObject
                    {
                        ObjectId = Guid.NewGuid().ToString(),
                        ObjectName = $"Image_{currentPage?.Objects.Count + 1}",
                        ObjectType = "Image",
                        Left = left,
                        Top = top,
                        Width = imgWidth,
                        Height = imgHeight,
                        ImagePath = dialog.FileName,
                        FillColor = "#FFFFFF",
                        StrokeColor = "#000000",
                        StrokeThickness = 0,
                        Opacity = 1.0,
                        ZIndex = currentPage?.Objects.Count ?? 0,
                        LinkType = LinkType.None
                    };

                    image.Tag = obj;
                    image.MouseLeftButtonDown += Object_MouseLeftButtonDown;
                    image.MouseRightButtonDown += Object_MouseRightButtonDown;

                    currentPage?.Objects.Add(obj);
                    UpdateStatus($"Image loaded: {System.IO.Path.GetFileName(dialog.FileName)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            DeactivateAllTools();
            UpdateToolButtons();
        }

        private void SaveProject()
        {
            if (currentProject == null) return;

            try
            {
                currentProject.Sanitize();
                var projectPath = System.IO.Path.Combine(currentProject.ProjectPath, $"{currentProject.ProjectName}.exp");
                SaveProjectToFile(projectPath);
                UpdateStatus($"Project saved: {System.IO.Path.GetFileName(projectPath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProjectToFile(string path)
        {
            if (currentProject == null) return;

            try
            {
                currentProject.Sanitize();
                var json = JsonSerializer.Serialize(currentProject, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(path, json);
                
                // Save to recent projects
                SaveToRecentProjects(path);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save project: {ex.Message}");
            }
        }

        private void SaveToRecentProjects(string path)
        {
            try
            {
                var recentProjectsFile = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "Exploder", "recent_projects.txt");

                var directory = System.IO.Path.GetDirectoryName(recentProjectsFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var recentProjects = new List<string>();
                
                if (File.Exists(recentProjectsFile))
                {
                    recentProjects = File.ReadAllLines(recentProjectsFile).ToList();
                }

                recentProjects.Remove(path);
                recentProjects.Insert(0, path);
                recentProjects = recentProjects.Take(10).ToList();

                File.WriteAllLines(recentProjectsFile, recentProjects);
            }
            catch
            {
                // Silently fail - this is not critical
            }
        }

        private void UpdateProjectInfo()
        {
            if (currentProject != null)
            {
                txtProjectName.Text = currentProject.ProjectName;
                this.Title = $"Exploder - {currentProject.ProjectName}";
            }
        }

        private void UpdatePageInfo()
        {
            if (currentPage != null && currentProject != null)
            {
                // Build breadcrumb path from root to current page
                var path = new List<string>();
                var page = currentPage;
                while (page != null)
                {
                    path.Add(page.PageName);
                    if (string.IsNullOrEmpty(page.ParentPageId))
                        break;
                    page = currentProject.Pages.FirstOrDefault(p => p.PageId == page.ParentPageId);
                }
                path.Reverse();
                txtCurrentPage.Text = string.Join(" / ", path);
                UpdateObjectCount();
            }
        }

        private void UpdateStatus(string message)
        {
            txtStatus.Text = message;
        }

        private void UpdateMousePosition(Point position)
        {
            txtMousePosition.Text = $"{(int)position.X}, {(int)position.Y}";
        }

        private void UpdateObjectCount()
        {
            if (currentPage != null)
            {
                txtObjectCount.Text = currentPage.Objects.Count.ToString();
            }
            else
            {
                txtObjectCount.Text = "0";
            }
        }

        private void UpdateProjectTree()
        {
            if (projectTreeView == null || currentProject == null) return;

            // Clear existing page items
            pagesItem.Items.Clear();

            // Add all pages to the tree
            foreach (var page in currentProject.Pages)
            {
                var pageItem = new TreeViewItem
                {
                    Tag = page, // Store the PageData object for navigation
                    IsExpanded = page == currentPage // Expand current page
                };

                // Add page icon or styling
                var headerStack = new StackPanel { Orientation = Orientation.Horizontal };
                headerStack.Children.Add(new TextBlock 
                { 
                    Text = "📄", 
                    Margin = new Thickness(0, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                headerStack.Children.Add(new TextBlock 
                { 
                    Text = page.PageName,
                    VerticalAlignment = VerticalAlignment.Center
                });
                pageItem.Header = headerStack;

                pagesItem.Items.Add(pageItem);
            }

            // Update object counts in the Objects section
            UpdateObjectsTree();
        }

        private void UpdateObjectsTree()
        {
            if (objectsItem == null || currentPage == null) return;

            // Clear existing object category items
            objectsItem.Items.Clear();

            // Count objects by type
            var shapes = currentPage.Objects.Where(o => o.ObjectType == "Circle" || o.ObjectType == "Rectangle" || 
                                                       o.ObjectType == "Triangle" || o.ObjectType == "Line").ToList();
            var texts = currentPage.Objects.Where(o => o.ObjectType == "Text").ToList();
            var images = currentPage.Objects.Where(o => o.ObjectType == "Image").ToList();

            // Create Shapes category
            var shapesItem = new TreeViewItem
            {
                Header = $"Shapes ({shapes.Count})",
                IsExpanded = true
            };
            objectsItem.Items.Add(shapesItem);

            // Create Text category
            var textsItem = new TreeViewItem
            {
                Header = $"Text ({texts.Count})",
                IsExpanded = true
            };
            objectsItem.Items.Add(textsItem);

            // Create Images category
            var imagesItem = new TreeViewItem
            {
                Header = $"Images ({images.Count})",
                IsExpanded = true
            };
            objectsItem.Items.Add(imagesItem);
        }

        private void ProjectTreeView_Selected(object sender, RoutedEventArgs e)
        {
            if (projectTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is PageData pageData)
            {
                // Navigate to the selected page
                NavigateToPageFromTree(pageData);
            }
        }

        private void NavigateToPageFromTree(PageData targetPage)
        {
            if (targetPage == null || targetPage == currentPage) return;

            // Save current page to history if it exists
            if (currentPage != null)
            {
                pageHistory.Push(currentPage);
            }

            // Load the target page
            LoadPage(targetPage);
            UpdateStatus($"Navigated to: {targetPage.PageName}");
        }

        private void DeleteClickedObject(UIElement? element)
        {
            if (element == null) return;

            // Get the ExploderObject from the element
            ExploderObject? objToDelete = null;
            
            if (element is FrameworkElement fe && fe.Tag is ExploderObject obj)
            {
                objToDelete = obj;
            }
            else if (element is FrameworkElement fe2 && fe2.Tag is TextOverlayInfo textInfo)
            {
                // If it's a text overlay, delete the associated shape
                objToDelete = textInfo.ObjectData;
            }

            if (objToDelete == null)
            {
                UpdateStatus("No object data found to delete");
                return;
            }

            // Show confirmation dialog
            var result = MessageBox.Show(
                $"Are you sure you want to delete '{objToDelete.ObjectName}' ({objToDelete.ObjectType})?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Save state for undo
                SaveStateForUndo();

                // Remove the object from the page data
                currentPage?.Objects.Remove(objToDelete);

                // Remove the UI element from the canvas
                drawingCanvas.Children.Remove(element);

                // Remove any associated text overlays
                RemoveTextOverlays(objToDelete);

                // Clear selection if this was the selected object
                if (selectedObject == element)
                {
                    selectedObject = null;
                }

                // Update UI
                UpdateObjectCount();
                UpdateObjectsTree();
                UpdateStatus($"Deleted: {objToDelete.ObjectName} ({objToDelete.ObjectType})");
            }
            else
            {
                UpdateStatus("Delete cancelled");
            }
        }

        private void UpdateCurrentMode()
        {
            txtCurrentMode.Text = currentMode.ToString();
        }

        // Window Control Methods
        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeWindow_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }



        private string GetShapeTypeName(UIElement shape)
        {
            return shape switch
            {
                Ellipse => "Circle",
                Rectangle => "Rectangle",
                Polygon => "Triangle",
                Line => "Line",
                TextBlock => "Text",
                Image => "Image",
                _ => "Shape"
            };
        }

        private string GetShapeType(UIElement shape)
        {
            return shape switch
            {
                Ellipse => "Circle",
                Rectangle => "Rectangle",
                Polygon => "Triangle",
                Line => "Line",
                TextBlock => "Text",
                Image => "Image",
                _ => "Shape"
            };
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Handle keyboard shortcuts
            if (e.Key == System.Windows.Input.Key.Delete && selectedObject != null)
            {
                btnDelete_Click(sender, e);
            }
            else if (e.Key == System.Windows.Input.Key.C && 
                     (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                btnCopy_Click(sender, e);
            }
            else if (e.Key == System.Windows.Input.Key.X && 
                     (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                btnCut_Click(sender, e);
            }
            else if (e.Key == System.Windows.Input.Key.V && 
                     (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                btnPaste_Click(sender, e);
            }
            else if (e.Key == System.Windows.Input.Key.Z && 
                     (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                btnUndo_Click(sender, e);
            }
            else if (e.Key == System.Windows.Input.Key.Y && 
                     (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                btnRedo_Click(sender, e);
            }
            else if (e.Key == System.Windows.Input.Key.D && currentMode == AppMode.Apply)
            {
                // D key toggles delete tool in edit mode
                btnDeleteTool_Click(sender, e);
            }
        }

        private void MenuExportAllLinks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentProject == null || currentPage == null)
                {
                    MessageBox.Show("No project or page loaded.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get all objects with links
                var objectsWithLinks = currentPage.Objects.Where(obj => obj.LinkType != LinkType.None).ToList();

                if (!objectsWithLinks.Any())
                {
                    MessageBox.Show("No objects with links found on the current page.", "No Links", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Create bulk link configuration
                var bulkConfig = new BulkLinkConfiguration
                {
                    ProjectName = currentProject.ProjectName,
                    PageName = currentPage.PageName,
                    ExportedAt = DateTime.Now,
                    Version = "1.0",
                    LinkConfigurations = new List<LinkConfiguration>()
                };

                // Add each object's link configuration
                foreach (var obj in objectsWithLinks)
                {
                    var linkConfig = new LinkConfiguration
                    {
                        LinkType = obj.LinkType,
                        LinkFileType = obj.LinkFileType,
                        LinkPageId = obj.LinkPageId,
                        LinkDocumentPath = obj.LinkDocumentPath,
                        LinkUrl = obj.LinkUrl,
                        LinkText = obj.Text,
                        ExcelRange = obj.ExcelRange,
                        Metadata = new LinkConfigurationMetadata
                        {
                            ExportedAt = DateTime.Now,
                            Version = "1.0",
                            Description = $"Link configuration for {obj.ObjectName}",
                            ObjectName = obj.ObjectName,
                            ObjectType = obj.ObjectType
                        }
                    };
                    bulkConfig.LinkConfigurations.Add(linkConfig);
                }

                // Serialize to JSON
                var json = JsonSerializer.Serialize(bulkConfig, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                // Show save dialog
                var dialog = new SaveFileDialog
                {
                    Title = "Export All Links",
                    Filter = "Bulk Link Configuration (*.bulk)|*.bulk|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = ".bulk",
                    FileName = $"BulkLinks_{currentProject.ProjectName}_{currentPage.PageName}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, json);
                    MessageBox.Show($"Exported {bulkConfig.LinkConfigurations.Count} link configurations!\n\nFile: {dialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting links: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuImportLinks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentProject == null || currentPage == null)
                {
                    MessageBox.Show("No project or page loaded.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Show open dialog
                var dialog = new OpenFileDialog
                {
                    Title = "Import Links",
                    Filter = "Bulk Link Configuration (*.bulk)|*.bulk|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = ".bulk"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Read and deserialize JSON
                    var json = File.ReadAllText(dialog.FileName);
                    var bulkConfig = JsonSerializer.Deserialize<BulkLinkConfiguration>(json);

                    if (bulkConfig == null || bulkConfig.LinkConfigurations == null)
                    {
                        MessageBox.Show("Invalid bulk link configuration file.", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Apply imported configurations
                    int appliedCount = 0;
                    foreach (var linkConfig in bulkConfig.LinkConfigurations)
                    {
                        // Find object by name and type
                        var targetObject = currentPage.Objects.FirstOrDefault(obj => 
                            obj.ObjectName == linkConfig.Metadata.ObjectName && 
                            obj.ObjectType == linkConfig.Metadata.ObjectType);

                        if (targetObject != null)
                        {
                            // Apply the link configuration
                            targetObject.LinkType = linkConfig.LinkType;
                            targetObject.LinkFileType = linkConfig.LinkFileType;
                            targetObject.LinkPageId = linkConfig.LinkPageId;
                            targetObject.LinkDocumentPath = linkConfig.LinkDocumentPath;
                            targetObject.LinkUrl = linkConfig.LinkUrl;
                            targetObject.Text = linkConfig.LinkText;
                            targetObject.ExcelRange = linkConfig.ExcelRange;
                            appliedCount++;
                        }
                    }

                    // Reload the page to reflect changes
                    LoadPage(currentPage);

                    MessageBox.Show($"Imported {appliedCount} link configurations successfully!", 
                        "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing links: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
