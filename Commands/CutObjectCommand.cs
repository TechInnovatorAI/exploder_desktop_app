using Exploder.Models;

namespace Exploder.Commands
{
    public class CutObjectCommand : ICommand
    {
        private readonly ExploderObject _object;
        private readonly PageData _page;
        private readonly int _originalIndex;
        private ExploderObject? _clipboardObject;

        public CutObjectCommand(ExploderObject obj, PageData page)
        {
            _object = obj;
            _page = page;
            _originalIndex = page.Objects.IndexOf(obj);
        }

        public void Execute()
        {
            // Copy the object to clipboard first
            _clipboardObject = CloneObject(_object);
            
            // Then remove it from the page
            _page.Objects.Remove(_object);
        }

        public void Undo()
        {
            // Restore the object to its original position
            if (_originalIndex >= 0 && _originalIndex <= _page.Objects.Count)
            {
                _page.Objects.Insert(_originalIndex, _object);
            }
            else
            {
                _page.Objects.Add(_object);
            }
            
            // Clear clipboard
            _clipboardObject = null;
        }

        public bool CanExecute()
        {
            return _object != null && _page != null && _page.Objects.Contains(_object);
        }

        public ExploderObject? GetCutObject()
        {
            return _clipboardObject;
        }

        private ExploderObject CloneObject(ExploderObject original)
        {
            return new ExploderObject
            {
                ObjectId = Guid.NewGuid().ToString(),
                ObjectName = original.ObjectName + " (Cut)",
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