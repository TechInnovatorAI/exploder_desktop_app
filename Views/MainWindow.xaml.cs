using Exploder.Infrastructure.Setting;
using Exploder.Models;
using Exploder.Services;
using Microsoft.Win32;
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
        
        // Undo/Redo
        private Stack<PageData> undoStack = new Stack<PageData>();
        private Stack<PageData> redoStack = new Stack<PageData>();

        private Dictionary<UIElement, Canvas> linkedPages = new Dictionary<UIElement, Canvas>();
        private Canvas? mainCanvasBackup;
        private Canvas? mainPageCanvas;

        private List<Button> viewModeButtons = new List<Button>();

        public MainWindow()
        {
            InitializeComponent();
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
        }

        private void CreateUIElementFromObject(ExploderObject obj)
        {
            UIElement? element = obj.ObjectType switch
            {
                "Circle" => CreateCircle(obj),
                "Rectangle" => CreateRectangle(obj),
                "RoundedRectangle" => CreateRoundedRectangle(obj),
                "Triangle" => CreateTriangle(obj),
                "Line" => CreateLine(obj),
                "Text" => CreateText(obj),
                "Image" => CreateImage(obj),
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
            }
        }

        private UIElement CreateCircle(ExploderObject obj)
        {
            var ellipse = new Ellipse
            {
                Width = obj.Width,
                Height = obj.Height,
                Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.FillColor) ?? System.Windows.Media.Brushes.LightBlue,
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.StrokeColor) ?? System.Windows.Media.Brushes.Blue,
                StrokeThickness = obj.StrokeThickness,
                Opacity = obj.Opacity
            };

            ellipse.MouseLeftButtonDown += Object_MouseLeftButtonDown;
            ellipse.MouseRightButtonDown += Object_MouseRightButtonDown;
            
            return ellipse;
        }

        private UIElement CreateRectangle(ExploderObject obj)
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = obj.Width,
                Height = obj.Height,
                Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.FillColor) ?? System.Windows.Media.Brushes.LightGreen,
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.StrokeColor) ?? System.Windows.Media.Brushes.Green,
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
                Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.FillColor) ?? System.Windows.Media.Brushes.LightYellow,
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.StrokeColor) ?? System.Windows.Media.Brushes.Orange,
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
                Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.FillColor) ?? System.Windows.Media.Brushes.LightPink,
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.StrokeColor) ?? System.Windows.Media.Brushes.Red,
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
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(obj.StrokeColor) ?? System.Windows.Media.Brushes.Black,
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
            
            return image;
        }

        private void Object_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currentMode == AppMode.View)
            {
                HandleObjectClick(sender as UIElement);
            }
            else if (currentMode == AppMode.Insert)
            {
                // In insert mode, clicking on objects should select them for editing
                SelectObject(sender as UIElement);
            }
            
            e.Handled = true;
        }

        private void Object_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is ExploderObject obj)
            {
                ShowObjectProperties(obj);
            }
            e.Handled = true;
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
                        OpenDocument(obj.LinkDocumentPath);
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

        private void OpenDocument(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show("Document not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                // Remove selection visual
            }

            selectedObject = element;
            
            if (selectedObject != null)
            {
                // Add selection visual
                UpdateStatus($"Selected: {((selectedObject as FrameworkElement)?.Tag as ExploderObject)?.ObjectName ?? "Unknown"}");
            }
        }

        private void ShowObjectProperties(ExploderObject obj)
        {
            var propertiesWindow = new ObjectPropertiesWindow(obj);
            propertiesWindow.Owner = this;
            
            if (propertiesWindow.ShowDialog() == true && propertiesWindow.ObjectData != null)
            {
                string? linkedPageId = null;
                // Handle page linking if link type is Page
                if (propertiesWindow.LinkTypeString == "Page" && currentProject != null)
                {
                    string pageName = propertiesWindow.LinkTarget.Trim();
                    if (string.IsNullOrEmpty(pageName))
                    {
                        pageName = $"Page {currentProject.Pages.Count + 1}";
                    }

                    // Check if a page with this name already exists
                    var existingPage = currentProject.Pages.FirstOrDefault(p => p.PageName.Equals(pageName, StringComparison.OrdinalIgnoreCase));
                    
                    if (existingPage != null)
                    {
                        // Use existing page
                        propertiesWindow.ObjectData.LinkType = LinkType.NewPage;
                        propertiesWindow.ObjectData.LinkPageId = existingPage.PageId;
                        propertiesWindow.ObjectData.LinkTarget = existingPage.PageName;
                        linkedPageId = existingPage.PageId;
                    }
                    else
                    {
                        // Create new page with the specified name
                        var newPageId = Guid.NewGuid().ToString();
                        var newPage = new PageData
                        {
                            PageId = newPageId,
                            PageName = pageName,
                            ParentPageId = currentPage?.PageId ?? "",
                            PageSettings = currentPage?.PageSettings.Clone() ?? new PageSettings()
                        };
                        currentProject.Pages.Add(newPage);
                        propertiesWindow.ObjectData.LinkType = LinkType.NewPage;
                        propertiesWindow.ObjectData.LinkPageId = newPageId;
                        propertiesWindow.ObjectData.LinkTarget = pageName;
                        linkedPageId = newPageId;
                    }
                }
                
                // Update the object
                var index = currentPage?.Objects.IndexOf(obj) ?? -1;
                if (index >= 0 && currentPage != null)
                {
                    currentPage.Objects[index] = propertiesWindow.ObjectData;
                    
                    // Recreate the UI element
                    var element = drawingCanvas.Children.OfType<FrameworkElement>()
                        .FirstOrDefault(e => e.Tag == obj);
                    
                    if (element != null)
                    {
                        drawingCanvas.Children.Remove(element);
                        CreateUIElementFromObject(propertiesWindow.ObjectData);
                    }
                }
                // Navigate to the linked page if set
                if (!string.IsNullOrEmpty(linkedPageId))
                {
                    NavigateToPage(linkedPageId);
                }
                
                UpdateStatus("Object properties updated");
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

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
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

        private void MenuPublish_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Publish functionality will be implemented in future versions.", 
                "Publish", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuObjects_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Objects menu functionality will be implemented in future versions.", 
                "Objects", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuDocuments_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Documents menu functionality will be implemented in future versions.", 
                "Documents", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Exploder v1.0\n\nA top-down documentation program for items with multiple layers of documentation.\n\n© 2025 Exploder Development Team", 
                "About Exploder", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Help documentation will be available in future versions.", 
                "Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Tool button handlers
        private void btnViewMode_Click(object sender, RoutedEventArgs e) => SetMode(AppMode.View);
        private void btnInsertMode_Click(object sender, RoutedEventArgs e) => SetMode(AppMode.Insert);
        private void btnEditMode_Click(object sender, RoutedEventArgs e) => SetMode(AppMode.Apply);

        private void btnCircleTool_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTools();
            circleToolActive = true;
            UpdateToolButtons();
            UpdateStatus("Circle tool active - click and drag to draw");
        }

        private void btnSquareTool_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTools();
            rectangleToolActive = true;
            UpdateToolButtons();
            UpdateStatus("Rectangle tool active - click and drag to draw");
        }

        private void btnRoundedRectTool_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTools();
            roundedRectToolActive = true;
            UpdateToolButtons();
            UpdateStatus("Rounded rectangle tool active - click and drag to draw");
        }

        private void btnTriangleTool_Click(object sender, RoutedEventArgs e)
        {
            SetMode(AppMode.Insert);
            triangleToolActive = true;
            UpdateStatus("Triangle tool active. Drag to draw a triangle.");
        }

        private void btnLineTool_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTools();
            lineToolActive = true;
            UpdateToolButtons();
            UpdateStatus("Line tool active - click and drag to draw");
        }

        private void btnTextTool_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTools();
            textToolActive = true;
            UpdateToolButtons();
            UpdateStatus("Text tool active - click to add text");
        }

        private void btnImageTool_Click(object sender, RoutedEventArgs e)
        {
            DeactivateAllTools();
            LoadImageFromFile();
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            // Implement undo functionality
            UpdateStatus("Undo not implemented yet");
        }

        private void btnRedo_Click(object sender, RoutedEventArgs e)
        {
            // Implement redo functionality
            UpdateStatus("Redo not implemented yet");
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedObject is FrameworkElement fe && fe.Tag is ExploderObject obj)
            {
                drawingCanvas.Children.Remove(selectedObject);
                currentPage?.Objects.Remove(obj);
                selectedObject = null;
                UpdateStatus("Object deleted");
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
            if (currentMode != AppMode.Insert) return;

            startPoint = e.GetPosition(drawingCanvas);
            isDrawing = true;

            if (textToolActive)
            {
                CreateTextAtPoint(startPoint);
            }
        }

        private void drawingCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!isDrawing || currentMode != AppMode.Insert) return;

            var currentPoint = e.GetPosition(drawingCanvas);
            UpdateMousePosition(currentPoint);

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
            if (!isDrawing || currentMode != AppMode.Insert) return;

            isDrawing = false;
            if (currentShape != null)
            {
                // Create a new ExploderObject for the shape
                var obj = new ExploderObject
                {
                    ObjectId = Guid.NewGuid().ToString(),
                    ObjectType = currentShape is Ellipse ? "Circle" : currentShape is Rectangle ? "Rectangle" : currentShape is Polygon ? "Triangle" : "Shape",
                    Left = Canvas.GetLeft(currentShape),
                    Top = Canvas.GetTop(currentShape),
                    Width = (currentShape as FrameworkElement)?.Width ?? 0,
                    Height = (currentShape as FrameworkElement)?.Height ?? 0,
                    FillColor = "#FFFFFF",
                    StrokeColor = "#000000",
                    StrokeThickness = 2,
                    Opacity = 1.0,
                    ZIndex = currentPage?.Objects.Count ?? 0
                };
                // Show the modal
                var modal = new ObjectPropertiesWindow(obj);
                if (modal.ShowDialog() == true && modal.ObjectData != null)
                {
                    string? linkedPageId = null;
                    // If the link type is Page, handle page creation/linking
                    if (modal.LinkTypeString == "Page" && currentProject != null)
                    {
                        string pageName = modal.LinkTarget.Trim();
                        if (string.IsNullOrEmpty(pageName))
                        {
                            pageName = $"Page {currentProject.Pages.Count + 1}";
                        }

                        // Check if a page with this name already exists
                        var existingPage = currentProject.Pages.FirstOrDefault(p => p.PageName.Equals(pageName, StringComparison.OrdinalIgnoreCase));
                        
                        if (existingPage != null)
                        {
                            // Use existing page
                            modal.ObjectData.LinkType = LinkType.NewPage;
                            modal.ObjectData.LinkPageId = existingPage.PageId;
                            modal.ObjectData.LinkTarget = existingPage.PageName;
                            linkedPageId = existingPage.PageId;
                        }
                        else
                        {
                            // Create new page with the specified name
                            var newPageId = Guid.NewGuid().ToString();
                            var newPage = new PageData
                            {
                                PageId = newPageId,
                                PageName = pageName,
                                ParentPageId = currentPage?.PageId ?? "",
                                PageSettings = currentPage?.PageSettings.Clone() ?? new PageSettings()
                            };
                            currentProject.Pages.Add(newPage);
                            modal.ObjectData.LinkType = LinkType.NewPage;
                            modal.ObjectData.LinkPageId = newPageId;
                            modal.ObjectData.LinkTarget = pageName;
                            linkedPageId = newPageId;
                        }
                    }
                    // Update shape appearance
                    if (currentShape is Shape s)
                    {
                        s.Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(modal.ObjectData.FillColor);
                    }
                    // Add to model
                    currentPage?.Objects.Add(modal.ObjectData);
                    // Navigate to the linked page if set
                    if (!string.IsNullOrEmpty(linkedPageId))
                    {
                        NavigateToPage(linkedPageId);
                    }
                }
                else
                {
                    // Remove the shape if cancelled
                    drawingCanvas.Children.Remove(currentShape);
                }
            }
            currentShape = null;
            DeactivateAllTools();
            UpdateToolButtons();
            UpdateStatus("Drawing completed");
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

            // Remove any existing view mode buttons
            foreach (var btn in viewModeButtons)
                drawingCanvas.Children.Remove(btn);
            viewModeButtons.Clear();

            switch (mode)
            {
                case AppMode.View:
                    UpdateStatus("View mode - click objects to navigate");
                    // For each shape, create a button overlay
                    foreach (UIElement child in drawingCanvas.Children.OfType<UIElement>().ToList())
                    {
                        if (child is Shape shape)
                        {
                            double left = Canvas.GetLeft(shape);
                            double top = Canvas.GetTop(shape);
                            double width = (shape as FrameworkElement)?.Width ?? 0;
                            double height = (shape as FrameworkElement)?.Height ?? 0;
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
                            var btn = new Button
                            {
                                Opacity = 0.3,
                                BorderBrush = Brushes.DarkBlue,
                                BorderThickness = new Thickness(2),
                                Background = Brushes.Transparent,
                                Width = width,
                                Height = height,
                                Tag = shape.Tag, // Pass the ExploderObject
                                Content = (shape.Tag as ExploderObject)?.ObjectName ?? ""
                            };
                            Canvas.SetLeft(btn, left);
                            Canvas.SetTop(btn, top);
                            btn.Click += (s, e) =>
                            {
                                HandleObjectClick(btn);
                            };
                            drawingCanvas.Children.Add(btn);
                            viewModeButtons.Add(btn);
                        }
                    }
                    break;
                case AppMode.Insert:
                    UpdateStatus("Insert mode - use tools to add objects");
                    break;
                case AppMode.Apply:
                    UpdateStatus("Edit mode - click objects to edit");
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
        }

        private void UpdateToolButtons()
        {
            btnCircleTool.IsEnabled = currentMode == AppMode.Insert;
            btnSquareTool.IsEnabled = currentMode == AppMode.Insert;
            btnRoundedRectTool.IsEnabled = currentMode == AppMode.Insert;
            btnTriangleTool.IsEnabled = currentMode == AppMode.Insert;
            btnLineTool.IsEnabled = currentMode == AppMode.Insert;
            btnTextTool.IsEnabled = currentMode == AppMode.Insert;
            btnImageTool.IsEnabled = currentMode == AppMode.Insert;
        }

        private void CreateShape(System.Windows.Point start, System.Windows.Point current)
        {
            if (circleToolActive)
            {
                var ellipse = new Ellipse
                {
                    Stroke = Brushes.Blue,
                    Fill = Brushes.LightBlue,
                    StrokeThickness = 2
                };
                currentShape = ellipse;
                drawingCanvas.Children.Add(ellipse);
            }
            else if (rectangleToolActive)
            {
                var rect = new Rectangle
                {
                    Stroke = Brushes.Green,
                    Fill = Brushes.LightGreen,
                    StrokeThickness = 2
                };
                currentShape = rect;
                drawingCanvas.Children.Add(rect);
            }
            else if (roundedRectToolActive)
            {
                var rect = new Rectangle
                {
                    Stroke = Brushes.Orange,
                    Fill = Brushes.LightYellow,
                    StrokeThickness = 2,
                    RadiusX = 10,
                    RadiusY = 10
                };
                currentShape = rect;
                drawingCanvas.Children.Add(rect);
            }
            else if (triangleToolActive)
            {
                var triangle = new Polygon
                {
                    Stroke = Brushes.Red,
                    Fill = Brushes.LightPink,
                    StrokeThickness = 2
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
                    Y2 = current.Y
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
                txtPageInfo.Text = $"Objects: {currentPage.Objects.Count}";
                UpdateObjectCount();
            }
        }

        private void UpdateStatus(string message)
        {
            txtStatus.Text = message;
            UpdateObjectCount();
        }

        private void UpdateMousePosition(System.Windows.Point position)
        {
            txtMousePosition.Text = $"{position.X:F0}, {position.Y:F0}";
        }

        private void UpdateObjectCount()
        {
            int count = currentPage?.Objects.Count ?? 0;
            txtObjectCount.Text = count.ToString();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete && selectedObject != null)
            {
                btnDelete_Click(sender, e);
            }
            else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                btnUndo_Click(sender, e);
            }
            else if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            {
                btnRedo_Click(sender, e);
            }
        }
    }
}
