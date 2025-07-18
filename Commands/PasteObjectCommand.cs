using Exploder.Models;

namespace Exploder.Commands
{
    public class PasteObjectCommand : ICommand
    {
        private readonly ExploderObject _objectToPaste;
        private readonly PageData _page;
        private ExploderObject? _pastedObject;

        public PasteObjectCommand(ExploderObject objectToPaste, PageData page)
        {
            _objectToPaste = objectToPaste;
            _page = page;
        }

        public void Execute()
        {
            _pastedObject = CloneObject(_objectToPaste);
            _pastedObject.ObjectId = Guid.NewGuid().ToString();
            _pastedObject.Left += 20; // Offset slightly from original
            _pastedObject.Top += 20;
            
            _page.Objects.Add(_pastedObject);
        }

        public void Undo()
        {
            if (_pastedObject != null)
            {
                _page.Objects.Remove(_pastedObject);
            }
        }

        public bool CanExecute()
        {
            return _objectToPaste != null && _page != null;
        }

        public ExploderObject? GetPastedObject()
        {
            return _pastedObject;
        }

        private ExploderObject CloneObject(ExploderObject original)
        {
            return new ExploderObject
            {
                ObjectId = Guid.NewGuid().ToString(),
                ObjectName = original.ObjectName,
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