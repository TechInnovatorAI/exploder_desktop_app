using Exploder.Models;

namespace Exploder.Commands
{
    public class AddObjectCommand : ICommand
    {
        private readonly ExploderObject _object;
        private readonly PageData _page;

        public AddObjectCommand(ExploderObject obj, PageData page)
        {
            _object = obj;
            _page = page;
        }

        public void Execute()
        {
            _page.Objects.Add(_object);
        }

        public void Undo()
        {
            _page.Objects.Remove(_object);
        }

        public bool CanExecute()
        {
            return _object != null && _page != null;
        }
    }
} 