using BoredGames.Common.Game;

namespace BoredGames.Apologies.Models;

public static class ActionArgs
{
    public record MovePawnArgs(GenericComponents.Move Move, GenericComponents.Move? SplitMove) : IGameActionArgs
    {
        public static string ActionName => "move";
    }
    
    public record DrawCardArgs : IGameActionArgs
    {
        public static string ActionName => "draw";
    }

    public class GetStatsArgs : IGameActionArgs
    {
        public static string ActionName => "stats";
    }
}