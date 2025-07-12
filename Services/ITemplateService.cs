using Exploder.Models;

namespace Exploder.Services
{
    public interface ITemplateService
    {
        Task<List<TemplateInfo>> GetAvailableTemplatesAsync();
        Task<ProjectData?> CreateProjectFromTemplateAsync(string templateName);
        Task<bool> SaveProjectAsTemplateAsync(ProjectData project, string templateName, string description);
        Task<bool> DeleteTemplateAsync(string templateName);
    }

    public class TemplateInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public string FilePath { get; set; } = "";
    }
} 