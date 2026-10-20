using Baraja.Implementaciones.Spider;
using Baraja.Interfaces;

namespace Baraja.Commands;

public interface IMoveCommand
{
    Result<bool> Execute();
    void Undo();
}

public class MoveCardsCommand(
    IEnumerable<PlayableCard> cards,
    ICardContainer source,
    ICardContainer destination) : IMoveCommand
{
    private readonly List<PlayableCard> _movingCards = cards.ToList();

    // Estado guardado para el Undo: ¿La carta que quedó en la cima estaba tapada y se volteó?
    private bool _sourceTopCardWasFlipped = false;

    public Result<bool> Execute()
    {
        var canExtract = source.CanExtract(_movingCards);
        if (!canExtract.IsSuccess) return canExtract;

        var canReceive = destination.CanReceive(_movingCards);
        if (!canReceive.IsSuccess) return canReceive;

        // Mover las cartas
        source.Pop(_movingCards.Count);
        destination.Push(_movingCards);

        // Regla común en solitarios: si la nueva carta en la cima del origen estaba tapada, se voltea
        if (source.Cards.Count > 0 && !source.Cards.Last().IsFaceUp)
        {
            _sourceTopCardWasFlipped = true;
            // Lógica para voltearla (requeriría un método FlipTopCard en ICardContainer)
        }

        return Result<bool>.Success(true);
    }

    public void Undo()
    {
        // Revertir el volteo de la carta origen si ocurrió
        if (_sourceTopCardWasFlipped)
        {
            // Revertir FlipTopCard
        }

        // Devolver las cartas del destino al origen
        destination.Pop(_movingCards.Count);
        source.Push(_movingCards);
    }
}
