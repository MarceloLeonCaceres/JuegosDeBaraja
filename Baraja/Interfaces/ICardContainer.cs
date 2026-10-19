using Baraja.Implementaciones.Spider;

namespace Baraja.Interfaces;

public interface ICardContainer
{
    string Id { get; }
    IReadOnlyList<PlayableCard> Cards { get; }

    // Validaciones delegadas a la lógica de cada juego/zona
    Result<bool> CanReceive(IEnumerable<PlayableCard> cards);
    Result<bool> CanExtract(IEnumerable<PlayableCard> cards);

    // Acciones
    void Push(IEnumerable<PlayableCard> cards);
    IEnumerable<PlayableCard> Pop(int count);
}

// Ejemplo: Una columna del Tableau en Spider Solitaire
public class TableauColumn(string id) : ICardContainer
{
    private readonly List<PlayableCard> _cards = [];

    public string Id => id;
    public IReadOnlyList<PlayableCard> Cards => _cards.AsReadOnly();

    public Result<bool> CanReceive(IEnumerable<PlayableCard> cards)
    {
        // Aquí irá la regla de Spider: ¿Es correlativo descendente?
        // Por ahora, asumimos éxito para estructurar la base.
        return Result<bool>.Success(true);
    }

    public Result<bool> CanExtract(IEnumerable<PlayableCard> cards)
    {
        // ¿Están boca arriba y son correlativas del mismo palo?
        return Result<bool>.Success(true);
    }

    public void Push(IEnumerable<PlayableCard> cards) => _cards.AddRange(cards);

    public IEnumerable<PlayableCard> Pop(int count)
    {
        var extracted = _cards.TakeLast(count).ToList();
        _cards.RemoveRange(_cards.Count - count, count);
        return extracted;
    }
}
