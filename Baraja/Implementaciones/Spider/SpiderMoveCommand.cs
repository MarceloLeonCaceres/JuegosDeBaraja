using Baraja.Commands;
using Baraja.Interfaces;

namespace Baraja.Implementaciones.Spider;

// Usamos el constructor primario de C# 12 para inyectar dependencias de la acción
public class SpiderMoveCommand(
        IEnumerable<PlayableCard> cards,
        SpiderTableauColumn source,
        SpiderTableauColumn destination,
        IReadOnlyList<SpiderFoundation> foundations) : IMoveCommand
{
    private readonly List<PlayableCard> _movingCards = cards.ToList();

    // Instantáneas de estado para posibilitar el Undo perfecto
    private bool _sourceTopCardFlipped;
    private bool _destTopCardFlippedAfterCompletion;
    private SpiderFoundation? _usedFoundation;

    public Result<bool> Execute()
    {
        // 1. Validar movimiento de jugador
        var extractResult = source.CanExtract(_movingCards);
        if (!extractResult.IsSuccess) return extractResult;

        var receiveResult = destination.CanReceive(_movingCards);
        if (!receiveResult.IsSuccess) return receiveResult;

        // 2. Ejecutar movimiento principal
        source.Pop(_movingCards.Count);
        destination.Push(_movingCards);

        // 3. Efecto secundario A: Voltear carta en origen
        if (source.Cards.Count > 0 && !source.Cards.Last().IsFaceUp)
        {
            _sourceTopCardFlipped = true;
            FlipTopCard(source);
        }

        // 4. Efecto secundario B: Escanear y completar palo en destino
        if (destination.HasCompletedSuitRun())
        {
            var completedSuit = destination.Pop(13).ToList();
            _usedFoundation = foundations.First(f => f.Cards.Count == 0);
            _usedFoundation.Push(completedSuit);

            // 5. Efecto secundario C: Voltear carta en destino tras vaciar el palo
            if (destination.Cards.Count > 0 && !destination.Cards.Last().IsFaceUp)
            {
                _destTopCardFlippedAfterCompletion = true;
                FlipTopCard(destination);
            }
        }

        return Result<bool>.Success(true);
    }

    public void Undo()
    {
        // Revertimos exactamente en el orden inverso a la ejecución

        // 1. Revertir volteo en destino tras completar palo
        if (_destTopCardFlippedAfterCompletion)
        {
            FlipTopCard(destination);
        }

        // 2. Devolver el palo completo de la fundación al destino
        if (_usedFoundation != null)
        {
            var suitCards = _usedFoundation.Pop(13); // La máquina física lo permite, la UI no.
            destination.Push(suitCards);
        }

        // 3. Revertir volteo en origen
        if (_sourceTopCardFlipped)
        {
            FlipTopCard(source);
        }

        // 4. Devolver las cartas originales del destino al origen
        destination.Pop(_movingCards.Count);
        source.Push(_movingCards);
    }

    // Helper DRY para voltear la carta en la cima de cualquier columna
    private static void FlipTopCard(ICardContainer container)
    {
        var topCard = container.Pop(1).First();
        container.Push([topCard.Flip()]); // Expresión de colección C# 12
    }
}
