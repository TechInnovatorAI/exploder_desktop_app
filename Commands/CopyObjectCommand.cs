using Exploder.Models;

namespace Exploder.Commands
{
    public class CopyObjectCommand : ICommand
    {
        private readonly ExploderObject _object;

        public CopyObjectCommand(ExploderObject obj)
        {
            _object = obj;
        }

        public void Execute()
        {
            // Copy operation doesn't modify the original object
            // The clipboard object is managed separately
        }

        public void Undo()
        {
            // Copy operation doesn't need undo since it doesn't modify anything
        }

        public bool CanExecute()
        {
            return _object != null;
        }

        public ExploderObject GetCopiedObject()
        {
            return CloneObject(_object);
        }

        private ExploderObject CloneObject(ExploderObject original)
        {
            return new ExploderObject
            {
                ObjectId = Guid.NewGuid().ToString(),
                ObjectName = original.ObjectName + " (Copy)",
                ObjectType = original.ObjectType,
                Left = original.Left,
                Top = original.Top,
                Width = original.Width,
                Height = original.Height,
                FillColor = original.FillColor,
                StrokeColor = original.StrokeColor,
                StrokeThickness = original.StrokeThickness,
                Opacity = original.Opacity,
                Text = original.Text,
                FontFamily = original.FontFamily,
                FontSize = original.FontSize,
                FontWeight = original.FontWeight,
                TextColor = original.TextColor,
                X1 = original.X1,
                Y1 = original.Y1,
                X2 = original.X2,
                Y2 = original.Y2,
                ImagePath = original.ImagePath,
                ImageSource = original.ImageSource,
                LinkType = original.LinkType,
                LinkTarget = original.LinkTarget,
                LinkPageId = original.LinkPageId,
                LinkDocumentPath = original.LinkDocumentPath,
                LinkUrl = original.LinkUrl,
                ExcelRange = original.ExcelRange,
                ZIndex = original.ZIndex,
                GroupId = original.GroupId,
                IsGrouped = original.IsGrouped
            };
        }
    }
} 