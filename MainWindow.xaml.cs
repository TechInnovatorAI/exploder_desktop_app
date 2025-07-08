using Exploder.Setting;
using Microsoft.Win32;   // For OpenFileDialog
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Exploder
{
    public partial class MainWindow : Window
    {
        private AppMode currentMode = AppMode.View;

        private bool circleToolActive = false;
        private bool squareToolActive = false;
        private bool lineToolActive = false;
        private bool roundedRectToolActive = false;
        private bool triangleToolActive = false;
        private bool textToolActive = false;




        private bool isDrawing = false;
        private Point startPoint;

        private Button? currentButton;
        private Ellipse? currentEllipse = null;
        private Rectangle? currentRectangle = null;
        private Rectangle? currentRoundedRectangle = null;
        private Line? currentLine = null;
        private Image? currentImage = null;
        private Polygon? currentTriangle = null;

        private Stack<UIElement> undoStack = new Stack<UIElement>();
        private Stack<UIElement> redoStack = new Stack<UIElement>();

        public MainWindow()
        {
            InitializeComponent();
            SetMode(AppMode.View);
            this.Loaded += (s, e) => MainContentGrid.Focus();
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

        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            drawingCanvas.Children.Clear();
            undoStack.Clear();
            redoStack.Clear();
            currentImage = null;
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Open Project File",
                Filter = "Exploder Project (*.exp)|*.exp",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                LoadProjectFromFile(dlg.FileName);
            }
        }
        private void LoadProjectFromFile(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                ProjectData project = System.Text.Json.JsonSerializer.Deserialize<ProjectData>(json);

                if (project == null)
                {
                    MessageBox.Show("Failed to load project: Empty or invalid file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                drawingCanvas.Children.Clear();
                undoStack.Clear();
                redoStack.Clear();

                foreach (var item in project.Shapes)
                {
                    UIElement? shape = item.Type switch
                    {
                        "Circle" => new Ellipse
                        {
                            Width = item.Width,
                            Height = item.Height,
                            Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(item.StrokeColor) ?? Brushes.Blue),
                            Fill = (SolidColorBrush)(new BrushConverter().ConvertFromString(item.FillColor) ?? Brushes.LightBlue),
                            StrokeThickness = 2
                        },
                        "Square" => new Rectangle
                        {
                            Width = item.Width,
                            Height = item.Height,
                            Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(item.StrokeColor) ?? Brushes.Green),
                            Fill = (SolidColorBrush)(new BrushConverter().ConvertFromString(item.FillColor) ?? Brushes.LightGreen),
                            StrokeThickness = 2
                        },
                        "Line" => new Line
                        {
                            X1 = item.X1,
                            Y1 = item.Y1,
                            X2 = item.X2,
                            Y2 = item.Y2,
                            Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(item.StrokeColor) ?? Brushes.Red),
                            StrokeThickness = 2,
                            Opacity = 0.7
                        },
                        "Image" =>
                            !string.IsNullOrEmpty(item.ImagePath) ? new Image
                            {
                                Width = item.Width,
                                Height = item.Height,
                                Source = new BitmapImage(new Uri(item.ImagePath)),
                                Stretch = Stretch.Uniform
                            } : null,
                        _ => null
                    };

                    if (shape != null)
                    {
                        Canvas.SetLeft(shape, item.Left);
                        Canvas.SetTop(shape, item.Top);
                        drawingCanvas.Children.Add(shape);
                        undoStack.Push(shape);
                    }
                }

                MessageBox.Show("Project loaded successfully.", "Open", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load project:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            btnSaveMode_Click(sender, e);
            SaveFileDialog dlg = new()
            {
                Title = "Save Project As",
                Filter = "Exploder Project (*.exp)|*.exp",
                DefaultExt = ".exp"
            };

            if (dlg.ShowDialog() == true)
            {
                SaveProjectToFile(dlg.FileName);
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public class PointData
        {
            public double X { get; set; }
            public double Y { get; set; }
        }
        private void SaveProjectToFile(string path)
        {
            try
    {
            ProjectData project = new();

            foreach (UIElement element in drawingCanvas.Children)
            {
                if (element is Ellipse ellipse)
                {
                    project.Shapes.Add(new ShapeData
                    {
                        Type = "Circle",
                        Left = Canvas.GetLeft(ellipse),
                        Top = Canvas.GetTop(ellipse),
                        Width = ellipse.Width,
                        Height = ellipse.Height,
                        FillColor = ((SolidColorBrush)ellipse.Fill).Color.ToString(),
                        StrokeColor = ((SolidColorBrush)ellipse.Stroke).Color.ToString()
                    });
                }
                else if (element is Rectangle rect)
                {
                    project.Shapes.Add(new ShapeData
                    {
                        Type = "Square",
                        Left = Canvas.GetLeft(rect),
                        Top = Canvas.GetTop(rect),
                        Width = rect.Width,
                        Height = rect.Height,
                        FillColor = ((SolidColorBrush)rect.Fill).Color.ToString(),
                        StrokeColor = ((SolidColorBrush)rect.Stroke).Color.ToString()
                    });
                }
                else if (element is Line line)
                {
                    project.Shapes.Add(new ShapeData
                    {
                        Type = "Line",
                        X1 = line.X1,
                        Y1 = line.Y1,
                        X2 = line.X2,
                        Y2 = line.Y2,
                        StrokeColor = ((SolidColorBrush)line.Stroke).Color.ToString()
                    });
                }
                else if (element is Image img && img.Source is BitmapImage bmp)
                {
                    project.Shapes.Add(new ShapeData
                    {
                        Type = "Image",
                        Left = Canvas.GetLeft(img),
                        Top = Canvas.GetTop(img),
                        Width = img.Width,
                        Height = img.Height,
                        ImagePath = bmp.UriSource?.LocalPath ?? ""
                    });
                }
                else if (element is Polygon polygon)
                {
                    if (polygon.Points.Count == 3) // Triangle
                    {
                        project.Shapes.Add(new ShapeData
                        {
                            Type = "Triangle",
                            //Points = polygon.Points.Select(p => new PointData { X = p.X, Y = p.Y }).ToList(),
                            FillColor = ((SolidColorBrush)polygon.Fill).Color.ToString(),
                            StrokeColor = ((SolidColorBrush)polygon.Stroke).Color.ToString()
                        });
                    }
                    else
                    {
                        // Handle other polygons if needed
                    }
                }
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            string jsonString = JsonSerializer.Serialize(project, options);

            System.IO.File.WriteAllText(path, jsonString);

            MessageBox.Show("Project saved successfully!", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving project: " + ex.Message);
            }
        }



        private void SetMode(AppMode mode)
        {
            currentMode = mode;

            switch (mode)
            {
                case AppMode.New:
                    Title = "Exploder - New Mode";
                    break;
                case AppMode.Open:
                    Title = "Exploder - Open Mode";
                    break;
                case AppMode.View:
                    btnCircleTool.IsEnabled = false;
                    btnSquareTool.IsEnabled = false;
                    btnLineTool.IsEnabled = false;
                    btnImageTool.IsEnabled = false;
                    btnRoundedRectTool.IsEnabled = false;
                    btntriangleTool.IsEnabled = false;
                    btnTextTool.IsEnabled = false;

                    circleToolActive = false;
                    squareToolActive = false;
                    lineToolActive = false;
                    roundedRectToolActive = false;
                    triangleToolActive = false;
                    textToolActive = false;

                    Title = "Exploder - View Mode";
                    break;

                case AppMode.Insert:
                    btnCircleTool.IsEnabled = true;
                    btnSquareTool.IsEnabled = true;
                    btnLineTool.IsEnabled = true;
                    btnImageTool.IsEnabled = true;
                    btnRoundedRectTool.IsEnabled = true;
                    btntriangleTool.IsEnabled = true;
                    btnTextTool.IsEnabled = true;

                    Title = "Exploder - Insert Mode";
                    break;
                case AppMode.Save:
                    btnCircleTool.IsEnabled = false;
                    btnSquareTool.IsEnabled = false;
                    btnLineTool.IsEnabled = false;
                    btnImageTool.IsEnabled = false;
                    btnRoundedRectTool.IsEnabled = false;
                    btntriangleTool.IsEnabled = false;
                    btnTextTool.IsEnabled = true;


                    circleToolActive = false;
                    squareToolActive = false;
                    lineToolActive = false;
                    roundedRectToolActive = false;
                    triangleToolActive = false;
                    textToolActive = true;

                    Title = "Exploder - Save Mode";
                    break;
                case AppMode.Apply:
                    btnCircleTool.IsEnabled = false;
                    btnSquareTool.IsEnabled = false;
                    btnLineTool.IsEnabled = false;
                    btnImageTool.IsEnabled = false;
                    btnRoundedRectTool.IsEnabled = false;
                    btntriangleTool.IsEnabled = false;
                    btnTextTool.IsEnabled = true;

                    circleToolActive = false;
                    squareToolActive = false;
                    lineToolActive = false;
                    roundedRectToolActive = false;
                    triangleToolActive = false;
                    textToolActive = true;

                    Title = "Exploder - Apply Mode";
                    SavePage();
                    break;
            }
        }

        private void SavePage()
        {
            var shapeData = SerializeShapes();

            File.WriteAllLines("page.exploder", shapeData); // Save shapes line by line

            MessageBox.Show("Page saved!", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            SetMode(AppMode.View);
        }
        private List<string> SerializeShapes()
        {
            var shapes = new List<string>();

            foreach (var child in drawingCanvas.Children)
            {
                if (child is Shape shape)
                {
                    var type = shape.GetType().Name;
                    var left = Canvas.GetLeft(shape);
                    var top = Canvas.GetTop(shape);
                    var width = shape.Width;
                    var height = shape.Height;

                    shapes.Add($"{type},{left},{top},{width},{height}");
                }
            }

            return shapes;
        }
        // Mode buttons
        private void btnViewMode_Click(object sender, RoutedEventArgs e) => SetMode(AppMode.View);
        private void btnInsertMode_Click(object sender, RoutedEventArgs e) => SetMode(AppMode.Insert);
        private void btnSaveMode_Click(object sender, RoutedEventArgs e) => SetMode(AppMode.Save);
        private void btnSavePageMode_Click(object sender, RoutedEventArgs e) => SetMode(AppMode.Apply);

        // Tool buttons
        private void btnCircleTool_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == AppMode.Insert)
            {
                circleToolActive = true;
                squareToolActive = false;
                lineToolActive = false;
                triangleToolActive = false;

            }
        }

        private void btnSquareTool_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == AppMode.Insert)
            {
                squareToolActive = true;
                circleToolActive = false;
                lineToolActive = false;
                triangleToolActive = false;

            }
        }

        private void btnLineTool_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == AppMode.Insert)
            {
                lineToolActive = true;
                circleToolActive = false;
                squareToolActive = false;
                triangleToolActive = false;
            }
        }

        private void btnImageTool_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == AppMode.Insert)
            {
                LoadImageFromFile();
            }
        }

        private void btnRoundedRectTool_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == AppMode.Insert)
            {
                
                roundedRectToolActive = true;
                circleToolActive = false;
                squareToolActive = false;
                lineToolActive = false;
                triangleToolActive = false;

            }
        }

        private void btnTriangleToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == AppMode.Insert)
            {
                triangleToolActive = true;
                circleToolActive = false;
                squareToolActive = false;
                lineToolActive = false;
                roundedRectToolActive = false;
            }          
        }
        private void btnTextTool_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == AppMode.Insert)
            {
                textToolActive = true;
                circleToolActive = squareToolActive = lineToolActive = roundedRectToolActive = false;
            }
        }

        // You can also add reposition/resize logic for other shapes here if needed
        private void LoadImageFromFile()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Select an Image",
                Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif;*.tiff",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(dlg.FileName));

                    currentImage = new Image
                    {
                        Source = bitmap,
                        Width = 800,
                        Height = 500,
                        Stretch = Stretch.Uniform
                    };

                    // Clear any current drawing selection
                    isDrawing = false;
                    currentEllipse = null;
                    currentRectangle = null;
                    currentLine = null;

                    // Position image in center of canvas
                    double left = (drawingCanvas.ActualWidth - currentImage.Width) / 2;
                    double top = (drawingCanvas.ActualHeight - currentImage.Height) / 2;

                    Canvas.SetLeft(currentImage, left);
                    Canvas.SetTop(currentImage, top);

                    drawingCanvas.Children.Add(currentImage);
                    undoStack.Push(currentImage);
                    redoStack.Clear();

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading image: " + ex.Message);
                }
            }
        }
        private void EllipseButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("You clicked the circle!");
        }
        // Drawing logic
        private void drawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currentMode != AppMode.Insert || (!circleToolActive && !squareToolActive && !lineToolActive && !triangleToolActive && !roundedRectToolActive && !textToolActive))
                return;

            isDrawing = true;
            startPoint = e.GetPosition(drawingCanvas);

            if (circleToolActive)
            {
                currentEllipse = new Ellipse
                {
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2,
                    Fill = Brushes.LightBlue,
                    Width = 0,
                    Height = 0,
                };

                var ellipseButton = new Button
                {
                    Content = currentEllipse,
                    Background = Brushes.Transparent,
                    BorderBrush = null,
                    Padding = new Thickness(0),
                    Width = 0,
                    Height = 0,
                };

                ellipseButton.Click += EllipseButton_Click;

                Canvas.SetLeft(ellipseButton, startPoint.X);
                Canvas.SetTop(ellipseButton, startPoint.Y);
                drawingCanvas.Children.Add(ellipseButton);
                currentButton = ellipseButton; // Track current shape as a button
            }

            else if (squareToolActive)
            {
                currentRectangle = new Rectangle
                {
                    Stroke = Brushes.Green,
                    StrokeThickness = 2,
                    Fill = Brushes.LightGreen,
                    Width = 0,
                    Height = 0,
                };

                currentButton = new Button
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.Transparent,
                    Opacity = 0.7,
                    Width = 0,
                    Height = 0
                };

                currentButton.Click += (s, args) =>
                {
                    MessageBox.Show("Square button clicked!");
                };

                Canvas.SetLeft(currentRectangle, startPoint.X);
                Canvas.SetTop(currentRectangle, startPoint.Y);
                Canvas.SetLeft(currentButton, startPoint.X);
                Canvas.SetTop(currentButton, startPoint.Y);


                drawingCanvas.Children.Add(currentRectangle);
                drawingCanvas.Children.Add(currentButton);
            }
            else if (lineToolActive)
            {
                currentLine = new Line
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    X1 = startPoint.X,
                    Y1 = startPoint.Y,
                    X2 = startPoint.X,
                    Y2 = startPoint.Y,
                    Opacity = 0.7
                };
                drawingCanvas.Children.Add(currentLine);
            }
            else if (roundedRectToolActive)
            {
                currentRoundedRectangle = new Rectangle
                {
                    RadiusX = 25,
                    RadiusY = 25,
                    Stroke = Brushes.Orange,
                    Fill = Brushes.LightSalmon,
                    StrokeThickness = 2,
                    Width = 0,
                    Height = 0,
                };

                currentButton = new Button
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Opacity = 0.7,
                    Width = 0,
                    Height = 0
                };

                currentButton.Click += (s, args) =>
                {
                    MessageBox.Show("Rounded Rectangle button clicked!");
                };

                Canvas.SetLeft(currentRoundedRectangle, startPoint.X);
                Canvas.SetTop(currentRoundedRectangle, startPoint.Y);

                Canvas.SetLeft(currentButton, startPoint.X);
                Canvas.SetTop(currentButton, startPoint.Y);

                drawingCanvas.Children.Add(currentRoundedRectangle);
                drawingCanvas.Children.Add(currentButton);
            }
            else if (triangleToolActive)
            {
                currentTriangle = new Polygon
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent
                };

                currentButton = new Button
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Opacity = 0.7,
                    Width = 0,
                    Height = 0
                };

                currentButton.Click += (s, args) =>
                {
                    MessageBox.Show("Triangle button clicked!");
                };

                Canvas.SetLeft(currentButton, startPoint.X);
                Canvas.SetTop(currentButton, startPoint.Y);

                drawingCanvas.Children.Add(currentTriangle);
                drawingCanvas.Children.Add(currentButton);
            }
            else if (textToolActive)
            {
                var textBox = new TextBox
                {
                    Text = "",
                    FontSize = 16,
                    Width = 50,
                    Height = 30,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1)
                };

                textBox.KeyDown += TextBox_KeyDown; // ✅ Add this line

                Canvas.SetLeft(textBox, startPoint.X);
                Canvas.SetTop(textBox, startPoint.Y);
                drawingCanvas.Children.Add(textBox);
                textBox.Focus();

                undoStack.Push(textBox);
                redoStack.Clear();
                isDrawing = false;
            }
            drawingCanvas.CaptureMouse();
        }

        private void drawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            if (!isDrawing) return;
           
            Point currentPoint = e.GetPosition(drawingCanvas);
            Point p1 = new Point((startPoint.X + currentPoint.X) / 2, startPoint.Y); // Top
            Point p2 = new Point(startPoint.X, currentPoint.Y); // Bottom left
            Point p3 = new Point(currentPoint.X, currentPoint.Y); // Bottom right
            double size = Math.Max(Math.Abs(currentPoint.X - startPoint.X), Math.Abs(currentPoint.Y - startPoint.Y));
            double width = Math.Abs(currentPoint.X - startPoint.X);
            double height = Math.Abs(currentPoint.Y - startPoint.Y);
            double left = Math.Min(currentPoint.X, startPoint.X);
            double top = Math.Min(currentPoint.Y, startPoint.Y);

            double buttonWidth = width * 1.02;
            double buttonHeight = height * 1.02;

            double offsetX = (buttonWidth - width) / 2;
            double offsetY = (buttonHeight - height) / 2;

            if (circleToolActive && currentEllipse != null)
            {
                currentEllipse.Width = width;
                currentEllipse.Height = height;

                currentButton.Width = buttonWidth;
                currentButton.Height = buttonHeight;

                Canvas.SetLeft(currentEllipse, left);
                Canvas.SetTop(currentEllipse, top);

                Canvas.SetLeft(currentButton, left - offsetX);
                Canvas.SetTop(currentButton, top - offsetY);
            }
            else if (squareToolActive && currentRectangle != null && currentButton != null)
            {
                currentRectangle.Width = width;
                currentRectangle.Height = height;

                currentButton.Width = buttonWidth;
                currentButton.Height = buttonHeight;

                Canvas.SetLeft(currentRectangle, left);
                Canvas.SetTop(currentRectangle, top);

                Canvas.SetLeft(currentButton, left - offsetX);
                Canvas.SetTop(currentButton, top - offsetY);
            }
            else if (lineToolActive && currentLine != null)
            {
                currentLine.X2 = currentPoint.X;
                currentLine.Y2 = currentPoint.Y;
            }
            else if (roundedRectToolActive && currentRoundedRectangle != null && currentButton != null)
            {
                currentRoundedRectangle.Width = width;
                currentRoundedRectangle.Height = height;

                currentButton.Width = buttonWidth;
                currentButton.Height = buttonHeight;

                Canvas.SetLeft(currentRoundedRectangle, left);
                Canvas.SetTop(currentRoundedRectangle, top);

                Canvas.SetLeft(currentButton, left - offsetX);
                Canvas.SetTop(currentButton, top - offsetY);
            }
            else if (triangleToolActive && currentTriangle != null && currentButton != null)
            {
                //Point p1 = new Point((startPoint.X + currentPoint.X) / 2, startPoint.Y); // Top
                //Point p2 = new Point(startPoint.X, currentPoint.Y); // Bottom left
                //Point p3 = new Point(currentPoint.X, currentPoint.Y); // Bottom right

                //currentTriangle.Points = new PointCollection { p1, p2, p3 };

                //currentButton.Width = buttonWidth;
                //currentButton.Height = buttonHeight;

                //Canvas.SetLeft(currentButton, left - offsetX);
                //Canvas.SetTop(currentButton, top - offsetY);
            }
        }

        private void drawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDrawing)
                return;

            isDrawing = false;
            drawingCanvas.ReleaseMouseCapture();
            if (currentButton != null)
            {
                undoStack.Push(currentButton);
                redoStack.Clear();
                currentButton = null;
            }
            if (currentEllipse != null)
            {
                undoStack.Push(currentEllipse);
                undoStack.Push(currentButton);
                redoStack.Clear();

                currentEllipse = null;
                currentButton = null;
            }

            if (currentRectangle != null)
            {
                undoStack.Push(currentRectangle);
                undoStack.Push(currentButton);
                redoStack.Clear();

                currentRectangle = null;
                currentButton = null;
            }

            if (currentLine != null)
            {
                undoStack.Push(currentLine);
                redoStack.Clear();
                currentLine = null;
            }
            if (currentRoundedRectangle != null)
            {
                undoStack.Push(currentRoundedRectangle);
                undoStack.Push(currentButton);
                redoStack.Clear();

                currentRoundedRectangle = null;
                currentButton = null;
            }

            if (currentTriangle != null)
            {
                undoStack.Push(currentTriangle);
                undoStack.Push(currentButton);
                redoStack.Clear();

                currentTriangle = null;
                currentButton = null;
            }
        }

        // Undo / Redo buttons
        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count > 0)
            {
                UIElement element = undoStack.Pop();
                drawingCanvas.Children.Remove(element);
                redoStack.Push(element);
            }
        }

        private void btnRedo_Click(object sender, RoutedEventArgs e)
        {
            if (redoStack.Count > 0)
            {
                UIElement element = redoStack.Pop();
                drawingCanvas.Children.Add(element);
                undoStack.Push(element);
            }
        }
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tb = sender as TextBox;

                var textBlock = new TextBlock
                {
                    Text = tb.Text,
                    FontSize = tb.FontSize,
                    Width = tb.Width,
                    Height = tb.Height,
                    Background = Brushes.Transparent
                };

                double left = Canvas.GetLeft(tb);
                double top = Canvas.GetTop(tb);

                Canvas.SetLeft(textBlock, left);
                Canvas.SetTop(textBlock, top);

                drawingCanvas.Children.Remove(tb);
                drawingCanvas.Children.Add(textBlock);

                undoStack.Push(textBlock);
                redoStack.Clear();

                e.Handled = true;

                SetMode(currentMode = AppMode.Insert);
                squareToolActive = true;
            }
        }
        // Keyboard shortcuts Ctrl+Z and Ctrl+Y
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.Z:
                        btnUndo_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.Y:
                        btnRedo_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.N:
                        MenuNew_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.O:
                        MenuOpen_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.S:
                        MenuSave_Click(null, null);
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}
