using BoredGames.Core.Game;
using JetBrains.Annotations;

namespace BoredGames.Games.Apologies.Models;

public static class ActionResponses
{
    [UsedImplicitly]
    public record DrawCardResponse(
        int CardDrawn,
        IEnumerable<GenericComponents.Moveset> Movesets) : IGameActionResponse;

    [UsedImplicitly]
    public record GetStatsResponse(
        IEnumerable<int> MovesMade,
        IEnumerable<int> PawnsKilled, 
        int GameTimeElapsed) : IGameActionResponse;
}