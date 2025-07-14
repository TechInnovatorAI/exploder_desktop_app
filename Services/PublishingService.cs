using Exploder.Models;
using System.IO.Compression;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace Exploder.Services
{
    public class PublishingService : IPublishingService
    {
        public async Task<bool> PublishProjectAsync(ProjectData project, string outputPath)
        {
            try
            {
                // Validate project before publishing
                if (!await ValidateProjectForPublishingAsync(project))
                {
                    return false;
                }

                // Create a copy of the project for publishing (remove edit-specific data)
                var publishedProject = CreatePublishedVersion(project);
                publishedProject.Sanitize();

                // Serialize the published project
                var json = JsonSerializer.Serialize(publishedProject, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                // Create output directory if it doesn't exist
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Write the published project file
                await File.WriteAllTextAsync(outputPath, json);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Publishing failed: {ex.Message}");
                return false;
            }
        }

        public async Task<string> CreateSelfExecutingPackageAsync(ProjectData project, string outputPath)
        {
            try
            {
                project.Sanitize();
                // Create a temporary directory for the package
                var tempDir = Path.Combine(Path.GetTempPath(), $"Exploder_Publish_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);

                // Create the published project file
                var projectFile = Path.Combine(tempDir, "project.exp");
                if (!await PublishProjectAsync(project, projectFile))
                {
                    return string.Empty;
                }

                // Copy required assets
                await CopyProjectAssetsAsync(project, tempDir);

                // Create a simple HTML viewer for the published project
                var htmlViewer = CreateHtmlViewer(project);
                var htmlPath = Path.Combine(tempDir, "viewer.html");
                await File.WriteAllTextAsync(htmlPath, htmlViewer);

                // Create a batch file to open the viewer
                var batchContent = CreateBatchLauncher();
                var batchPath = Path.Combine(tempDir, "launch.bat");
                await File.WriteAllTextAsync(batchPath, batchContent);

                // Create the ZIP package
                var zipPath = outputPath.EndsWith(".zip") ? outputPath : outputPath + ".zip";
                ZipFile.CreateFromDirectory(tempDir, zipPath);

                // Clean up temporary directory
                Directory.Delete(tempDir, true);

                return zipPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Package creation failed: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<bool> ValidateProjectForPublishingAsync(ProjectData project)
        {
            if (project == null || string.IsNullOrEmpty(project.ProjectName))
            {
                return false;
            }

            if (project.Pages == null || project.Pages.Count == 0)
            {
                return false;
            }

            // Check if all referenced files exist
            foreach (var page in project.Pages)
            {
                foreach (var obj in page.Objects)
                {
                    if (obj.LinkType == LinkType.Document && !string.IsNullOrEmpty(obj.LinkDocumentPath))
                    {
                        if (!File.Exists(obj.LinkDocumentPath))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private ProjectData CreatePublishedVersion(ProjectData original)
        {
            // Create a copy of the project with only the necessary data for viewing
            var published = new ProjectData
            {
                ProjectName = original.ProjectName + " (Published)",
                ProjectPath = original.ProjectPath,
                PageSettings = original.PageSettings,
                Pages = new List<PageData>()
            };

            // Copy pages with only viewable data
            foreach (var page in original.Pages)
            {
                var publishedPage = new PageData
                {
                    PageId = page.PageId,
                    PageName = page.PageName,
                    ParentPageId = page.ParentPageId,
                    PageSettings = page.PageSettings,
                    Objects = new List<ExploderObject>()
                };

                // Copy objects with only necessary properties
                foreach (var obj in page.Objects)
                {
                    var publishedObj = new ExploderObject
                    {
                        ObjectId = obj.ObjectId,
                        ObjectName = obj.ObjectName,
                        ObjectType = obj.ObjectType,
                        Left = obj.Left,
                        Top = obj.Top,
                        Width = obj.Width,
                        Height = obj.Height,
                        FillColor = obj.FillColor,
                        StrokeColor = obj.StrokeColor,
                        StrokeThickness = obj.StrokeThickness,
                        Opacity = obj.Opacity,
                        Text = obj.Text,
                        FontFamily = obj.FontFamily,
                        FontSize = obj.FontSize,
                        FontWeight = obj.FontWeight,
                        X1 = obj.X1,
                        Y1 = obj.Y1,
                        X2 = obj.X2,
                        Y2 = obj.Y2,
                        ImagePath = obj.ImagePath,
                        ImageSource = obj.ImageSource,
                        LinkType = obj.LinkType,
                        LinkTarget = obj.LinkTarget,
                        LinkPageId = obj.LinkPageId,
                        LinkDocumentPath = obj.LinkDocumentPath,
                        LinkUrl = obj.LinkUrl,
                        ExcelRange = obj.ExcelRange,
                        ZIndex = obj.ZIndex
                    };

                    publishedPage.Objects.Add(publishedObj);
                }

                published.Pages.Add(publishedPage);
            }

            return published;
        }

        private async Task CopyProjectAssetsAsync(ProjectData project, string targetDir)
        {
            var assetsDir = Path.Combine(targetDir, "assets");
            Directory.CreateDirectory(assetsDir);

            // Copy referenced images and documents
            foreach (var page in project.Pages)
            {
                foreach (var obj in page.Objects)
                {
                    if (obj.LinkType == LinkType.Document && !string.IsNullOrEmpty(obj.LinkDocumentPath))
                    {
                        if (File.Exists(obj.LinkDocumentPath))
                        {
                            var fileName = Path.GetFileName(obj.LinkDocumentPath);
                            var targetPath = Path.Combine(assetsDir, fileName);
                            File.Copy(obj.LinkDocumentPath, targetPath);
                        }
                    }

                    if (!string.IsNullOrEmpty(obj.ImagePath) && File.Exists(obj.ImagePath))
                    {
                        var fileName = Path.GetFileName(obj.ImagePath);
                        var targetPath = Path.Combine(assetsDir, fileName);
                        File.Copy(obj.ImagePath, targetPath);
                    }
                }
            }
        }

        private string CreateHtmlViewer(ProjectData project)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>{project.ProjectName} - Published</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }}
        .container {{ max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .page {{ margin-bottom: 40px; padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
        .page-title {{ font-size: 24px; font-weight: bold; margin-bottom: 20px; color: #333; }}
        .object {{ position: relative; margin: 10px 0; padding: 10px; border: 1px solid #ccc; border-radius: 3px; }}
        .object-name {{ font-weight: bold; color: #666; }}
        .navigation {{ text-align: center; margin: 20px 0; }}
        .nav-button {{ padding: 10px 20px; margin: 0 10px; background: #007bff; color: white; border: none; border-radius: 3px; cursor: pointer; }}
        .nav-button:hover {{ background: #0056b3; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{project.ProjectName}</h1>
            <p>Published Document - Generated by Exploder</p>
        </div>
        
        <div class='navigation'>
            <button class='nav-button' onclick='showPage(0)'>Main Page</button>
            {string.Join("", project.Pages.Skip(1).Select((page, index) => $"<button class='nav-button' onclick='showPage({index + 1})'>{page.PageName}</button>"))}
        </div>
        
        {string.Join("", project.Pages.Select((page, pageIndex) => $@"
        <div id='page-{pageIndex}' class='page' style='display: {(pageIndex == 0 ? "block" : "none")};'>
            <div class='page-title'>{page.PageName}</div>
            {string.Join("", page.Objects.Select(obj => $@"
            <div class='object' style='left: {obj.Left}px; top: {obj.Top}px; width: {obj.Width}px; height: {obj.Height}px; background-color: {obj.FillColor}; border: {obj.StrokeThickness}px solid {obj.StrokeColor};'>
                <div class='object-name'>{obj.ObjectName}</div>
                {(string.IsNullOrEmpty(obj.Text) ? "" : $"<div>{obj.Text}</div>")}
            </div>"))}
        </div>"))}
    </div>
    
    <script>
        function showPage(pageIndex) {{
            // Hide all pages
            for (let i = 0; i < {project.Pages.Count}; i++) {{
                document.getElementById('page-' + i).style.display = 'none';
            }}
            // Show selected page
            document.getElementById('page-' + pageIndex).style.display = 'block';
        }}
    </script>
</body>
</html>";
        }

        private string CreateBatchLauncher()
        {
            return @"
@echo off
echo Starting Exploder Published Document...
echo.
echo Opening viewer in default browser...
start viewer.html
echo.
echo If the viewer doesn't open automatically, please open 'viewer.html' in your web browser.
pause";
        }
    }
} 