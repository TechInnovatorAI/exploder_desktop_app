namespace Exploder.Commands
{
    public interface ICommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
    }
} 