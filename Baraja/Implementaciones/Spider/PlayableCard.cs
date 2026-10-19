namespace Baraja.Implementaciones.Spider;
// El Wrapper que omitimos en el bloque anterior
public record PlayableCard(Card Card, bool IsFaceUp = false)
{
    public PlayableCard Flip() => this with { IsFaceUp = !IsFaceUp };
}
