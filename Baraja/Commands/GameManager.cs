namespace Baraja.Commands;

// Gestor central para la UI o la API
public class GameManager
{
    private readonly Stack<IMoveCommand> _history = new();
    public int MoveCount => _history.Count;
    public Result<bool> TryExecuteMove(IMoveCommand command)
    {
        var result = command.Execute();
        if (result.IsSuccess)
        {
            _history.Push(command);
        }
        return result;
    }

    public void UndoLastMove()
    {
        if (_history.TryPop(out var lastCommand))
        {
            lastCommand.Undo();
        }
    }
}
