using Baraja.Interfaces;

namespace Baraja;

public class RandomShuffler : IShuffler
{
    private readonly Random _random = new();

    public RandomShuffler(int seed)
    {
        _random = new Random(seed);
    }

    public void Shuffle(IList<Card> cards)
    {
        int n = cards.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            (cards[n], cards[k]) = (cards[k], cards[n]);
        }
    }
}
