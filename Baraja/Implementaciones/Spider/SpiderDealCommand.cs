using Baraja.Commands;
using Baraja.Interfaces;

namespace Baraja.Implementaciones.Spider;

public class SpiderDealCommand(SpiderGameTable table, IDealer dealer) : IMoveCommand
{
    private bool _wasExecuted = false;

    // Guardamos rastro de si el robo por casualidad completó un palo mágico (efecto dominó)
    private readonly List<(SpiderTableauColumn Column, SpiderFoundation Foundation, bool Flipped)> _completions = [];

    public Result<bool> Execute()
    {
        // 1. Validaciones
        if (table.Columns.Any(c => c.Cards.Count == 0))
            return Result<bool>.Failure("No puedes robar del mazo si hay columnas vacías.");

        if (dealer.RemainingCards < table.Columns.Count)
            return Result<bool>.Failure("No hay suficientes cartas en el mazo.");

        // 2. Repartir 1 carta a cada columna
        foreach (var column in table.Columns)
        {
            var newCard = new PlayableCard(dealer.Deal().Value!, IsFaceUp: true);
            column.Push([newCard]);
        }

        // 3. Revisar si la carta que cayó completó un palo
        foreach (var column in table.Columns)
        {
            if (column.HasCompletedSuitRun())
            {
                var completedSuit = column.Pop(13).ToList();
                var emptyFoundation = table.Foundations.First(f => f.Cards.Count == 0);
                emptyFoundation.Push(completedSuit);

                bool topFlipped = false;
                if (column.Cards.Count > 0 && !column.Cards.Last().IsFaceUp)
                {
                    topFlipped = true;
                    var topCard = column.Pop(1).First();
                    column.Push([topCard.Flip()]);
                }

                _completions.Add((column, emptyFoundation, topFlipped));
            }
        }

        _wasExecuted = true;
        return Result<bool>.Success(true);
    }

    public void Undo()
    {
        if (!_wasExecuted) return;

        // 1. Revertir las fundaciones completadas (en orden inverso LIFO)
        for (int i = _completions.Count - 1; i >= 0; i--)
        {
            var comp = _completions[i];

            if (comp.Flipped)
            {
                var topCard = comp.Column.Pop(1).First();
                comp.Column.Push([topCard.Flip()]); // Tapar de nuevo
            }

            var suitCards = comp.Foundation.Pop(13);
            comp.Column.Push(suitCards); // Devolver las 13 cartas a la mesa
        }

        // 2. Quitar la carta superior de cada columna y devolverla al mazo
        // En orden inverso (de la columna 10 a la 1) para que el Dealer quede exacto
        for (int i = table.Columns.Count - 1; i >= 0; i--)
        {
            var cardToReturn = table.Columns[i].Pop(1).First();
            dealer.Return(cardToReturn.Card);
        }
    }
}
