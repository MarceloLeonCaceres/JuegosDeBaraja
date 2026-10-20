using Baraja.Commands;
using Baraja.Interfaces;

namespace Baraja.Implementaciones.Spider;

public class SpiderDealCommand(SpiderGameTable table, IDealer dealer) : IMoveCommand
{
    private bool _wasExecuted = false;

    public Result<bool> Execute()
    {
        if (table.Columns.Any(c => c.Cards.Count == 0))
            return Result<bool>.Failure("No puedes robar del mazo si hay columnas vacías.");

        if (dealer.RemainingCards < table.Columns.Count)
            return Result<bool>.Failure("No hay suficientes cartas en el mazo.");

        foreach (var column in table.Columns)
        {
            var newCard = new PlayableCard(dealer.Deal().Value!, IsFaceUp: true);
            column.Push([newCard]);
        }

        _wasExecuted = true;
        return Result<bool>.Success(true);
    }

    public void Undo()
    {
        if (!_wasExecuted) return;

        // Retirar la última carta de cada columna y devolverla al mazo en orden inverso
        for (int i = table.Columns.Count - 1; i >= 0; i--)
        {
            var cardToReturn = table.Columns[i].Pop(1).First();
            dealer.Return(cardToReturn.Card);
        }
    }

}
