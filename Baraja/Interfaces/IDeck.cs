namespace Baraja.Interfaces;

public interface IDeck
{
    IReadOnlyCollection<Card> GetCards();
}

