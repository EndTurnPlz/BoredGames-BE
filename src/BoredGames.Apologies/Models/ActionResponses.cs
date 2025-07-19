using BoredGames.Common.Game;
using JetBrains.Annotations;

namespace BoredGames.Apologies.Models;

public static class ActionResponses
{
    [UsedImplicitly]
    public record DrawCardResponse(
        int CurrentView,
        int CardDrawn,
        IEnumerable<GenericComponents.Moveset> Movesets) : IGameActionResponse;

    [UsedImplicitly]
    public record EndgameStatsResponse(
        IEnumerable<int> MovesMade,
        IEnumerable<int> PawnsKilled, 
        long GameTimeElapsed) : IGameActionResponse;
}