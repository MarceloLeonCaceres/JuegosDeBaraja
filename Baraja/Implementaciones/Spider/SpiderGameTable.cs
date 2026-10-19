using Baraja.Interfaces;
using System.Collections.Generic;

namespace Baraja.Implementaciones.Spider;

public class SpiderGameTable
{
    private readonly IDealer _dealer;
    public IReadOnlyList<SpiderTableauColumn> Columns { get; }

    // Exponemos las cartas restantes en el mazo (Stock)
    public int CardsLeftInStock => _dealer.RemainingCards;
    // Nueva zona de juego
    public IReadOnlyList<SpiderFoundation> Foundations { get; }
    public SpiderGameTable(IDealer dealer)
    {
        _dealer = dealer;

        var columns = new List<SpiderTableauColumn>(10);
        for (int i = 0; i < 10; i++) columns.Add(new SpiderTableauColumn($"Columna_{i + 1}"));
        Columns = columns.AsReadOnly();

        // Inicializar los 8 espacios para los palos completados
        var foundations = new List<SpiderFoundation>(8);
        for (int i = 0; i < 8; i++) foundations.Add(new SpiderFoundation($"Fundacion_{i + 1}"));
        Foundations = foundations.AsReadOnly();
    }

    public Result<bool> DealInitialBoard()
    {
        // Validamos que el Dealer haya sido cargado con el mazo doble correcto
        if (_dealer.RemainingCards != 104)
        {
            return Result<bool>.Failure("El juego de Spider requiere exactamente 104 cartas en el mazo.");
        }

        // Las primeras 4 columnas reciben 6 cartas. Las últimas 6 columnas reciben 5 cartas.
        for (int colIndex = 0; colIndex < 10; colIndex++)
        {
            int cardsForThisColumn = colIndex < 4 ? 6 : 5;
            var cardsToPush = new List<PlayableCard>();

            for (int cardIndex = 0; cardIndex < cardsForThisColumn; cardIndex++)
            {
                var dealResult = _dealer.Deal();
                if (!dealResult.IsSuccess || dealResult.Value == null)
                {
                    return Result<bool>.Failure("Error crítico al repartir desde la baraja.");
                }

                // En Spider, solo la carta superior (la última que se reparte) va descubierta
                bool isFaceUp = cardIndex == (cardsForThisColumn - 1);
                cardsToPush.Add(new PlayableCard(dealResult.Value, isFaceUp));
            }

            // Insertamos el bloque de cartas inicial en la columna
            Columns[colIndex].Push(cardsToPush);
        }

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Escanea el tablero, mueve los palos completos a las fundaciones 
    /// y voltea la carta que queda expuesta.
    /// </summary>
    public Result<int> CheckAndMoveCompletedSuits()
    {
        int suitsCompletedThisTurn = 0;

        foreach (var column in Columns)
        {
            if (column.HasCompletedSuitRun())
            {
                // 1. Extraer la secuencia completa K-A (13 cartas)
                var completedSuit = column.Pop(13).ToList();

                // 2. Mover a la primera fundación vacía disponible
                var emptyFoundation = Foundations.First(f => f.Cards.Count == 0);
                emptyFoundation.Push(completedSuit);

                // 3. Regla de Solitario: Voltear la nueva carta en la cima de la columna si está tapada
                if (column.Cards.Count > 0 && !column.Cards.Last().IsFaceUp)
                {
                    var topCard = column.Pop(1).First();
                    column.Push([topCard.Flip()]); // C# 12 collection expression
                }

                suitsCompletedThisTurn++;
            }
        }

        return Result<int>.Success(suitsCompletedThisTurn);
    }

    // Método de victoria rápida
    public bool IsGameWon => Foundations.All(f => f.Cards.Count == 13);
}
