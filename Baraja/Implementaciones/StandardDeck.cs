using Baraja.Interfaces;

namespace Baraja.Implementaciones;

public class StandardDeck : IDeck
{
    public IReadOnlyCollection<Card> GetCards()
    {
        var cards = new List<Card>(52);
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (Rank rank in Enum.GetValues<Rank>())
            {
                cards.Add(new Card(suit, rank));
            }
        }
        return cards.AsReadOnly();
    }
}

