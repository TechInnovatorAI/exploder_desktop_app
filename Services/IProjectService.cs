using Exploder.Models;

namespace Exploder.Services
{
    public interface IProjectService
    {
        Task<ProjectData?> LoadProjectAsync(string filePath);
        Task SaveProjectAsync(ProjectData project, string filePath);
        Task<List<string>> GetRecentProjectsAsync();
        Task SaveToRecentProjectsAsync(string projectPath);
        Task<ProjectData> CreateNewProjectAsync(string projectName, string projectPath, PageSettings settings);
    }
} 