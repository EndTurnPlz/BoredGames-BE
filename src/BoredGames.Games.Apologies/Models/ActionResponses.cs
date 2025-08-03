using BoredGames.Core.Game;
using JetBrains.Annotations;

namespace BoredGames.Games.Apologies.Models;

public static class ActionResponses
{
    [UsedImplicitly]
    public record DrawCardResponse(
        IEnumerable<GenericModels.Moveset> Movesets) : IGameActionResponse;
}