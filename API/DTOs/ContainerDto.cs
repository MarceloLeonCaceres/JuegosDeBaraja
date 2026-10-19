namespace API.DTOs;

using System.Collections.Generic;

public record ContainerDto(string Id, IReadOnlyList<CardDto> Cards);

