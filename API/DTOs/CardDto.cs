namespace API.DTOs;

// Usamos string? para Suit y Rank. Si está tapada, viajan como null.
public record CardDto(string? Suit, string? Rank, bool IsFaceUp);

