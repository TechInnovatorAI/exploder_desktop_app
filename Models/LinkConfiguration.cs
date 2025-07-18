using System.Text.Json.Serialization;

namespace Exploder.Models
{
    /// <summary>
    /// Represents a link configuration that can be imported/exported
    /// </summary>
    public class LinkConfiguration
    {
        /// <summary>
        /// Type of link (None, NewPage, Document, Url, ExcelData)
        /// </summary>
        public LinkType LinkType { get; set; } = LinkType.None;

        /// <summary>
        /// Type of document file (Video, PDF, Excel, Word)
        /// </summary>
        public LinkFileType LinkFileType { get; set; } = LinkFileType.None;

        /// <summary>
        /// Target page ID for page navigation
        /// </summary>
        public string LinkPageId { get; set; } = "";

        /// <summary>
        /// Path to document file
        /// </summary>
        public string LinkDocumentPath { get; set; } = "";

        /// <summary>
        /// URL for web links
        /// </summary>
        public string LinkUrl { get; set; } = "";

        /// <summary>
        /// Display text for the link
        /// </summary>
        public string LinkText { get; set; } = "";

        /// <summary>
        /// Excel range configuration for Excel data links
        /// </summary>
        public ExcelRange? ExcelRange { get; set; }

        /// <summary>
        /// Metadata about the exported configuration
        /// </summary>
        public LinkConfigurationMetadata Metadata { get; set; } = new LinkConfigurationMetadata();
    }

    /// <summary>
    /// Metadata about the link configuration export
    /// </summary>
    public class LinkConfigurationMetadata
    {
        /// <summary>
        /// When the configuration was exported
        /// </summary>
        public DateTime ExportedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Version of the configuration format
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Description of the link configuration
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Name of the object this configuration was exported from
        /// </summary>
        public string ObjectName { get; set; } = "";

        /// <summary>
        /// Type of the object this configuration was exported from
        /// </summary>
        public string ObjectType { get; set; } = "";
    }
} 