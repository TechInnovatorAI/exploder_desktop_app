using Exploder.Models;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace Exploder.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly string _templatesDirectory;

        public TemplateService()
        {
            _templatesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Exploder", "Templates");
            
            if (!Directory.Exists(_templatesDirectory))
            {
                Directory.CreateDirectory(_templatesDirectory);
            }
        }

        public async Task<List<TemplateInfo>> GetAvailableTemplatesAsync()
        {
            var templates = new List<TemplateInfo>();

            try
            {
                var templateFiles = Directory.GetFiles(_templatesDirectory, "*.template");
                
                foreach (var file in templateFiles)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var templateInfo = JsonSerializer.Deserialize<TemplateInfo>(json);
                        
                        if (templateInfo != null)
                        {
                            templateInfo.FilePath = file;
                            templates.Add(templateInfo);
                        }
                    }
                    catch
                    {
                        // Skip invalid template files
                        continue;
                    }
                }
            }
            catch
            {
                // Return empty list if directory doesn't exist or other error
            }

            return templates.OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();
        }

        public async Task<ProjectData?> CreateProjectFromTemplateAsync(string templateName)
        {
            try
            {
                var templates = await GetAvailableTemplatesAsync();
                var template = templates.FirstOrDefault(t => t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
                
                if (template == null)
                {
                    return null;
                }

                var projectJson = await File.ReadAllTextAsync(template.FilePath);
                var project = JsonSerializer.Deserialize<ProjectData>(projectJson);
                
                if (project != null)
                {
                    // Generate new IDs for the project and pages
                    project.ProjectName = $"{project.ProjectName} (Copy)";
                    project.ProjectPath = "";
                    
                    foreach (var page in project.Pages)
                    {
                        page.PageId = Guid.NewGuid().ToString();
                        foreach (var obj in page.Objects)
                        {
                            obj.ObjectId = Guid.NewGuid().ToString();
                        }
                    }
                }

                return project;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SaveProjectAsTemplateAsync(ProjectData project, string templateName, string description)
        {
            try
            {
                var templateInfo = new TemplateInfo
                {
                    Name = templateName,
                    Description = description,
                    Category = "Custom",
                    CreatedDate = DateTime.Now,
                    FilePath = Path.Combine(_templatesDirectory, $"{templateName}.template")
                };

                var json = JsonSerializer.Serialize(project, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                await File.WriteAllTextAsync(templateInfo.FilePath, json);

                // Save template metadata
                var metadataJson = JsonSerializer.Serialize(templateInfo, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                var metadataPath = Path.Combine(_templatesDirectory, $"{templateName}.meta");
                await File.WriteAllTextAsync(metadataPath, metadataJson);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteTemplateAsync(string templateName)
        {
            try
            {
                var templates = await GetAvailableTemplatesAsync();
                var template = templates.FirstOrDefault(t => t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
                
                if (template == null)
                {
                    return false;
                }

                if (File.Exists(template.FilePath))
                {
                    File.Delete(template.FilePath);
                }

                var metadataPath = Path.Combine(_templatesDirectory, $"{templateName}.meta");
                if (File.Exists(metadataPath))
                {
                    File.Delete(metadataPath);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task CreateDefaultTemplatesAsync()
        {
            // Create a basic machine documentation template
            var machineTemplate = CreateMachineDocumentationTemplate();
            await SaveProjectAsTemplateAsync(machineTemplate, "Machine Documentation", 
                "Template for documenting machinery with multiple components");

            // Create a basic organizational chart template
            var orgChartTemplate = CreateOrgChartTemplate();
            await SaveProjectAsTemplateAsync(orgChartTemplate, "Organizational Chart", 
                "Template for creating organizational charts");

            // Create a basic business documentation template
            var businessTemplate = CreateBusinessDocumentationTemplate();
            await SaveProjectAsTemplateAsync(businessTemplate, "Business Documentation", 
                "Template for business process documentation");
        }

        private ProjectData CreateMachineDocumentationTemplate()
        {
            var project = new ProjectData
            {
                ProjectName = "Machine Documentation Template",
                ProjectPath = "",
                PageSettings = new PageSettings
                {
                    PageSize = "A4",
                    Orientation = "Landscape",
                    MarginSize = 20.0,
                    BackgroundColor = "#F5F5F5",
                    ShowGrid = true,
                    ShowRulers = true,
                    Width = 297.0,
                    Height = 210.0
                },
                Pages = new List<PageData>
                {
                    new PageData
                    {
                        PageId = Guid.NewGuid().ToString(),
                        PageName = "Main Overview",
                        PageSettings = new PageSettings
                        {
                            PageSize = "A4",
                            Orientation = "Landscape",
                            MarginSize = 20.0,
                            BackgroundColor = "#F5F5F5",
                            ShowGrid = true,
                            ShowRulers = true,
                            Width = 297.0,
                            Height = 210.0
                        },
                        Objects = new List<ExploderObject>
                        {
                            new ExploderObject
                            {
                                ObjectId = Guid.NewGuid().ToString(),
                                ObjectName = "Engine",
                                ObjectType = "Rectangle",
                                Left = 50,
                                Top = 50,
                                Width = 80,
                                Height = 60,
                                FillColor = "#FFE6E6",
                                StrokeColor = "#CC0000",
                                StrokeThickness = 2,
                                Text = "Engine",
                                FontFamily = "Arial",
                                FontSize = 12,
                                LinkType = LinkType.NewPage,
                                LinkTarget = "Engine Details"
                            },
                            new ExploderObject
                            {
                                ObjectId = Guid.NewGuid().ToString(),
                                ObjectName = "Transmission",
                                ObjectType = "Rectangle",
                                Left = 200,
                                Top = 50,
                                Width = 80,
                                Height = 60,
                                FillColor = "#E6F3FF",
                                StrokeColor = "#0066CC",
                                StrokeThickness = 2,
                                Text = "Transmission",
                                FontFamily = "Arial",
                                FontSize = 12,
                                LinkType = LinkType.NewPage,
                                LinkTarget = "Transmission Details"
                            },
                            new ExploderObject
                            {
                                ObjectId = Guid.NewGuid().ToString(),
                                ObjectName = "Hydraulics",
                                ObjectType = "Rectangle",
                                Left = 350,
                                Top = 50,
                                Width = 80,
                                Height = 60,
                                FillColor = "#E6FFE6",
                                StrokeColor = "#00CC00",
                                StrokeThickness = 2,
                                Text = "Hydraulics",
                                FontFamily = "Arial",
                                FontSize = 12,
                                LinkType = LinkType.NewPage,
                                LinkTarget = "Hydraulics Details"
                            }
                        }
                    }
                }
            };

            return project;
        }

        private ProjectData CreateOrgChartTemplate()
        {
            var project = new ProjectData
            {
                ProjectName = "Organizational Chart Template",
                ProjectPath = "",
                PageSettings = new PageSettings
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    MarginSize = 20.0,
                    BackgroundColor = "#FFFFFF",
                    ShowGrid = false,
                    ShowRulers = true,
                    Width = 210.0,
                    Height = 297.0
                },
                Pages = new List<PageData>
                {
                    new PageData
                    {
                        PageId = Guid.NewGuid().ToString(),
                        PageName = "Organization Overview",
                        PageSettings = new PageSettings
                        {
                            PageSize = "A4",
                            Orientation = "Portrait",
                            MarginSize = 20.0,
                            BackgroundColor = "#FFFFFF",
                            ShowGrid = false,
                            ShowRulers = true,
                            Width = 210.0,
                            Height = 297.0
                        },
                        Objects = new List<ExploderObject>
                        {
                            new ExploderObject
                            {
                                ObjectId = Guid.NewGuid().ToString(),
                                ObjectName = "CEO",
                                ObjectType = "RoundedRectangle",
                                Left = 80,
                                Top = 30,
                                Width = 60,
                                Height = 40,
                                FillColor = "#FFE6CC",
                                StrokeColor = "#FF6600",
                                StrokeThickness = 2,
                                Text = "CEO",
                                FontFamily = "Arial",
                                FontSize = 12,
                                LinkType = LinkType.NewPage,
                                LinkTarget = "CEO Details"
                            },
                            new ExploderObject
                            {
                                ObjectId = Guid.NewGuid().ToString(),
                                ObjectName = "CTO",
                                ObjectType = "RoundedRectangle",
                                Left = 30,
                                Top = 100,
                                Width = 60,
                                Height = 40,
                                FillColor = "#E6CCFF",
                                StrokeColor = "#6600CC",
                                StrokeThickness = 2,
                                Text = "CTO",
                                FontFamily = "Arial",
                                FontSize = 12,
                                LinkType = LinkType.NewPage,
                                LinkTarget = "CTO Details"
                            },
                            new ExploderObject
                            {
                                ObjectId = Guid.NewGuid().ToString(),
                                ObjectName = "CFO",
                                ObjectType = "RoundedRectangle",
                                Left = 130,
                                Top = 100,
                                Width = 60,
                                Height = 40,
                                FillColor = "#CCE6FF",
                                StrokeColor = "#0066CC",
                                StrokeThickness = 2,
                                Text = "CFO",
                                FontFamily = "Arial",
                                FontSize = 12,
                                LinkType = LinkType.NewPage,
                                LinkTarget = "CFO Details"
                            }
                        }
                    }
                }
            };

            return project;
        }

        private ProjectData CreateBusinessDocumentationTemplate()
        {
            var project = new ProjectData
            {
                ProjectName = "Business Documentation Template",
                ProjectPath = "",
                PageSettings = new PageSettings
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    MarginSize = 20.0,
                    BackgroundColor = "#FFFFFF",
                    ShowGrid = true,
                    ShowRulers = true,
                    Width = 210.0,
                    Height = 297.0
                },
                Pages = new List<PageData>
                {
                    new PageData
                    {
                        PageId = Guid.NewGuid().ToString(),
                        PageName = "Business Overview",
                        PageSettings = new PageSettings
                        {
                            PageSize = "A4",
                            Orientation = "Portrait",
                            MarginSize = 20.0,
                            BackgroundColor = "#FFFFFF",
                            ShowGrid = true,
                            ShowRulers = true,
                            Width = 210.0,
                            Height = 297.0
                        },
                        Objects = new List<ExploderObject>
                        {
                            new ExploderObject
                            {
                                ObjectId = Guid.NewGuid().ToString(),
                                ObjectName = "Process Flow",
                                ObjectType = "Rectangle",
                                Left = 30,
                                Top = 50,
                                Width = 80,
                                Height = 60,
                                FillColor = "#F0F8FF",
                                StrokeColor = "#4169E1",
                                StrokeThickness = 2,
                                Text = "Process Flow",
                                FontFamily = "Arial",
                                FontSize = 12,
                                LinkType = LinkType.NewPage,
                                LinkTarget = "Process Flow Details"
                            },
                            new ExploderObject
                            {
                                ObjectId = Guid.NewGuid().ToString(),
                                ObjectName = "Policies",
                                ObjectType = "Rectangle",
                                Left = 130,
                                Top = 50,
                                Width = 80,
                                Height = 60,
                                FillColor = "#FFF8DC",
                                StrokeColor = "#DAA520",
                                StrokeThickness = 2,
                                Text = "Policies",
                                FontFamily = "Arial",
                                FontSize = 12,
                                LinkType = LinkType.NewPage,
                                LinkTarget = "Policies Details"
                            },
                            new ExploderObject
                            {
                                ObjectId = Guid.NewGuid().ToString(),
                                ObjectName = "Procedures",
                                ObjectType = "Rectangle",
                                Left = 30,
                                Top = 130,
                                Width = 80,
                                Height = 60,
                                FillColor = "#F0FFF0",
                                StrokeColor = "#228B22",
                                StrokeThickness = 2,
                                Text = "Procedures",
                                FontFamily = "Arial",
                                FontSize = 12,
                                LinkType = LinkType.NewPage,
                                LinkTarget = "Procedures Details"
                            },
                            new ExploderObject
                            {
                                ObjectId = Guid.NewGuid().ToString(),
                                ObjectName = "Forms",
                                ObjectType = "Rectangle",
                                Left = 130,
                                Top = 130,
                                Width = 80,
                                Height = 60,
                                FillColor = "#FFF0F5",
                                StrokeColor = "#DC143C",
                                StrokeThickness = 2,
                                Text = "Forms",
                                FontFamily = "Arial",
                                FontSize = 12,
                                LinkType = LinkType.NewPage,
                                LinkTarget = "Forms Details"
                            }
                        }
                    }
                }
            };

            return project;
        }
    }
} 