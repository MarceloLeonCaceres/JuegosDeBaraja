using Baraja.Implementaciones.Spider;

namespace Baraja.Interfaces;

public abstract class CardStack(string id)
{
    protected readonly List<PlayableCard> _cards = [];

    public string Id => id;
    public IReadOnlyList<PlayableCard> Cards => _cards.AsReadOnly();

    public void Push(IEnumerable<PlayableCard> cards) => _cards.AddRange(cards);

    public IEnumerable<PlayableCard> Pop(int count)
    {
        var extracted = _cards.TakeLast(count).ToList();
        _cards.RemoveRange(_cards.Count - extracted.Count, extracted.Count);
        return extracted;
    }
}