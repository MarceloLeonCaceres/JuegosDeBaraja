using Baraja.Interfaces;
using System.Collections.Generic;

namespace Baraja.Implementaciones.Spider;

public class SpiderFoundation(string id) : CardStack(id), ICardContainer
{
    private readonly List<PlayableCard> _cards = [];

    public string Id => id;
    public IReadOnlyList<PlayableCard> Cards => _cards.AsReadOnly();

    public Result<bool> CanReceive(IEnumerable<PlayableCard> cards) => Result<bool>.Success(true);

    // Regla: Una vez en la fundación, no se pueden sacar
    public Result<bool> CanExtract(IEnumerable<PlayableCard> cards) =>
        Result<bool>.Failure("Las cartas completadas no se pueden devolver al juego.");

    public void Push(IEnumerable<PlayableCard> cards) => _cards.AddRange(cards);

    public IEnumerable<PlayableCard> Pop(int count)
    {
        var extracted = _cards.TakeLast(count).ToList();
        _cards.RemoveRange(_cards.Count - extracted.Count, extracted.Count);
        return extracted;
    }
}
