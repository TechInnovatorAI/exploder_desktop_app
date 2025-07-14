using Exploder.Models;
using System.Text.Json;
using System.IO;

namespace Exploder.Services
{
    public class ProjectService : IProjectService
    {
        private readonly string _recentProjectsFile;

        public ProjectService()
        {
            _recentProjectsFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Exploder", "recent_projects.txt");
        }

        public async Task<ProjectData?> LoadProjectAsync(string filePath)
        {
            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<ProjectData>(json);
            }
            catch
            {
                return null;
            }
        }

        public async Task SaveProjectAsync(ProjectData project, string filePath)
        {
            project.Sanitize();
            var json = JsonSerializer.Serialize(project, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<List<string>> GetRecentProjectsAsync()
        {
            try
            {
                if (File.Exists(_recentProjectsFile))
                {
                    var lines = await File.ReadAllLinesAsync(_recentProjectsFile);
                    return lines.Where(line => !string.IsNullOrWhiteSpace(line) && File.Exists(line))
                              .Take(10)
                              .ToList();
                }
            }
            catch
            {
                // Silently fail - this is not critical
            }
            return new List<string>();
        }

        public async Task SaveToRecentProjectsAsync(string projectPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(_recentProjectsFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var recentProjects = await GetRecentProjectsAsync();
                recentProjects.Remove(projectPath);
                recentProjects.Insert(0, projectPath);
                recentProjects = recentProjects.Take(10).ToList();

                await File.WriteAllLinesAsync(_recentProjectsFile, recentProjects);
            }
            catch
            {
                // Silently fail - this is not critical
            }
        }

        public async Task<ProjectData> CreateNewProjectAsync(string projectName, string projectPath, PageSettings settings)
        {
            var project = new ProjectData
            {
                ProjectName = projectName,
                ProjectPath = projectPath,
                PageSettings = settings,
                Pages = new List<PageData>
                {
                    new PageData
                    {
                        PageId = Guid.NewGuid().ToString(),
                        PageName = "Main Page",
                        PageSettings = settings.Clone(),
                        Objects = new List<ExploderObject>()
                    }
                }
            };

            return project;
        }
    }
} 