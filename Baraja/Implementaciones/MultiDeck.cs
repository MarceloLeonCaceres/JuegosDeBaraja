using Baraja.Interfaces;

namespace Baraja.Implementaciones;

// C# 12 Primary Constructor
public class MultiDeck(int setsPerSuit, IEnumerable<Suit> allowedSuits) : IDeck
{
    public IReadOnlyCollection<Card> GetCards()
    {
        var cards = new List<Card>();
        var suits = allowedSuits.ToList();

        for (int i = 0; i < setsPerSuit; i++)
        {
            foreach (var suit in suits)
            {
                foreach (Rank rank in Enum.GetValues<Rank>())
                {
                    cards.Add(new Card(suit, rank));
                }
            }
        }
        return cards.AsReadOnly();
    }
}

// Fábrica para facilitar la creación de juegos
public static class SpiderDeckFactory
{
    // 104 cartas: 8 sets de picas
    public static IDeck CreateOneSuit() =>
        new MultiDeck(8, [Suit.Spades]);

    // 104 cartas: 4 sets de picas, 4 de corazones
    public static IDeck CreateTwoSuits() =>
        new MultiDeck(4, [Suit.Spades, Suit.Hearts]);

    // 104 cartas: 2 barajas estándar
    public static IDeck CreateFourSuits() =>
        new MultiDeck(2, Enum.GetValues<Suit>()); // 104 cartas: 2 barajas estándar
    public static IDeck CreatePersonalizado() =>
        new MultiDeck(1, Enum.GetValues<Suit>().Append(Suit.Spades).Append(Suit.Spades).Append(Suit.Spades).Append(Suit.Spades));
}
