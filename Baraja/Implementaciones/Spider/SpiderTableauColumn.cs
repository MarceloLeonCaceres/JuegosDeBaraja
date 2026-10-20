namespace Baraja.Implementaciones.Spider;

using Baraja.Interfaces;
using System.Collections.Generic;
using System.Linq;

public class SpiderTableauColumn(string id) : CardStack(id), ICardContainer
{
    private readonly List<PlayableCard> _cards = [];

    public string Id => id;
    public IReadOnlyList<PlayableCard> Cards => _cards.AsReadOnly();   

    public Result<bool> CanReceive(IEnumerable<PlayableCard> incomingCards)
    {
        var cardsList = incomingCards.ToList();
        if (cardsList.Count == 0) return Result<bool>.Failure("No hay cartas para mover.");

        // Regla 1: Las columnas vacías aceptan cualquier secuencia válida
        if (_cards.Count == 0) return Result<bool>.Success(true);

        var topCard = _cards.Last();
        var bottomIncomingCard = cardsList.First();

        // Regla 2: La carta entrante más alta debe ser un rango menor a la carta en la cima
        if ((int)topCard.Card.Rank - 1 == (int)bottomIncomingCard.Card.Rank)
        {
            return Result<bool>.Success(true);
        }

        return Result<bool>.Failure($"No puedes poner un {bottomIncomingCard.Card.Rank} sobre un {topCard.Card.Rank}.");
    }

    public Result<bool> CanExtract(IEnumerable<PlayableCard> cardsToExtract)
    {
        var cardsList = cardsToExtract.ToList();
        if (cardsList.Count == 0) return Result<bool>.Failure("Selección vacía.");

        // Validar que las cartas solicitadas existan en la base de la pila
        if (_cards.Count < cardsList.Count) return Result<bool>.Failure("No hay suficientes cartas.");

        var actualTail = _cards.TakeLast(cardsList.Count).ToList();
        if (!actualTail.SequenceEqual(cardsList)) return Result<bool>.Failure("Las cartas no coinciden con la cima de la columna.");

        // Regla 3: Para mover múltiples cartas, deben ser del mismo palo y descender consecutivamente
        for (int i = 0; i < cardsList.Count; i++)
        {
            if (!cardsList[i].IsFaceUp)
                return Result<bool>.Failure("No puedes mover cartas tapadas.");

            if (i > 0)
            {
                var previous = cardsList[i - 1].Card;
                var current = cardsList[i].Card;

                if (previous.Suit != current.Suit)
                    return Result<bool>.Failure("El bloque debe ser del mismo palo para moverse junto.");

                if ((int)previous.Rank - 1 != (int)current.Rank)
                    return Result<bool>.Failure("Las cartas no forman una secuencia descendente perfecta.");
            }
        }

        return Result<bool>.Success(true);
    }

    public void Push(IEnumerable<PlayableCard> cards) => _cards.AddRange(cards);

    public IEnumerable<PlayableCard> Pop(int count)
    {
        var extracted = _cards.TakeLast(count).ToList();
        _cards.RemoveRange(_cards.Count - count, count);
        return extracted;
    }

    // Método auxiliar para detectar cuándo enviar un palo a las fundaciones
    public bool HasCompletedSuitRun()
    {
        if (_cards.Count < 13) return false;

        var last13 = _cards.TakeLast(13).ToList();

        // Verificación rápida de extremos antes de iterar
        if (last13.Last().Card.Rank != Rank.Ace || last13.First().Card.Rank != Rank.King)
            return false;

        return CanExtract(last13).IsSuccess;
    }

}
