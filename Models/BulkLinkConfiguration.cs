using System.Text.Json.Serialization;

namespace Exploder.Models
{
    /// <summary>
    /// Represents a bulk link configuration for importing/exporting multiple link settings
    /// </summary>
    public class BulkLinkConfiguration
    {
        /// <summary>
        /// Name of the project these links belong to
        /// </summary>
        public string ProjectName { get; set; } = "";

        /// <summary>
        /// Name of the page these links belong to
        /// </summary>
        public string PageName { get; set; } = "";

        /// <summary>
        /// When the bulk configuration was exported
        /// </summary>
        public DateTime ExportedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Version of the bulk configuration format
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Description of the bulk configuration
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// List of individual link configurations
        /// </summary>
        public List<LinkConfiguration> LinkConfigurations { get; set; } = new List<LinkConfiguration>();

        /// <summary>
        /// Summary statistics about the bulk configuration
        /// </summary>
        public BulkConfigurationStats Stats { get; set; } = new BulkConfigurationStats();
    }

    /// <summary>
    /// Statistics about the bulk link configuration
    /// </summary>
    public class BulkConfigurationStats
    {
        /// <summary>
        /// Total number of link configurations
        /// </summary>
        public int TotalLinks { get; set; } = 0;

        /// <summary>
        /// Number of page navigation links
        /// </summary>
        public int PageLinks { get; set; } = 0;

        /// <summary>
        /// Number of document links
        /// </summary>
        public int DocumentLinks { get; set; } = 0;

        /// <summary>
        /// Number of URL links
        /// </summary>
        public int UrlLinks { get; set; } = 0;

        /// <summary>
        /// Number of Excel data links
        /// </summary>
        public int ExcelLinks { get; set; } = 0;

        /// <summary>
        /// Number of different object types
        /// </summary>
        public int ObjectTypes { get; set; } = 0;

        /// <summary>
        /// List of unique object types
        /// </summary>
        public List<string> UniqueObjectTypes { get; set; } = new List<string>();
    }
} 