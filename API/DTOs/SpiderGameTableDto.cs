namespace API.DTOs;

using System.Collections.Generic;

public record SpiderGameTableDto(
    int CardsLeftInStock,
    bool IsGameWon,
    IReadOnlyList<ContainerDto> Columns,
    IReadOnlyList<ContainerDto> Foundations
);

