using Baraja.Commands;


namespace Baraja.Implementaciones.Spider;

public class SpiderCompleteSuitCommand(SpiderGameTable table) : IMoveCommand
{
    // AHORA GUARDAMOS LA LISTA DE CARTAS (List<PlayableCard> Cards) EN MEMORIA
    private readonly List<(SpiderTableauColumn Column, SpiderFoundation Foundation, List<PlayableCard> Cards, bool Flipped)> _completions = [];
    private bool _wasExecuted = false;

    public Result<bool> Execute()
    {
        bool anyCompleted = false;

        foreach (var column in table.Columns)
        {
            if (column.HasCompletedSuitRun())
            {
                // 1. Extraer las 13 cartas
                var completedSuit = column.Pop(13).ToList();
                var emptyFoundation = table.Foundations.First(f => f.Cards.Count == 0);
                emptyFoundation.Push(completedSuit);

                // 2. Voltear la carta que quedó expuesta
                bool topFlipped = false;
                if (column.Cards.Count > 0 && !column.Cards.Last().IsFaceUp)
                {
                    topFlipped = true;
                    var topCard = column.Pop(1).First();
                    column.Push([topCard.Flip()]);
                }

                // 3. GUARDAMOS LA REFERENCIA DE LAS 13 CARTAS EXACTAS
                _completions.Add((column, emptyFoundation, completedSuit, topFlipped));
                anyCompleted = true;
            }
        }

        _wasExecuted = true;
        return anyCompleted ? Result<bool>.Success(true) : Result<bool>.Failure("Nada que completar.");
    }

    public void Undo()
    {
        if (!_wasExecuted) return;

        // Recorrer en reversa
        for (int i = _completions.Count - 1; i >= 0; i--)
        {
            var comp = _completions[i];

            // 1. Revertir la carta descubierta (volver a taparla)
            if (comp.Flipped && comp.Column.Cards.Count > 0)
            {
                var topCard = comp.Column.Pop(1).First();
                comp.Column.Push([topCard.Flip()]);
            }

            // 2. Retirar las cartas de la fundación para limpiarla visualmente
            var removed = comp.Foundation.Pop(comp.Cards.Count).ToList();
            System.Diagnostics.Debug.Assert(removed.Count == comp.Cards.Count);
            System.Diagnostics.Debug.Assert(comp.Foundation.Cards.Count == 0);

            comp.Column.Push(comp.Cards);
        }
    }
}
