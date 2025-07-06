public class ProjectData
{
    public List<ShapeData> Shapes { get; set; } = new List<ShapeData>();
}

public class ShapeData
{
    public string Type { get; set; } = "";

    // Common properties
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    // For Line
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }

    // Colors (stored as string)
    public string FillColor { get; set; } = "";
    public string StrokeColor { get; set; } = "";

    // For images
    public string ImagePath { get; set; } = "";
}
