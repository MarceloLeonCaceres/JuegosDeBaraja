namespace Baraja;

public enum Suit
{
    Hearts,   // Corazones rojos
    Spades,   // Corazones negros / Picas
    Clubs,    // Tréboles
    Diamonds  // Brillos / Diamantes
}

public enum Rank
{
    Ace = 1, Two = 2, Three = 3, Four = 4, Five = 5,
    Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10,
    Jack = 11, Queen = 12, King = 13
}

// Inmutabilidad garantizada
public record Card(Suit Suit, Rank Rank);

