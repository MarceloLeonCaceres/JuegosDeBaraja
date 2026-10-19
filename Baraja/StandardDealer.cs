using Baraja.Interfaces;

namespace Baraja;

public class StandardDealer : IDealer
{
    private readonly IDeck _deck;
    private readonly IShuffler _shuffler;
    private readonly Stack<Card> _drawPile;

    public int RemainingCards => _drawPile.Count;

    // Inyección de dependencias (D de SOLID)
    public StandardDealer(IDeck deck, IShuffler shuffler)
    {
        _deck = deck;
        _shuffler = shuffler;
        _drawPile = new Stack<Card>();
        ResetAndShuffle();
    }

    public void ResetAndShuffle()
    {
        _drawPile.Clear();
        var cards = _deck.GetCards().ToList();
        _shuffler.Shuffle(cards);

        foreach (var card in cards)
        {
            _drawPile.Push(card);
        }
    }

    public Result<Card> Deal()
    {
        if (_drawPile.Count == 0)
        {
            return Result<Card>.Failure("No quedan cartas en la baraja.");
        }

        return Result<Card>.Success(_drawPile.Pop());
    }

    public Result<List<Card>> DealMany(int count)
    {
        if (count <= 0) return Result<List<Card>>.Failure("La cantidad debe ser mayor a cero.");
        if (count > RemainingCards) return Result<List<Card>>.Failure($"Solo quedan {RemainingCards} cartas.");

        var dealtCards = new List<Card>(count);
        for (int i = 0; i < count; i++)
        {
            dealtCards.Add(_drawPile.Pop());
        }

        return Result<List<Card>>.Success(dealtCards);
    }
    public void Return(Card card)
    {
        _drawPile.Push(card);
    }
}
