using Exploder.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.Json;
using System.Linq; // Added for FirstOrDefault

namespace Exploder.Views
{
    public partial class ObjectPropertiesWindow : Window
    {
        private ExploderObject? currentObject;
        private UIElement? currentElement;
        private bool isUpdating = false;
        private ProjectData? currentProject;
        private PageData? currentPage;
        
        // Property to communicate back to MainWindow
        public PageData? NewlyCreatedPage { get; private set; }

        public ObjectPropertiesWindow()
        {
            try
            {
                InitializeComponent();
                InitializeControls();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing ObjectPropertiesWindow: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public void SetObject(ExploderObject obj, UIElement element, ProjectData project, PageData page)
        {
            try
            {
                if (obj == null)
                {
                    throw new ArgumentNullException(nameof(obj), "ExploderObject cannot be null");
                }
                
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element), "UIElement cannot be null");
                }
                
                if (project == null)
                {
                    throw new ArgumentNullException(nameof(project), "ProjectData cannot be null");
                }
                if (page == null)
                {
                    throw new ArgumentNullException(nameof(page), "PageData cannot be null");
                }
                
                currentObject = obj;
                currentElement = element;
                currentProject = project;
                currentPage = page;
                LoadObjectProperties();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting object in properties window: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void InitializeControls()
        {
            try
            {
                // Initialize color combos
                InitializeColorComboBox(cmbFillColor);
                InitializeColorComboBox(cmbStrokeColor);

                // Initialize font family combo
                cmbFontFamily.Items.Clear();
                cmbFontFamily.Items.Add("Arial");
                cmbFontFamily.Items.Add("Calibri");
                cmbFontFamily.Items.Add("Times New Roman");
                cmbFontFamily.Items.Add("Courier New");
                cmbFontFamily.Items.Add("Segoe UI");
                cmbFontFamily.Items.Add("Verdana");
                cmbFontFamily.SelectedIndex = 0;

                // Initialize font weight combo
                cmbFontWeight.Items.Clear();
                cmbFontWeight.Items.Add("Normal");
                cmbFontWeight.Items.Add("Bold");
                cmbFontWeight.Items.Add("Light");
                cmbFontWeight.SelectedIndex = 0;

                // Initialize stretch mode combo
                cmbStretchMode.Items.Clear();
                cmbStretchMode.Items.Add("Uniform");
                cmbStretchMode.Items.Add("UniformToFill");
                cmbStretchMode.Items.Add("Fill");
                cmbStretchMode.Items.Add("None");
                cmbStretchMode.SelectedIndex = 0;

                // Initialize link type combo
                cmbLinkType.Items.Clear();
                cmbLinkType.Items.Add("None");
                cmbLinkType.Items.Add("New Page");
                cmbLinkType.Items.Add("Document");
                cmbLinkType.Items.Add("URL");
                cmbLinkType.Items.Add("Excel Data");
                cmbLinkType.SelectedIndex = 0;

                // Initialize document type combo
                cmbDocumentType.Items.Clear();
                cmbDocumentType.Items.Add("Video");
                cmbDocumentType.Items.Add("PDF");
                cmbDocumentType.Items.Add("Excel");
                cmbDocumentType.Items.Add("Word");
                cmbDocumentType.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing controls: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void InitializeColorComboBox(ComboBox comboBox)
        {
            comboBox.Items.Clear();
            comboBox.Items.Add("Transparent");
            comboBox.Items.Add("Black");
            comboBox.Items.Add("White");
            comboBox.Items.Add("Red");
            comboBox.Items.Add("Green");
            comboBox.Items.Add("Blue");
            comboBox.Items.Add("Yellow");
            comboBox.Items.Add("Cyan");
            comboBox.Items.Add("Magenta");
            comboBox.Items.Add("Gray");
            comboBox.Items.Add("Light Gray");
            comboBox.Items.Add("Dark Gray");
            comboBox.Items.Add("Orange");
            comboBox.Items.Add("Purple");
            comboBox.Items.Add("Brown");
            comboBox.SelectedIndex = 0;
        }

        private void LoadObjectProperties()
        {
            if (currentObject == null || currentElement == null) 
            {
                MessageBox.Show("Current object or element is null.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            isUpdating = true;

            try
            {
                // Set object type
                txtObjectType.Text = currentObject.ObjectType ?? "Unknown";

                // Basic properties
                txtObjectName.Text = currentObject.ObjectName ?? "";
                txtX.Text = currentObject.Left.ToString();
                txtY.Text = currentObject.Top.ToString();
                txtWidth.Text = currentObject.Width.ToString();
                txtHeight.Text = currentObject.Height.ToString();

                // Appearance properties
                SetComboBoxSelection(cmbFillColor, currentObject.FillColor ?? "Transparent");
                SetComboBoxSelection(cmbStrokeColor, currentObject.StrokeColor ?? "Black");
                txtStrokeWidth.Text = currentObject.StrokeThickness.ToString();
                
                // Update opacity slider and text
                double opacity = Math.Max(0, Math.Min(1, currentObject.Opacity)); // Clamp between 0 and 1
                sliderOpacity.Value = opacity;
                txtOpacityValue.Text = $"{(int)(opacity * 100)}%";

                // Update color preview
                UpdateColorPreview();

                // Show/hide specific property panels based on object type
                ShowRelevantPropertyPanels();

                // Load specific properties
                LoadSpecificProperties();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading object properties: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void ShowRelevantPropertyPanels()
        {
            // Hide all specific panels first
            textPropertiesPanel.Visibility = Visibility.Collapsed;
            linkPropertiesPanel.Visibility = Visibility.Collapsed;
            imagePropertiesPanel.Visibility = Visibility.Collapsed;

            // Show relevant panel based on object type
            switch (currentObject?.ObjectType)
            {
                case "Text":
                    textPropertiesPanel.Visibility = Visibility.Visible;
                    linkPropertiesPanel.Visibility = Visibility.Visible;
                    break;
                case "Image":
                    imagePropertiesPanel.Visibility = Visibility.Visible;
                    linkPropertiesPanel.Visibility = Visibility.Visible;
                    break;
                default:
                    // Show link properties for all other object types (shapes, etc.)
                    linkPropertiesPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void LoadSpecificProperties()
        {
            if (currentObject == null) return;

            switch (currentObject.ObjectType)
            {
                case "Text":
                    txtTextContent.Text = currentObject.Text ?? "";
                    SetComboBoxSelection(cmbFontFamily, currentObject.FontFamily ?? "Arial");
                    txtFontSize.Text = currentObject.FontSize.ToString();
                    SetComboBoxSelection(cmbFontWeight, currentObject.FontWeight ?? "Normal");
                    break;

                case "Image":
                    txtImagePath.Text = currentObject.ImagePath ?? "";
                    SetComboBoxSelection(cmbStretchMode, currentObject.StretchMode ?? "Uniform");
                    break;
            }

            // Load link properties for all object types
            LoadLinkProperties();
        }

        private void LoadLinkProperties()
        {
            if (currentObject == null) return;

            isUpdating = true;

            try
            {
                // Set link type
                SetComboBoxSelection(cmbLinkType, GetLinkTypeDisplayName(currentObject.LinkType));

                // Set link target - always show page name since Link Target is for page creation
                if (!string.IsNullOrEmpty(currentObject.LinkPageId) && currentProject?.Pages != null)
                {
                    var targetPage = currentProject.Pages.FirstOrDefault(p => p.PageId == currentObject.LinkPageId);
                    txtLinkTarget.Text = targetPage?.PageName ?? "";
                }
                else
                {
                    txtLinkTarget.Text = "";
                }

                // Set specific link properties based on type (for additional fields)
                switch (currentObject.LinkType)
                {
                    case LinkType.Document:
                        txtDocumentPath.Text = currentObject.LinkDocumentPath ?? "";
                        SetComboBoxSelection(cmbDocumentType, GetDocumentTypeDisplayName(currentObject.LinkFileType));
                        break;

                    case LinkType.ExcelData:
                        if (currentObject.ExcelRange != null)
                        {
                            txtExcelPath.Text = currentObject.ExcelRange.FilePath ?? "";
                            txtSheetName.Text = currentObject.ExcelRange.SheetName ?? "";
                            txtCellRange.Text = currentObject.ExcelRange.CellRange ?? "";
                        }
                        break;
                }

                // Update panel visibility
                UpdateLinkPanelVisibility();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading link properties: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isUpdating = false;
            }
        }

        private string GetLinkTypeDisplayName(LinkType linkType)
        {
            return linkType switch
            {
                LinkType.None => "None",
                LinkType.NewPage => "New Page",
                LinkType.Document => "Document",
                LinkType.Url => "URL",
                LinkType.ExcelData => "Excel Data",
                _ => "None"
            };
        }

        private string GetDocumentTypeDisplayName(LinkFileType fileType)
        {
            return fileType switch
            {
                LinkFileType.None => "Video",
                LinkFileType.Video => "Video",
                LinkFileType.PDF => "PDF",
                LinkFileType.Excel => "Excel",
                LinkFileType.Word => "Word",
                _ => "Video"
            };
        }

        private LinkType GetLinkTypeFromDisplayName(string displayName)
        {
            return displayName switch
            {
                "None" => LinkType.None,
                "New Page" => LinkType.NewPage,
                "Document" => LinkType.Document,
                "URL" => LinkType.Url,
                "Excel Data" => LinkType.ExcelData,
                _ => LinkType.None
            };
        }

        private LinkFileType GetDocumentTypeFromDisplayName(string displayName)
        {
            return displayName switch
            {
                "Video" => LinkFileType.Video,
                "PDF" => LinkFileType.PDF,
                "Excel" => LinkFileType.Excel,
                "Word" => LinkFileType.Word,
                _ => LinkFileType.Video
            };
        }

        private void UpdateLinkPanelVisibility()
        {
            // Hide all link panels first
            documentLinkPanel.Visibility = Visibility.Collapsed;
            documentTypePanel.Visibility = Visibility.Collapsed;
            urlLinkPanel.Visibility = Visibility.Collapsed;
            excelLinkPanel.Visibility = Visibility.Collapsed;

            // Show relevant panel based on link type
            switch (currentObject?.LinkType)
            {
                case LinkType.Document:
                    documentLinkPanel.Visibility = Visibility.Visible;
                    documentTypePanel.Visibility = Visibility.Visible;
                    break;
                case LinkType.Url:
                    urlLinkPanel.Visibility = Visibility.Visible;
                    break;
                case LinkType.ExcelData:
                    excelLinkPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void SetComboBoxSelection(ComboBox comboBox, string value)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i].ToString() == value)
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }
            comboBox.SelectedIndex = 0;
        }

        private void UpdateColorPreview()
        {
            if (currentObject == null) return;

            var color = GetColorFromString(currentObject.FillColor ?? "Transparent");
            colorPreview.Background = new SolidColorBrush(color);
        }

        private Color GetColorFromString(string colorName)
        {
            return colorName.ToLower() switch
            {
                "transparent" => Colors.Transparent,
                "black" => Colors.Black,
                "white" => Colors.White,
                "red" => Colors.Red,
                "green" => Colors.Green,
                "blue" => Colors.Blue,
                "yellow" => Colors.Yellow,
                "cyan" => Colors.Cyan,
                "magenta" => Colors.Magenta,
                "gray" => Colors.Gray,
                "light gray" => Colors.LightGray,
                "dark gray" => Colors.DarkGray,
                "orange" => Colors.Orange,
                "purple" => Colors.Purple,
                "brown" => Colors.Brown,
                _ => Colors.Transparent
            };
        }

        private void ApplyChanges()
        {
            if (currentObject == null || currentElement == null || isUpdating) return;

            try
            {
                // Update basic properties
                currentObject.ObjectName = txtObjectName.Text;
                
                if (double.TryParse(txtX.Text, out double x))
                    currentObject.Left = x;
                if (double.TryParse(txtY.Text, out double y))
                    currentObject.Top = y;
                if (double.TryParse(txtWidth.Text, out double width))
                    currentObject.Width = width;
                if (double.TryParse(txtHeight.Text, out double height))
                    currentObject.Height = height;

                // Update appearance properties
                currentObject.FillColor = cmbFillColor.SelectedItem?.ToString();
                currentObject.StrokeColor = cmbStrokeColor.SelectedItem?.ToString();
                currentObject.Opacity = sliderOpacity.Value;
                
                if (double.TryParse(txtStrokeWidth.Text, out double strokeWidth))
                    currentObject.StrokeThickness = strokeWidth;

                // Update specific properties
                UpdateSpecificProperties();

                // Update the UI element
                UpdateUIElement();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying changes: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSpecificProperties()
        {
            if (currentObject == null) return;

            switch (currentObject.ObjectType)
            {
                case "Text":
                    currentObject.Text = txtTextContent.Text;
                    currentObject.FontFamily = cmbFontFamily.SelectedItem?.ToString();
                    if (double.TryParse(txtFontSize.Text, out double fontSize))
                        currentObject.FontSize = fontSize;
                    currentObject.FontWeight = cmbFontWeight.SelectedItem?.ToString();
                    break;

                case "Image":
                    currentObject.ImagePath = txtImagePath.Text;
                    currentObject.StretchMode = cmbStretchMode.SelectedItem?.ToString();
                    break;
            }

            // Update link properties for all object types
            UpdateLinkProperties();
        }

        private void UpdateLinkProperties()
        {
            if (currentObject == null) return;

            // Update link type
            currentObject.LinkType = GetLinkTypeFromDisplayName(cmbLinkType.SelectedItem?.ToString() ?? "None");

            // Update link target - always treat as page name since Link Target is for page creation
            currentObject.LinkPageId = txtLinkTarget.Text?.Trim() ?? "";

            // Update specific link properties based on type (for additional fields)
            switch (currentObject.LinkType)
            {
                case LinkType.Document:
                    currentObject.LinkDocumentPath = txtDocumentPath.Text?.Trim() ?? "";
                    currentObject.LinkFileType = GetDocumentTypeFromDisplayName(cmbDocumentType.SelectedItem?.ToString() ?? "Video");
                    break;

                case LinkType.Url:
                    currentObject.LinkUrl = txtUrl.Text?.Trim() ?? "";
                    break;

                case LinkType.ExcelData:
                    if (currentObject.ExcelRange == null)
                        currentObject.ExcelRange = new ExcelRange();
                    
                    currentObject.ExcelRange.FilePath = txtExcelPath.Text?.Trim() ?? "";
                    currentObject.ExcelRange.SheetName = txtSheetName.Text;
                    currentObject.ExcelRange.CellRange = txtCellRange.Text;
                    break;
            }
        }

        private void UpdateUIElement()
        {
            if (currentElement == null || currentObject == null) return;

            // Update position
            Canvas.SetLeft(currentElement, currentObject.Left);
            Canvas.SetTop(currentElement, currentObject.Top);

            // Update size
            if (currentElement is FrameworkElement fe)
            {
                fe.Width = currentObject.Width;
                fe.Height = currentObject.Height;
            }

            // Update appearance
            if (currentElement is Shape shape)
            {
                shape.Fill = new SolidColorBrush(GetColorFromString(currentObject.FillColor ?? "Transparent"));
                shape.Stroke = new SolidColorBrush(GetColorFromString(currentObject.StrokeColor ?? "Black"));
                shape.StrokeThickness = currentObject.StrokeThickness;
                shape.Opacity = currentObject.Opacity;
            }
            else if (currentElement is TextBlock textBlock)
            {
                textBlock.Text = currentObject.Text ?? "";
                textBlock.FontFamily = new FontFamily(currentObject.FontFamily ?? "Arial");
                textBlock.FontSize = currentObject.FontSize;
                textBlock.FontWeight = GetFontWeight(currentObject.FontWeight ?? "Normal");
                textBlock.Foreground = new SolidColorBrush(GetColorFromString(currentObject.FillColor ?? "Black"));
                textBlock.Opacity = currentObject.Opacity;
            }
            else if (currentElement is Image image)
            {
                image.Opacity = currentObject.Opacity;
                image.Stretch = GetStretchMode(currentObject.StretchMode ?? "Uniform");
            }
        }

        private FontWeight GetFontWeight(string weight)
        {
            return weight.ToLower() switch
            {
                "bold" => FontWeights.Bold,
                "light" => FontWeights.Light,
                _ => FontWeights.Normal
            };
        }

        private Stretch GetStretchMode(string stretch)
        {
            return stretch switch
            {
                "Uniform" => Stretch.Uniform,
                "UniformToFill" => Stretch.UniformToFill,
                "Fill" => Stretch.Fill,
                "None" => Stretch.None,
                _ => Stretch.Uniform
            };
        }

        // Event handlers for property changes
        private void txtObjectName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtX_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtY_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void cmbFillColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;
            UpdateColorPreview();
            ApplyChanges();
        }

        private void sliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdating) return;
            if (txtOpacityValue != null)
            {
                txtOpacityValue.Text = $"{(int)(sliderOpacity.Value * 100)}%";
            }
            ApplyChanges();
        }

        private void cmbStrokeColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtStrokeWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtTextContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void cmbFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtFontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void cmbFontWeight_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtLinkTarget_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtImagePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void btnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Image",
                    Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All Files (*.*)|*.*",
                    DefaultExt = ".png"
                };

                if (dialog.ShowDialog() == true)
                {
                    txtImagePath.Text = dialog.FileName;
                    ApplyChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting image: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbStretchMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        // Link property event handlers
        private void cmbLinkType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;
            UpdateLinkPanelVisibility();
            ApplyChanges();
        }

        private void txtDocumentPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void cmbDocumentType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtExcelPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtSheetName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void txtCellRange_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating) return;
            ApplyChanges();
        }

        private void btnBrowseDocument_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Document",
                    Filter = "All Files (*.*)|*.*",
                    DefaultExt = "*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    txtDocumentPath.Text = dialog.FileName;
                    ApplyChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting document: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBrowseExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Excel File",
                    Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                    DefaultExt = ".xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    txtExcelPath.Text = dialog.FileName;
                    ApplyChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting Excel file: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExportLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentObject == null)
                {
                    MessageBox.Show("No object selected for export.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create link configuration object
                var linkConfig = new LinkConfiguration
                {
                    LinkType = currentObject.LinkType,
                    LinkFileType = currentObject.LinkFileType,
                    LinkPageId = currentObject.LinkPageId,
                    LinkDocumentPath = currentObject.LinkDocumentPath,
                    LinkUrl = currentObject.LinkUrl,
                    LinkText = currentObject.Text,
                    ExcelRange = currentObject.ExcelRange,
                    Metadata = new LinkConfigurationMetadata
                    {
                        ExportedAt = DateTime.Now,
                        Version = "1.0",
                        Description = $"Link configuration for {currentObject.ObjectName}",
                        ObjectName = currentObject.ObjectName,
                        ObjectType = currentObject.ObjectType
                    }
                };

                // Serialize to JSON
                var json = JsonSerializer.Serialize(linkConfig, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                // Show save dialog
                var dialog = new SaveFileDialog
                {
                    Title = "Export Link Configuration",
                    Filter = "Link Configuration (*.link)|*.link|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = ".link",
                    FileName = $"LinkConfig_{currentObject.ObjectName}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, json);
                    MessageBox.Show($"Link configuration exported successfully!\n\nFile: {dialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting link configuration: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnImportLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentObject == null)
                {
                    MessageBox.Show("No object selected for import.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Show open dialog
                var dialog = new OpenFileDialog
                {
                    Title = "Import Link Configuration",
                    Filter = "Link Configuration (*.link)|*.link|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = ".link"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Read and deserialize JSON
                    var json = File.ReadAllText(dialog.FileName);
                    var linkConfig = JsonSerializer.Deserialize<LinkConfiguration>(json);

                    if (linkConfig == null)
                    {
                        MessageBox.Show("Invalid link configuration file.", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Apply the imported configuration
                    ApplyImportedLinkConfiguration(linkConfig);
                    
                    MessageBox.Show($"Link configuration imported successfully!", 
                        "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing link configuration: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyImportedLinkConfiguration(LinkConfiguration linkConfig)
        {
            if (currentObject == null || linkConfig == null) return;

            // Update the object with imported configuration
            currentObject.LinkType = linkConfig.LinkType;
            currentObject.LinkFileType = linkConfig.LinkFileType;
            currentObject.LinkPageId = linkConfig.LinkPageId;
            currentObject.LinkDocumentPath = linkConfig.LinkDocumentPath;
            currentObject.LinkUrl = linkConfig.LinkUrl;
            currentObject.Text = linkConfig.LinkText;
            currentObject.ExcelRange = linkConfig.ExcelRange;

            // Reload the UI to reflect changes
            LoadLinkProperties();
        }

        // Window control event handlers
        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyChanges();

                // --- Page Creation Logic (for any link type) ---
                if (currentObject != null && currentProject != null && currentPage != null)
                {
                    string pageName = txtLinkTarget.Text?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(pageName))
                    {
                        // Check if a page with this name already exists
                        var existingPage = currentProject.Pages.FirstOrDefault(p => p.PageName == pageName);
                        if (existingPage == null)
                        {
                            // Create new page like the main page
                            var newPage = new PageData
                            {
                                PageId = Guid.NewGuid().ToString(),
                                PageName = pageName,
                                ParentPageId = currentPage.PageId, // Make it a child of current page
                                Objects = new List<ExploderObject>(),
                                PageSettings = currentPage.PageSettings.Clone()
                            };

                            // Add to project
                            currentProject.Pages.Add(newPage);
                            
                            // Link the current object to the new page
                            currentObject.LinkPageId = newPage.PageId;
                            
                            // Set the newly created page for MainWindow to navigate to
                            NewlyCreatedPage = newPage;
                        }
                        else
                        {
                            // Link to existing page
                            currentObject.LinkPageId = existingPage.PageId;
                            
                            // Set the existing page for MainWindow to navigate to
                            NewlyCreatedPage = existingPage;
                        }
                    }
                }

                // Set dialog result to true to indicate successful completion
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying changes: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 