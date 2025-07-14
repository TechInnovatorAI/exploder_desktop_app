using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using Exploder.Models;

namespace Exploder.Views
{
    public partial class ObjectPropertiesWindow : Window
    {
        public ExploderObject? ObjectData { get; private set; }
        // Removed unused originalObject field

        public string ButtonContent => txtButtonContent.Text;
        public string ButtonColor => cmbButtonColor.SelectedItem?.ToString() ?? "#FFFFFF";
        public string LinkTypeString => (cmbLinkType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";
        public string LinkTarget => txtLinkTarget.Text;
        public string TextColor => cmbTextColor.SelectedItem?.ToString() ?? "#000000";
        public double StrokeThickness => double.TryParse((cmbTextSize.SelectedItem as ComboBoxItem)?.Content?.ToString(), out var size) ? size : 1.0;
        public string TextContent => txtTextContent.Text;
        public string SelectedFontFamily => (cmbFontFamily.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Arial";
        public double SelectedFontSize => double.TryParse((cmbFontSize.SelectedItem as ComboBoxItem)?.Content?.ToString(), out var size) ? size : 12.0;

        public ObjectPropertiesWindow(ExploderObject obj)
        {
            InitializeComponent();
            ObjectData = obj;
            // Populate color combos
            var colorList = new[] { "#000000", "#FFFFFF", "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF" };
            cmbButtonColor.ItemsSource = colorList;
            cmbTextColor.ItemsSource = colorList;
            cmbButtonColor.SelectedItem = obj.FillColor ?? "#FFFFFF";
            cmbTextColor.SelectedItem = obj.StrokeColor ?? "#000000";
            // Populate stroke thickness combo
            cmbTextSize.SelectedItem = obj.StrokeThickness.ToString();
            
            // Initialize text content
            txtTextContent.Text = obj.Text ?? "";
            
            // Initialize font family
            cmbFontFamily.SelectedItem = obj.FontFamily ?? "Arial";
            
            // Initialize font size
            cmbFontSize.SelectedItem = obj.FontSize.ToString();
            
            cmbLinkType.SelectedIndex = 0;
            txtButtonContent.Text = obj.ObjectName;
            txtLinkTarget.Text = obj.LinkTarget;
            // Set up event handlers
            btnOK.Click += BtnOK_Click;
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
            cmbLinkType.SelectionChanged += CmbLinkType_SelectionChanged;
            btnBrowse.Click += BtnBrowse_Click;
            cmbButtonColor.SelectionChanged += (s, e) => UpdateColorPreviews();
            cmbTextColor.SelectionChanged += (s, e) => UpdateColorPreviews();
            // Initialize the UI based on current link type
            UpdateLinkTargetUI();
            UpdateColorPreviews();
        }

        // Remove all fields, methods, and logic referencing old controls.
        // Only keep the constructor, BtnOK_Click, and properties for the new controls.

        private void BtnOK_Click(object? sender, RoutedEventArgs e)
        {
            if (ObjectData != null)
            {
                // Validate page name if link type is Page
                if (LinkTypeString == "Page" && string.IsNullOrWhiteSpace(txtLinkTarget.Text))
                {
                    MessageBox.Show("Please enter a page name for the Page link type.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ObjectData.ObjectName = txtButtonContent.Text;
                ObjectData.FillColor = ButtonColor;
                ObjectData.StrokeColor = TextColor;
                ObjectData.StrokeThickness = StrokeThickness;
                ObjectData.Text = TextContent;
                ObjectData.FontFamily = SelectedFontFamily;
                ObjectData.FontSize = SelectedFontSize;
                ObjectData.LinkTarget = txtLinkTarget.Text;
                ObjectData.LinkType = LinkType.None;
                ObjectData.LinkFileType = LinkFileType.None;
                ObjectData.LinkDocumentPath = "";
                switch (LinkTypeString)
                {
                    case "Page":
                        ObjectData.LinkType = LinkType.NewPage;
                        break;
                    case "URL":
                        ObjectData.LinkType = LinkType.Url;
                        break;
                    case "Video":
                        ObjectData.LinkType = LinkType.Document;
                        ObjectData.LinkFileType = LinkFileType.Video;
                        ObjectData.LinkDocumentPath = txtLinkTarget.Text;
                        break;
                    case "PDF":
                        ObjectData.LinkType = LinkType.Document;
                        ObjectData.LinkFileType = LinkFileType.PDF;
                        ObjectData.LinkDocumentPath = txtLinkTarget.Text;
                        break;
                    case "Excel":
                        ObjectData.LinkType = LinkType.Document;
                        ObjectData.LinkFileType = LinkFileType.Excel;
                        ObjectData.LinkDocumentPath = txtLinkTarget.Text;
                        break;
                    case "Word":
                        ObjectData.LinkType = LinkType.Document;
                        ObjectData.LinkFileType = LinkFileType.Word;
                        ObjectData.LinkDocumentPath = txtLinkTarget.Text;
                        break;
                }
            }
            DialogResult = true;
            Close();
        }

        private string GetColorString(System.Windows.Media.Brush brush)
        {
            if (brush is SolidColorBrush solidBrush)
            {
                return $"#{solidBrush.Color.R:X2}{solidBrush.Color.G:X2}{solidBrush.Color.B:X2}";
            }
            return "#FFFFFF";
        }

        private void CmbLinkType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateLinkTargetUI();
        }

        private void UpdateLinkTargetUI()
        {
            var linkType = LinkTypeString;
            switch (linkType)
            {
                case "Page":
                    txtLinkTarget.ToolTip = "Enter the name of the page to navigate to. If the page doesn't exist, it will be created.";
                    btnBrowse.IsEnabled = false;
                    break;
                case "URL":
                    txtLinkTarget.ToolTip = "Enter the URL to open (e.g., https://example.com)";
                    btnBrowse.IsEnabled = false;
                    break;
                case "Video":
                case "PDF":
                case "Excel":
                case "Word":
                    txtLinkTarget.ToolTip = "Enter the file path or click Browse to select a file";
                    btnBrowse.IsEnabled = true;
                    break;
                default:
                    txtLinkTarget.ToolTip = "Select a link type first";
                    btnBrowse.IsEnabled = false;
                    break;
            }
        }

        private void UpdateColorPreviews()
        {
            if (cmbButtonColor.SelectedItem is string btnColor)
                lblButtonColorPreview.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnColor));
            if (cmbTextColor.SelectedItem is string txtColor)
                lblTextColorPreview.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(txtColor));
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var linkType = LinkTypeString;
            string filter = "";
            LinkFileType fileType = LinkFileType.None;
            switch (linkType)
            {
                case "Video":
                    filter = "Video Files|*.mp4;*.avi;*.mov;*.wmv;*.flv|All Files|*.*";
                    fileType = LinkFileType.Video;
                    break;
                case "PDF":
                    filter = "PDF Files|*.pdf|All Files|*.*";
                    fileType = LinkFileType.PDF;
                    break;
                case "Excel":
                    filter = "Excel Files|*.xlsx;*.xls|All Files|*.*";
                    fileType = LinkFileType.Excel;
                    break;
                case "Word":
                    filter = "Word Files|*.docx;*.doc|All Files|*.*";
                    fileType = LinkFileType.Word;
                    break;
                default:
                    filter = "All Files|*.*";
                    fileType = LinkFileType.None;
                    break;
            }

            var dialog = new OpenFileDialog
            {
                Title = $"Select {linkType} File",
                Filter = filter
            };

            if (dialog.ShowDialog() == true)
            {
                txtLinkTarget.Text = dialog.FileName;
                if (ObjectData != null)
                {
                    ObjectData.LinkFileType = fileType;
                    ObjectData.LinkDocumentPath = dialog.FileName;
                }
            }
        }

        private void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 