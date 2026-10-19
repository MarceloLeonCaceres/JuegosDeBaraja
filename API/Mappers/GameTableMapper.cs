using API.DTOs;
using Baraja.Implementaciones.Spider;
using Baraja.Interfaces;
using System.Linq;

namespace API.Mappers;

public static class GameTableMapper
{
    public static CardDto ToDto(this PlayableCard playableCard)
    {
        // Regla de seguridad vital: Enmascaramiento de datos
        if (playableCard.IsFaceUp)
        {
            return new CardDto(
                Suit: playableCard.Card.Suit.ToString(),
                Rank: playableCard.Card.Rank.ToString(),
                IsFaceUp: true
            );
        }

        // El frontend recibe nulos, haciendo imposible hacer trampa
        return new CardDto(Suit: null, Rank: null, IsFaceUp: false);
    }

    public static ContainerDto ToDto(this ICardContainer container)
    {
        var mappedCards = container.Cards
                                   .Select(c => c.ToDto())
                                   .ToList();

        return new ContainerDto(container.Id, mappedCards);
    }

    public static SpiderGameTableDto ToDto(this SpiderGameTable table)
    {
        return new SpiderGameTableDto(
            CardsLeftInStock: table.CardsLeftInStock,
            IsGameWon: table.IsGameWon,
            Columns: table.Columns.Select(c => c.ToDto()).ToList(),
            Foundations: table.Foundations.Select(f => f.ToDto()).ToList()
        );
    }
}
