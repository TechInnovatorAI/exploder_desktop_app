using System.Collections.Generic;

namespace Exploder.Models
{
    public class ProjectData
    {
        public string ProjectName { get; set; } = "";
        public string ProjectPath { get; set; } = "";
        public PageSettings PageSettings { get; set; } = new PageSettings();
        public List<PageData> Pages { get; set; } = new List<PageData>();
        public List<string> RecentProjects { get; set; } = new List<string>();

        // Sanitizer: Ensures all double values are finite
        public void Sanitize()
        {
            PageSettings.Sanitize();
            foreach (var page in Pages)
            {
                page.Sanitize();
            }
        }
    }

    public class PageSettings
    {
        public string PageSize { get; set; } = "A4";
        public string Orientation { get; set; } = "Portrait";
        public double MarginSize { get; set; } = 20.0;
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public bool ShowGrid { get; set; } = false;
        public bool ShowRulers { get; set; } = true;
        public double Width { get; set; } = 210.0; // mm
        public double Height { get; set; } = 297.0; // mm

        public PageSettings Clone()
        {
            return new PageSettings
            {
                PageSize = this.PageSize,
                Orientation = this.Orientation,
                MarginSize = this.MarginSize,
                BackgroundColor = this.BackgroundColor,
                ShowGrid = this.ShowGrid,
                ShowRulers = this.ShowRulers,
                Width = this.Width,
                Height = this.Height
            };
        }

        public void Sanitize()
        {
            if (double.IsNaN(MarginSize) || double.IsInfinity(MarginSize)) MarginSize = 0;
            if (double.IsNaN(Width) || double.IsInfinity(Width)) Width = 0;
            if (double.IsNaN(Height) || double.IsInfinity(Height)) Height = 0;
        }
    }

    public class PageData
    {
        public string PageId { get; set; } = "";
        public string PageName { get; set; } = "";
        public string ParentPageId { get; set; } = "";
        public List<ExploderObject> Objects { get; set; } = new List<ExploderObject>();
        public PageSettings PageSettings { get; set; } = new PageSettings();

        public void Sanitize()
        {
            PageSettings.Sanitize();
            foreach (var obj in Objects)
            {
                obj.Sanitize();
            }
        }
    }

    public class ExploderObject
    {
        public string ObjectId { get; set; } = "";
        public string ObjectName { get; set; } = "";
        public string ObjectType { get; set; } = "";
        // Position and size
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        // Visual properties
        public string FillColor { get; set; } = "#FFFFFF";
        public string StrokeColor { get; set; } = "#000000";
        public double StrokeThickness { get; set; } = 1.0;
        public double Opacity { get; set; } = 1.0;
        // Text properties
        public string Text { get; set; } = "";
        public string FontFamily { get; set; } = "Arial";
        public double FontSize { get; set; } = 12.0;
        public string FontWeight { get; set; } = "Normal";
        public string TextColor { get; set; } = "#000000";
        // Line properties
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        // Image properties
        public string ImagePath { get; set; } = "";
        public string ImageSource { get; set; } = ""; // Relative or absolute path
        // Link properties
        public LinkType LinkType { get; set; } = LinkType.None;
        public LinkFileType LinkFileType { get; set; } = LinkFileType.None;
        public string LinkTarget { get; set; } = "";
        public string LinkPageId { get; set; } = "";
        public string LinkDocumentPath { get; set; } = "";
        public string LinkUrl { get; set; } = "";
        public ExcelRange? ExcelRange { get; set; }
        // Z-order
        public int ZIndex { get; set; } = 0;
        // Group properties
        public string GroupId { get; set; } = "";
        public bool IsGrouped { get; set; } = false;

        public void Sanitize()
        {
            if (double.IsNaN(Left) || double.IsInfinity(Left)) Left = 0;
            if (double.IsNaN(Top) || double.IsInfinity(Top)) Top = 0;
            if (double.IsNaN(Width) || double.IsInfinity(Width)) Width = 0;
            if (double.IsNaN(Height) || double.IsInfinity(Height)) Height = 0;
            if (double.IsNaN(StrokeThickness) || double.IsInfinity(StrokeThickness)) StrokeThickness = 1.0;
            if (double.IsNaN(Opacity) || double.IsInfinity(Opacity)) Opacity = 1.0;
            if (double.IsNaN(FontSize) || double.IsInfinity(FontSize)) FontSize = 12.0;
            if (double.IsNaN(X1) || double.IsInfinity(X1)) X1 = 0;
            if (double.IsNaN(Y1) || double.IsInfinity(Y1)) Y1 = 0;
            if (double.IsNaN(X2) || double.IsInfinity(X2)) X2 = 0;
            if (double.IsNaN(Y2) || double.IsInfinity(Y2)) Y2 = 0;
        }
    }

    public enum LinkType
    {
        None,
        NewPage,
        Document,
        Url,
        ExcelData
    }

    public enum LinkFileType
    {
        None,
        Video,
        PDF,
        Excel,
        Word
    }

    public class ExcelRange
    {
        public string FilePath { get; set; } = "";
        public string SheetName { get; set; } = "";
        public string CellRange { get; set; } = ""; // e.g., "A1:B10"
        public bool IsRelative { get; set; } = true;
    }
}
