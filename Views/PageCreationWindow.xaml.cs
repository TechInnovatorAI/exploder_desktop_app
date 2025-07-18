using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Exploder.Models;

namespace Exploder.Views
{
    public partial class PageCreationWindow : Window
    {
        public PageData? CreatedPage { get; private set; }
        public PageSettings? PageSettings { get; private set; }
        
        private ProjectData? currentProject;
        private Dictionary<string, (double width, double height)> pageSizes = new();

        public PageCreationWindow(ProjectData? project = null)
        {
            InitializeComponent();
            currentProject = project;
            InitializePageSizes();
            LoadParentPages();
            UpdateStatus("Ready to create page");
        }

        private void InitializePageSizes()
        {
            pageSizes = new Dictionary<string, (double width, double height)>
            {
                { "A4 (210 x 297 mm)", (210, 297) },
                { "Letter (216 x 279 mm)", (216, 279) },
                { "Legal (216 x 356 mm)", (216, 356) },
                { "A3 (297 x 420 mm)", (297, 420) },
                { "A5 (148 x 210 mm)", (148, 210) }
            };
        }

        private void LoadParentPages()
        {
            cmbParentPage.Items.Clear();
            cmbParentPage.Items.Add("None (Root Page)");

            if (currentProject?.Pages != null)
            {
                foreach (var page in currentProject.Pages)
                {
                    cmbParentPage.Items.Add(page.PageName);
                }
            }

            cmbParentPage.SelectedIndex = 0;
        }

        private void UpdateStatus(string message)
        {
            txtStatus.Text = message;
        }

        private void txtPageName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputs();
        }

        private void cmbParentPage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateInputs();
        }

        private void cmbPageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = cmbPageSize.SelectedItem as ComboBoxItem;
            if (selectedItem?.Content.ToString() == "Custom")
            {
                customDimensionsPanel.Visibility = Visibility.Visible;
                UpdateCustomDimensions();
            }
            else
            {
                customDimensionsPanel.Visibility = Visibility.Collapsed;
                UpdatePageDimensions();
            }
            ValidateInputs();
        }

        private void txtCustomDimensions_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCustomDimensions();
            ValidateInputs();
        }

        private void txtMarginSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputs();
        }

        private void cmbBackgroundColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateInputs();
        }

        private void cmbTemplateType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = cmbTemplateType.SelectedItem as ComboBoxItem;
            var templateType = selectedItem?.Content.ToString();

            if (templateType != "Blank Page")
            {
                templateOptionsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                templateOptionsPanel.Visibility = Visibility.Collapsed;
            }

            ValidateInputs();
        }

        private void UpdatePageDimensions()
        {
            var selectedItem = cmbPageSize.SelectedItem as ComboBoxItem;
            if (selectedItem?.Content?.ToString() != null && pageSizes.ContainsKey(selectedItem.Content.ToString()))
            {
                var (width, height) = pageSizes[selectedItem.Content.ToString()];
                
                if (radioLandscape.IsChecked == true)
                {
                    // Swap width and height for landscape
                    (width, height) = (height, width);
                }

                txtCustomWidth.Text = width.ToString();
                txtCustomHeight.Text = height.ToString();
            }
        }

        private void UpdateCustomDimensions()
        {
            // This method is called when custom dimensions are being used
            // The dimensions are already set in the text boxes
        }

        private void ValidateInputs()
        {
            bool isValid = true;
            string errorMessage = "";

            // Validate page name
            if (string.IsNullOrWhiteSpace(txtPageName.Text))
            {
                isValid = false;
                errorMessage = "Page name is required";
            }

            // Validate custom dimensions if custom size is selected
            if (cmbPageSize.SelectedItem is ComboBoxItem selectedSize && 
                selectedSize.Content.ToString() == "Custom")
            {
                if (!double.TryParse(txtCustomWidth.Text, out double width) || width <= 0)
                {
                    isValid = false;
                    errorMessage = "Invalid custom width";
                }
                else if (!double.TryParse(txtCustomHeight.Text, out double height) || height <= 0)
                {
                    isValid = false;
                    errorMessage = "Invalid custom height";
                }
            }

            // Validate margin size
            if (!double.TryParse(txtMarginSize.Text, out double margin) || margin < 0)
            {
                isValid = false;
                errorMessage = "Invalid margin size";
            }

            // Update UI based on validation
            if (isValid)
            {
                UpdateStatus("Ready to create page");
                btnCreate.IsEnabled = true;
            }
            else
            {
                UpdateStatus($"Error: {errorMessage}");
                btnCreate.IsEnabled = false;
            }
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Creating page...");

                // Create page settings
                var settings = CreatePageSettings();

                // Create the page
                var page = new PageData
                {
                    PageId = Guid.NewGuid().ToString(),
                    PageName = txtPageName.Text.Trim(),
                    PageSettings = settings,
                    Objects = new List<ExploderObject>()
                };

                // Set parent page if selected
                if (cmbParentPage.SelectedIndex > 0)
                {
                    var parentPageName = cmbParentPage.SelectedItem.ToString();
                    var parentPage = currentProject?.Pages.FirstOrDefault(p => p.PageName == parentPageName);
                    if (parentPage != null)
                    {
                        page.ParentPageId = parentPage.PageId;
                    }
                }

                // Add template objects if selected
                AddTemplateObjects(page);

                CreatedPage = page;
                PageSettings = settings;

                UpdateStatus("Page created successfully!");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error creating page: {ex.Message}");
                MessageBox.Show($"Error creating page: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private PageSettings CreatePageSettings()
        {
            // Get page dimensions
            double width = 210, height = 297; // Default to A4
            if (cmbPageSize.SelectedItem is ComboBoxItem selectedSize && 
                selectedSize.Content.ToString() == "Custom")
            {
                width = double.Parse(txtCustomWidth.Text);
                height = double.Parse(txtCustomHeight.Text);
            }
            else
            {
                var selectedItem = cmbPageSize.SelectedItem as ComboBoxItem;
                if (selectedItem?.Content?.ToString() != null && pageSizes.ContainsKey(selectedItem.Content.ToString()))
                {
                    (width, height) = pageSizes[selectedItem.Content.ToString()];
                }
            }

            // Swap dimensions for landscape
            if (radioLandscape.IsChecked == true)
            {
                (width, height) = (height, width);
            }

            // Get background color
            var backgroundColor = GetBackgroundColor();

            return new PageSettings
            {
                PageSize = cmbPageSize.SelectedItem?.ToString() ?? "A4",
                Orientation = radioPortrait.IsChecked == true ? "Portrait" : "Landscape",
                MarginSize = double.Parse(txtMarginSize.Text),
                BackgroundColor = backgroundColor,
                ShowGrid = chkShowGrid.IsChecked == true,
                ShowRulers = chkShowRulers.IsChecked == true,
                Width = width,
                Height = height
            };
        }

        private string GetBackgroundColor()
        {
            var selectedItem = cmbBackgroundColor.SelectedItem as ComboBoxItem;
            return selectedItem?.Content.ToString() switch
            {
                "White" => "#FFFFFF",
                "Light Gray" => "#F5F5F5",
                "Gray" => "#808080",
                "Dark Gray" => "#404040",
                "Black" => "#000000",
                "Light Blue" => "#E6F3FF",
                "Light Green" => "#E6FFE6",
                "Light Yellow" => "#FFFFE6",
                "Transparent" => "#00000000",
                _ => "#FFFFFF"
            };
        }

        private void AddTemplateObjects(PageData page)
        {
            var templateType = cmbTemplateType.SelectedItem?.ToString();
            if (templateType == "Blank Page") return;

            var objects = new List<ExploderObject>();
            int zIndex = 0;

            // Add title if selected
            if (chkAddTitle.IsChecked == true)
            {
                var titleObject = new ExploderObject
                {
                    ObjectId = Guid.NewGuid().ToString(),
                    ObjectName = "PageTitle",
                    ObjectType = "Text",
                    Left = 50,
                    Top = 50,
                    Width = page.PageSettings.Width - 100,
                    Height = 60,
                    Text = page.PageName,
                    FontFamily = "Arial",
                    FontSize = 24,
                    FontWeight = "Bold",
                    TextColor = "#000000",
                    FillColor = "#FFFFFF",
                    StrokeColor = "#000000",
                    StrokeThickness = 0,
                    Opacity = 1.0,
                    ZIndex = zIndex++,
                    LinkType = LinkType.None
                };
                objects.Add(titleObject);
            }

            // Add navigation buttons if selected
            if (chkAddNavigation.IsChecked == true)
            {
                // Back button
                var backButton = new ExploderObject
                {
                    ObjectId = Guid.NewGuid().ToString(),
                    ObjectName = "BackButton",
                    ObjectType = "Rectangle",
                    Left = 20,
                    Top = page.PageSettings.Height - 80,
                    Width = 80,
                    Height = 30,
                    FillColor = "#007ACC",
                    StrokeColor = "#005A9E",
                    StrokeThickness = 1,
                    Opacity = 1.0,
                    ZIndex = zIndex++,
                    LinkType = LinkType.NewPage,
                    Text = "Back"
                };
                objects.Add(backButton);

                // Home button
                var homeButton = new ExploderObject
                {
                    ObjectId = Guid.NewGuid().ToString(),
                    ObjectName = "HomeButton",
                    ObjectType = "Rectangle",
                    Left = 120,
                    Top = page.PageSettings.Height - 80,
                    Width = 80,
                    Height = 30,
                    FillColor = "#28A745",
                    StrokeColor = "#1E7E34",
                    StrokeThickness = 1,
                    Opacity = 1.0,
                    ZIndex = zIndex++,
                    LinkType = LinkType.NewPage,
                    Text = "Home"
                };
                objects.Add(homeButton);
            }

            // Add content area if selected
            if (chkAddContentArea.IsChecked == true)
            {
                var contentArea = new ExploderObject
                {
                    ObjectId = Guid.NewGuid().ToString(),
                    ObjectName = "ContentArea",
                    ObjectType = "Rectangle",
                    Left = 50,
                    Top = 150,
                    Width = page.PageSettings.Width - 100,
                    Height = page.PageSettings.Height - 250,
                    FillColor = "#FFFFFF",
                    StrokeColor = "#CCCCCC",
                    StrokeThickness = 2,
                    Opacity = 1.0,
                    ZIndex = zIndex++,
                    LinkType = LinkType.None
                };
                objects.Add(contentArea);
            }

            // Add template-specific objects
            switch (templateType)
            {
                case "Title Page":
                    AddTitlePageObjects(page, objects, ref zIndex);
                    break;
                case "Content Page":
                    AddContentPageObjects(page, objects, ref zIndex);
                    break;
                case "Navigation Page":
                    AddNavigationPageObjects(page, objects, ref zIndex);
                    break;
                case "Form Page":
                    AddFormPageObjects(page, objects, ref zIndex);
                    break;
                case "Chart Page":
                    AddChartPageObjects(page, objects, ref zIndex);
                    break;
                case "Image Gallery":
                    AddImageGalleryObjects(page, objects, ref zIndex);
                    break;
            }

            page.Objects.AddRange(objects);
        }

        private void AddTitlePageObjects(PageData page, List<ExploderObject> objects, ref int zIndex)
        {
            // Add subtitle
            var subtitle = new ExploderObject
            {
                ObjectId = Guid.NewGuid().ToString(),
                ObjectName = "Subtitle",
                ObjectType = "Text",
                Left = 50,
                Top = 120,
                Width = page.PageSettings.Width - 100,
                Height = 40,
                Text = "Subtitle goes here",
                FontFamily = "Arial",
                FontSize = 16,
                FontWeight = "Normal",
                TextColor = "#666666",
                FillColor = "#FFFFFF",
                StrokeColor = "#000000",
                StrokeThickness = 0,
                Opacity = 1.0,
                ZIndex = zIndex++,
                LinkType = LinkType.None
            };
            objects.Add(subtitle);

            // Add date
            var dateText = new ExploderObject
            {
                ObjectId = Guid.NewGuid().ToString(),
                ObjectName = "Date",
                ObjectType = "Text",
                Left = 50,
                Top = page.PageSettings.Height - 100,
                Width = 200,
                Height = 30,
                Text = DateTime.Now.ToString("MMMM dd, yyyy"),
                FontFamily = "Arial",
                FontSize = 12,
                FontWeight = "Normal",
                TextColor = "#666666",
                FillColor = "#FFFFFF",
                StrokeColor = "#000000",
                StrokeThickness = 0,
                Opacity = 1.0,
                ZIndex = zIndex++,
                LinkType = LinkType.None
            };
            objects.Add(dateText);
        }

        private void AddContentPageObjects(PageData page, List<ExploderObject> objects, ref int zIndex)
        {
            // Add content placeholder
            var contentPlaceholder = new ExploderObject
            {
                ObjectId = Guid.NewGuid().ToString(),
                ObjectName = "ContentPlaceholder",
                ObjectType = "Text",
                Left = 70,
                Top = 170,
                Width = page.PageSettings.Width - 140,
                Height = 100,
                Text = "Content goes here...\n\nAdd your text, images, and other content in this area.",
                FontFamily = "Arial",
                FontSize = 14,
                FontWeight = "Normal",
                TextColor = "#333333",
                FillColor = "#FFFFFF",
                StrokeColor = "#000000",
                StrokeThickness = 0,
                Opacity = 1.0,
                ZIndex = zIndex++,
                LinkType = LinkType.None
            };
            objects.Add(contentPlaceholder);
        }

        private void AddNavigationPageObjects(PageData page, List<ExploderObject> objects, ref int zIndex)
        {
            // Add navigation menu items
            string[] menuItems = { "Option 1", "Option 2", "Option 3", "Option 4" };
            double startY = 200;
            double itemHeight = 50;

            for (int i = 0; i < menuItems.Length; i++)
            {
                var menuItem = new ExploderObject
                {
                    ObjectId = Guid.NewGuid().ToString(),
                    ObjectName = $"MenuItem{i + 1}",
                    ObjectType = "Rectangle",
                    Left = 100,
                    Top = startY + (i * itemHeight),
                    Width = 200,
                    Height = 40,
                    FillColor = "#007ACC",
                    StrokeColor = "#005A9E",
                    StrokeThickness = 1,
                    Opacity = 1.0,
                    ZIndex = zIndex++,
                    LinkType = LinkType.NewPage,
                    Text = menuItems[i]
                };
                objects.Add(menuItem);
            }
        }

        private void AddFormPageObjects(PageData page, List<ExploderObject> objects, ref int zIndex)
        {
            // Add form fields
            string[] fieldLabels = { "Name:", "Email:", "Phone:", "Message:" };
            double startY = 200;
            double fieldHeight = 60;

            for (int i = 0; i < fieldLabels.Length; i++)
            {
                // Label
                var label = new ExploderObject
                {
                    ObjectId = Guid.NewGuid().ToString(),
                    ObjectName = $"Label{i + 1}",
                    ObjectType = "Text",
                    Left = 50,
                    Top = startY + (i * fieldHeight),
                    Width = 100,
                    Height = 30,
                    Text = fieldLabels[i],
                    FontFamily = "Arial",
                    FontSize = 12,
                    FontWeight = "Bold",
                    TextColor = "#333333",
                    FillColor = "#FFFFFF",
                    StrokeColor = "#000000",
                    StrokeThickness = 0,
                    Opacity = 1.0,
                    ZIndex = zIndex++,
                    LinkType = LinkType.None
                };
                objects.Add(label);

                // Input field
                var inputField = new ExploderObject
                {
                    ObjectId = Guid.NewGuid().ToString(),
                    ObjectName = $"InputField{i + 1}",
                    ObjectType = "Rectangle",
                    Left = 160,
                    Top = startY + (i * fieldHeight),
                    Width = 300,
                    Height = 30,
                    FillColor = "#FFFFFF",
                    StrokeColor = "#CCCCCC",
                    StrokeThickness = 1,
                    Opacity = 1.0,
                    ZIndex = zIndex++,
                    LinkType = LinkType.None
                };
                objects.Add(inputField);
            }
        }

        private void AddChartPageObjects(PageData page, List<ExploderObject> objects, ref int zIndex)
        {
            // Add chart placeholder
            var chartPlaceholder = new ExploderObject
            {
                ObjectId = Guid.NewGuid().ToString(),
                ObjectName = "ChartPlaceholder",
                ObjectType = "Rectangle",
                Left = 100,
                Top = 200,
                Width = 400,
                Height = 300,
                FillColor = "#F8F9FA",
                StrokeColor = "#DEE2E6",
                StrokeThickness = 2,
                Opacity = 1.0,
                ZIndex = zIndex++,
                LinkType = LinkType.None
            };
            objects.Add(chartPlaceholder);

            // Add chart title
            var chartTitle = new ExploderObject
            {
                ObjectId = Guid.NewGuid().ToString(),
                ObjectName = "ChartTitle",
                ObjectType = "Text",
                Left = 100,
                Top = 170,
                Width = 400,
                Height = 30,
                Text = "Chart Title",
                FontFamily = "Arial",
                FontSize = 16,
                FontWeight = "Bold",
                TextColor = "#333333",
                FillColor = "#FFFFFF",
                StrokeColor = "#000000",
                StrokeThickness = 0,
                Opacity = 1.0,
                ZIndex = zIndex++,
                LinkType = LinkType.None
            };
            objects.Add(chartTitle);
        }

        private void AddImageGalleryObjects(PageData page, List<ExploderObject> objects, ref int zIndex)
        {
            // Add image placeholders in a grid
            int columns = 3;
            int rows = 2;
            double imageWidth = 150;
            double imageHeight = 120;
            double startX = 50;
            double startY = 200;
            double spacing = 20;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    var imagePlaceholder = new ExploderObject
                    {
                        ObjectId = Guid.NewGuid().ToString(),
                        ObjectName = $"ImagePlaceholder{row * columns + col + 1}",
                        ObjectType = "Rectangle",
                        Left = startX + (col * (imageWidth + spacing)),
                        Top = startY + (row * (imageHeight + spacing)),
                        Width = imageWidth,
                        Height = imageHeight,
                        FillColor = "#E9ECEF",
                        StrokeColor = "#CED4DA",
                        StrokeThickness = 1,
                        Opacity = 1.0,
                        ZIndex = zIndex++,
                        LinkType = LinkType.None
                    };
                    objects.Add(imagePlaceholder);
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 