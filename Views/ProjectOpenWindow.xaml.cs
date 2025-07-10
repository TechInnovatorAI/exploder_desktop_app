using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Exploder.Models;

namespace Exploder.Views
{
    public partial class ProjectOpenWindow : Window
    {
        public ProjectData? ProjectData { get; private set; }
        public string? SelectedProjectPath { get; private set; }
        
        private readonly string recentProjectsFile = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "Exploder", "recent_projects.txt");

        public ProjectOpenWindow()
        {
            InitializeComponent();
            LoadRecentProjects();
            SetDefaultProjectFolder();
        }

        private void SetDefaultProjectFolder()
        {
            string defaultFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                "Exploder Projects");
            
            if (!Directory.Exists(defaultFolder))
            {
                Directory.CreateDirectory(defaultFolder);
            }
            
            btnBrowseFolder.Tag = defaultFolder;
            txtProjectFolder.Text = defaultFolder;
        }

        private void LoadRecentProjects()
        {
            try
            {
                if (File.Exists(recentProjectsFile))
                {
                    var recentProjects = File.ReadAllLines(recentProjectsFile)
                        .Where(line => !string.IsNullOrWhiteSpace(line) && File.Exists(line))
                        .Take(10)
                        .ToList();

                    lstRecentProjects.ItemsSource = recentProjects;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading recent projects: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveRecentProject(string projectPath)
        {
            try
            {
                var directory = System.IO.Path.GetDirectoryName(recentProjectsFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var recentProjects = new List<string>();
                
                // Load existing projects
                if (File.Exists(recentProjectsFile))
                {
                    recentProjects = File.ReadAllLines(recentProjectsFile).ToList();
                }

                // Add new project to the beginning
                recentProjects.Remove(projectPath); // Remove if already exists
                recentProjects.Insert(0, projectPath);

                // Keep only the last 10 projects
                recentProjects = recentProjects.Take(10).ToList();

                File.WriteAllLines(recentProjectsFile, recentProjects);
            }
            catch (Exception ex)
            {
                // Silently fail - this is not critical
                System.Diagnostics.Debug.WriteLine($"Error saving recent project: {ex.Message}");
            }
        }

        private void btnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Project Folder",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                var folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    btnBrowseFolder.Tag = folderPath;
                    txtProjectFolder.Text = folderPath;
                }
            }
        }

        private void BtnColorPicker_Click(object sender, RoutedEventArgs e)
        {
            // For now, use a simple color picker approach
            // In a full implementation, you might want to create a custom color picker dialog
            var color = System.Windows.Media.Colors.LightBlue;
            colorPreview.Fill = new SolidColorBrush(color);
        }

        private void ComboPageSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (comboPageSize.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                // Update page size preview if needed
                // This could show a preview of the selected page size
            }
        }

        private void LstRecentProjects_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            btnOpenProject.IsEnabled = lstRecentProjects.SelectedItem != null;
        }

        private void LstRecentProjects_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstRecentProjects.SelectedItem != null)
            {
                btnOpenProject_Click(sender, e);
            }
        }

        private void btnOpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (lstRecentProjects.SelectedItem is string projectPath)
            {
                SelectedProjectPath = projectPath;
                DialogResult = true;
                Close();
            }
        }

        private void BtnBrowseProject_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Exploder Project",
                Filter = "Exploder Project (*.exp)|*.exp|All Files (*.*)|*.*",
                DefaultExt = ".exp"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedProjectPath = dialog.FileName;
                DialogResult = true;
                Close();
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedIndex == 0) // New Project tab
            {
                if (string.IsNullOrWhiteSpace(txtProjectName.Text))
                {
                    MessageBox.Show("Please enter a project name.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtProjectFolder.Text))
                {
                    MessageBox.Show("Please select a project folder.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create new project data
                ProjectData = new ProjectData
                {
                    ProjectName = txtProjectName.Text.Trim(),
                    ProjectPath = txtProjectFolder.Text,
                    PageSettings = new PageSettings
                    {
                        PageSize = GetSelectedPageSize(),
                        Orientation = radioPortrait.IsChecked == true ? "Portrait" : "Landscape",
                        MarginSize = double.TryParse(txtMarginSize.Text, out double margin) ? margin : 20.0,
                        BackgroundColor = GetColorString(colorPreview.Fill),
                        ShowGrid = chkShowGrid.IsChecked == true,
                        ShowRulers = chkShowRulers.IsChecked == true
                    }
                };

                // Set page dimensions based on selection
                SetPageDimensions(ProjectData.PageSettings);

                // Create initial page
                var initialPage = new PageData
                {
                    PageId = Guid.NewGuid().ToString(),
                    PageName = "Main Page",
                    PageSettings = ProjectData.PageSettings.Clone()
                };
                ProjectData.Pages.Add(initialPage);

                DialogResult = true;
                Close();
            }
        }

        private string GetSelectedPageSize()
        {
            if (comboPageSize.SelectedItem is System.Windows.Controls.ComboBoxItem item && 
                item.Tag is string tag)
            {
                return tag;
            }
            return "A4";
        }

        private string GetColorString(System.Windows.Media.Brush brush)
        {
            if (brush is SolidColorBrush solidBrush)
            {
                return $"#{solidBrush.Color.R:X2}{solidBrush.Color.G:X2}{solidBrush.Color.B:X2}";
            }
            return "#FFFFFF";
        }

        private void SetPageDimensions(PageSettings settings)
        {
            switch (settings.PageSize)
            {
                case "A4":
                    settings.Width = 210.0;
                    settings.Height = 297.0;
                    break;
                case "Letter":
                    settings.Width = 215.9; // 8.5 inches in mm
                    settings.Height = 279.4; // 11 inches in mm
                    break;
                case "Legal":
                    settings.Width = 215.9; // 8.5 inches in mm
                    settings.Height = 355.6; // 14 inches in mm
                    break;
                case "A3":
                    settings.Width = 297.0;
                    settings.Height = 420.0;
                    break;
                default:
                    settings.Width = 210.0;
                    settings.Height = 297.0;
                    break;
            }

            if (settings.Orientation == "Landscape")
            {
                var temp = settings.Width;
                settings.Width = settings.Height;
                settings.Height = temp;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
