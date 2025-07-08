namespace Exploder.Setting
{
    public class SerializedElement
    {
        public string Type { get; set; } = "";
        public double Width { get; set; }
        public double Height { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }

        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }

        public string ImagePath { get; set; } = "";
    }
}
