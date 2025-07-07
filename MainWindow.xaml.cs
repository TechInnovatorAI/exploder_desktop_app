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
        



        private bool isDrawing = false;
        private Point startPoint;

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


                    circleToolActive = false;
                    squareToolActive = false;
                    lineToolActive = false;
                    roundedRectToolActive = false;
                    triangleToolActive = false;

                    Title = "Exploder - View Mode";
                    break;

                case AppMode.Insert:
                    btnCircleTool.IsEnabled = true;
                    btnSquareTool.IsEnabled = true;
                    btnLineTool.IsEnabled = true;
                    btnImageTool.IsEnabled = true;
                    btnRoundedRectTool.IsEnabled = true;
                    btntriangleTool.IsEnabled = true;


                    Title = "Exploder - Insert Mode";
                    break;
                case AppMode.Save:
                    btnCircleTool.IsEnabled = false;
                    btnSquareTool.IsEnabled = false;
                    btnLineTool.IsEnabled = false;
                    btnImageTool.IsEnabled = false;
                    btnRoundedRectTool.IsEnabled = false;
                    btntriangleTool.IsEnabled = false;

                    circleToolActive = false;
                    squareToolActive = false;
                    lineToolActive = false;
                    roundedRectToolActive = false;
                    triangleToolActive = false;

                    Title = "Exploder - Save Mode";
                    break;
                case AppMode.Apply:
                    btnCircleTool.IsEnabled = false;
                    btnSquareTool.IsEnabled = false;
                    btnLineTool.IsEnabled = false;
                    btnImageTool.IsEnabled = false;
                    btnRoundedRectTool.IsEnabled = false;
                    btntriangleTool.IsEnabled = false;

                    circleToolActive = false;
                    squareToolActive = false;
                    lineToolActive = false;
                    roundedRectToolActive = false;
                    triangleToolActive = false;

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

        // Drawing logic
        private void drawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currentMode != AppMode.Insert || (!circleToolActive && !squareToolActive && !lineToolActive && !triangleToolActive))
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

                Canvas.SetLeft(currentEllipse, startPoint.X);
                Canvas.SetTop(currentEllipse, startPoint.Y);
                drawingCanvas.Children.Add(currentEllipse);
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

                Canvas.SetLeft(currentRectangle, startPoint.X);
                Canvas.SetTop(currentRectangle, startPoint.Y);
                drawingCanvas.Children.Add(currentRectangle);
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
                    Opacity = 0.3
                };
                Canvas.SetLeft(currentRoundedRectangle, startPoint.X);
                Canvas.SetTop(currentRoundedRectangle, startPoint.Y);
                drawingCanvas.Children.Add(currentRoundedRectangle);
            }
            else if (triangleToolActive)
            {
                currentTriangle = new Polygon
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent
                };

                drawingCanvas.Children.Add(currentTriangle);
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

            if (circleToolActive && currentEllipse != null)
            {
                currentEllipse.Width = width;
                currentEllipse.Height = height;
                Canvas.SetLeft(currentEllipse,left);
                Canvas.SetTop(currentEllipse, top);
            }
            else if (squareToolActive && currentRectangle != null)
            {
                currentRectangle.Width = width;
                currentRectangle.Height = height;
                Canvas.SetLeft(currentRectangle,left);
                Canvas.SetTop(currentRectangle, top);
            }
            else if (lineToolActive && currentLine != null)
            {
                currentLine.X2 = currentPoint.X;
                currentLine.Y2 = currentPoint.Y;
            }
            else if (roundedRectToolActive && currentRoundedRectangle != null)
            {
                currentRoundedRectangle.Width = width;
                currentRoundedRectangle.Height = height;
                Canvas.SetLeft(currentRoundedRectangle, left);
                Canvas.SetTop(currentRoundedRectangle, top);
            }

            else if (triangleToolActive && currentTriangle != null)
            {
                
                // Calculate three points of a triangle
               

                PointCollection points = new PointCollection { p1, p2, p3 };
                currentTriangle.Points = points;
            }
        }

        private void drawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDrawing)
                return;

            isDrawing = false;
            drawingCanvas.ReleaseMouseCapture();

            if (currentEllipse != null)
            {
                undoStack.Push(currentEllipse);
                redoStack.Clear();
                currentEllipse = null;
            }

            if (currentRectangle != null)
            {
                undoStack.Push(currentRectangle);
                redoStack.Clear();
                currentRectangle = null;
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
                redoStack.Clear();
                currentRoundedRectangle = null;
            }
            if (triangleToolActive && currentTriangle != null)
            {
                undoStack.Push(currentTriangle);
                redoStack.Clear();
                currentTriangle = null;
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
