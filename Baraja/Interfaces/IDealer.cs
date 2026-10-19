namespace Baraja.Interfaces;

public interface IDealer
{
    int RemainingCards { get; }
    void ResetAndShuffle();
    Result<Card> Deal();
    Result<List<Card>> DealMany(int count);
    void Return(Card card);
}

