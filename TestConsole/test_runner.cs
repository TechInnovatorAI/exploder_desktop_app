using System;
using System.IO;
using System.Text.Json;
using Exploder.Models;
using Exploder.Services;
using System.Collections.Generic; // Added for List<T>

namespace Exploder.Tests
{
    public class TestRunner
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Exploder Application Tests ===\n");

            TestProjectService();
            TestPublishingService();
            TestTemplateService();
            TestDataModels();
            TestFileOperations();

            Console.WriteLine("\n=== All Tests Completed ===");
        }

        private static void TestProjectService()
        {
            Console.WriteLine("Testing Project Service...");
            
            try
            {
                var service = new ProjectService();
                
                // Test project creation
                var settings = new PageSettings
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    MarginSize = 20.0,
                    BackgroundColor = "#FFFFFF"
                };

                var project = service.CreateNewProjectAsync("Test Project", "C:\\Test", settings).Result;
                
                if (project != null && project.ProjectName == "Test Project")
                {
                    Console.WriteLine("✓ Project creation test passed");
                }
                else
                {
                    Console.WriteLine("✗ Project creation test failed");
                }

                // Test project serialization
                var json = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
                var deserializedProject = JsonSerializer.Deserialize<ProjectData>(json);
                
                if (deserializedProject?.ProjectName == project.ProjectName)
                {
                    Console.WriteLine("✓ Project serialization test passed");
                }
                else
                {
                    Console.WriteLine("✗ Project serialization test failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Project service test failed: {ex.Message}");
            }
        }

        private static void TestPublishingService()
        {
            Console.WriteLine("\nTesting Publishing Service...");
            
            try
            {
                var service = new PublishingService();
                
                // Create a test project
                var project = new ProjectData
                {
                    ProjectName = "Test Publishing Project",
                    ProjectPath = "C:\\Test",
                    PageSettings = new PageSettings(),
                    Pages = new List<PageData>
                    {
                        new PageData
                        {
                            PageId = Guid.NewGuid().ToString(),
                            PageName = "Test Page",
                            Objects = new List<ExploderObject>
                            {
                                new ExploderObject
                                {
                                    ObjectId = Guid.NewGuid().ToString(),
                                    ObjectName = "Test Object",
                                    ObjectType = "Rectangle",
                                    Left = 100,
                                    Top = 100,
                                    Width = 50,
                                    Height = 50
                                }
                            }
                        }
                    }
                };

                // Test project validation
                var isValid = service.ValidateProjectForPublishingAsync(project).Result;
                
                if (isValid)
                {
                    Console.WriteLine("✓ Project validation test passed");
                }
                else
                {
                    Console.WriteLine("✗ Project validation test failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Publishing service test failed: {ex.Message}");
            }
        }

        private static void TestTemplateService()
        {
            Console.WriteLine("\nTesting Template Service...");
            
            try
            {
                var service = new TemplateService();
                
                // Test template creation
                var project = new ProjectData
                {
                    ProjectName = "Test Template",
                    ProjectPath = "",
                    PageSettings = new PageSettings(),
                    Pages = new List<PageData>()
                };

                var success = service.SaveProjectAsTemplateAsync(project, "Test Template", "A test template").Result;
                
                if (success)
                {
                    Console.WriteLine("✓ Template creation test passed");
                }
                else
                {
                    Console.WriteLine("✗ Template creation test failed");
                }

                // Test template retrieval
                var templates = service.GetAvailableTemplatesAsync().Result;
                
                if (templates != null)
                {
                    Console.WriteLine($"✓ Template retrieval test passed ({templates.Count} templates found)");
                }
                else
                {
                    Console.WriteLine("✗ Template retrieval test failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Template service test failed: {ex.Message}");
            }
        }

        private static void TestDataModels()
        {
            Console.WriteLine("\nTesting Data Models...");
            
            try
            {
                // Test ExploderObject creation
                var obj = new ExploderObject
                {
                    ObjectId = Guid.NewGuid().ToString(),
                    ObjectName = "Test Object",
                    ObjectType = "Rectangle",
                    Left = 100,
                    Top = 100,
                    Width = 50,
                    Height = 50,
                    FillColor = "#FF0000",
                    StrokeColor = "#000000",
                    StrokeThickness = 2.0,
                    Text = "Test Text",
                    FontFamily = "Arial",
                    FontSize = 12.0,
                    LinkType = LinkType.None
                };

                if (obj.ObjectName == "Test Object" && obj.ObjectType == "Rectangle")
                {
                    Console.WriteLine("✓ ExploderObject creation test passed");
                }
                else
                {
                    Console.WriteLine("✗ ExploderObject creation test failed");
                }

                // Test PageData creation
                var page = new PageData
                {
                    PageId = Guid.NewGuid().ToString(),
                    PageName = "Test Page",
                    Objects = new List<ExploderObject> { obj }
                };

                if (page.PageName == "Test Page" && page.Objects.Count == 1)
                {
                    Console.WriteLine("✓ PageData creation test passed");
                }
                else
                {
                    Console.WriteLine("✗ PageData creation test failed");
                }

                // Test PageSettings cloning
                var settings = new PageSettings
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    MarginSize = 20.0,
                    BackgroundColor = "#FFFFFF"
                };

                var clonedSettings = settings.Clone();
                
                if (clonedSettings.PageSize == settings.PageSize && 
                    clonedSettings.Orientation == settings.Orientation)
                {
                    Console.WriteLine("✓ PageSettings cloning test passed");
                }
                else
                {
                    Console.WriteLine("✗ PageSettings cloning test failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Data models test failed: {ex.Message}");
            }
        }

        private static void TestFileOperations()
        {
            Console.WriteLine("\nTesting File Operations...");
            
            try
            {
                // Test JSON serialization/deserialization
                var project = new ProjectData
                {
                    ProjectName = "File Test Project",
                    ProjectPath = "C:\\Test",
                    PageSettings = new PageSettings(),
                    Pages = new List<PageData>()
                };

                var json = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
                
                if (!string.IsNullOrEmpty(json) && json.Contains("File Test Project"))
                {
                    Console.WriteLine("✓ JSON serialization test passed");
                }
                else
                {
                    Console.WriteLine("✗ JSON serialization test failed");
                }

                var deserializedProject = JsonSerializer.Deserialize<ProjectData>(json);
                
                if (deserializedProject?.ProjectName == project.ProjectName)
                {
                    Console.WriteLine("✓ JSON deserialization test passed");
                }
                else
                {
                    Console.WriteLine("✗ JSON deserialization test failed");
                }

                // Test file writing/reading (in temp directory)
                var tempFile = Path.Combine(Path.GetTempPath(), "exploder_test.exp");
                File.WriteAllText(tempFile, json);
                
                if (File.Exists(tempFile))
                {
                    Console.WriteLine("✓ File writing test passed");
                    
                    var readJson = File.ReadAllText(tempFile);
                    var readProject = JsonSerializer.Deserialize<ProjectData>(readJson);
                    
                    if (readProject?.ProjectName == project.ProjectName)
                    {
                        Console.WriteLine("✓ File reading test passed");
                    }
                    else
                    {
                        Console.WriteLine("✗ File reading test failed");
                    }
                    
                    // Clean up
                    File.Delete(tempFile);
                }
                else
                {
                    Console.WriteLine("✗ File writing test failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ File operations test failed: {ex.Message}");
            }
        }
    }
} 