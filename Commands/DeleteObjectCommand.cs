using Exploder.Models;

namespace Exploder.Commands
{
    public class DeleteObjectCommand : ICommand
    {
        private readonly ExploderObject _object;
        private readonly PageData _page;
        private readonly int _originalIndex;

        public DeleteObjectCommand(ExploderObject obj, PageData page)
        {
            _object = obj;
            _page = page;
            _originalIndex = page.Objects.IndexOf(obj);
        }

        public void Execute()
        {
            _page.Objects.Remove(_object);
        }

        public void Undo()
        {
            if (_originalIndex >= 0 && _originalIndex <= _page.Objects.Count)
            {
                _page.Objects.Insert(_originalIndex, _object);
            }
            else
            {
                _page.Objects.Add(_object);
            }
        }

        public bool CanExecute()
        {
            return _object != null && _page != null && _page.Objects.Contains(_object);
        }
    }
} 