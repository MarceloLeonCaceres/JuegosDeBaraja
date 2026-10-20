using Baraja.Commands;

namespace Baraja.Implementaciones.Spider;

public class SpiderMoveCommand : IMoveCommand
{
    private readonly List<PlayableCard> _cardsToMove;
    private readonly SpiderTableauColumn _source;
    private readonly SpiderTableauColumn _destination;
    private bool _wasSourceFlipped = false;

    // Fíjate que ya NO le pasamos _gameTable.Foundations en el constructor
    public SpiderMoveCommand(List<PlayableCard> cardsToMove, SpiderTableauColumn source, SpiderTableauColumn destination)
    {
        _cardsToMove = cardsToMove;
        _source = source;
        _destination = destination;
    }

    public Result<bool> Execute()
    {
        // 1. Validar origen y destino ANTES de tocar el estado
        var canExtract = _source.CanExtract(_cardsToMove);
        if (!canExtract.IsSuccess)
            return canExtract;

        var canReceive = _destination.CanReceive(_cardsToMove);
        if (!canReceive.IsSuccess)
            return canReceive;

        // 2. Extraer del origen y empujar al destino
        var extracted = _source.Pop(_cardsToMove.Count).ToList();
        _destination.Push(extracted);

        // 3. Voltear la carta expuesta
        if (_source.Cards.Count > 0 && !_source.Cards.Last().IsFaceUp)
        {
            var top = _source.Pop(1).First();
            _source.Push([top.Flip()]);
            _wasSourceFlipped = true;
        }

        return Result<bool>.Success(true);
    }

    public void Undo()
    {
        // SEGURIDAD 1: Validar que hay cartas antes de intentar volver a tapar
        if (_wasSourceFlipped && _source.Cards.Count > 0)
        {
            var top = _source.Pop(1).First();
            _source.Push([top.Flip()]);
        }

        // SEGURIDAD 2: Solo regresar las cartas si el destino las tiene
        if (_destination.Cards.Count >= _cardsToMove.Count)
        {
            var extracted = _destination.Pop(_cardsToMove.Count).ToList();
            _source.Push(extracted);
        }
    }
}
