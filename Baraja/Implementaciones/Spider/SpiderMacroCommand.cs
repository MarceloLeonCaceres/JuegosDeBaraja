using Baraja.Commands;

namespace Baraja.Implementaciones.Spider;

public class SpiderMacroCommand : IMoveCommand
{
    private readonly List<IMoveCommand> _commands = new();

    public void AddCommand(IMoveCommand command)
    {
        _commands.Add(command);
    }

    public Result<bool> Execute()
    {
        // Retornamos éxito porque ejecutaremos los sub-comandos individualmente 
        // desde la UI para poder intercalar las animaciones visuales.
        return Result<bool>.Success(true);
    }

    public void Undo()
    {
        // Fundamental: Deshacer en orden inverso (LIFO)
        for (int i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }
}
