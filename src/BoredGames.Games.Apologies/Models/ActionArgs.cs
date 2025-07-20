using BoredGames.Core.Game;
using JetBrains.Annotations;

namespace BoredGames.Games.Apologies.Models;

public static class ActionArgs
{
    [UsedImplicitly]
    public record MovePawnArgs(GenericComponents.Move Move, GenericComponents.Move? SplitMove) : IGameActionArgs
    {
        public static string ActionName => "move";
    }
    
    [UsedImplicitly]
    public record DrawCardArgs : IGameActionArgs
    {
        public static string ActionName => "draw";
    }

    [UsedImplicitly]
    public class GetStatsArgs : IGameActionArgs
    {
        public static string ActionName => "stats";
    }
}