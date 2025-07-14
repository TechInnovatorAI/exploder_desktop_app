using Exploder.Models;

namespace Exploder.Services
{
    public interface IPublishingService
    {
        Task<bool> PublishProjectAsync(ProjectData project, string outputPath);
        Task<string> CreateSelfExecutingPackageAsync(ProjectData project, string outputPath);
        Task<bool> ValidateProjectForPublishingAsync(ProjectData project);
    }
} 