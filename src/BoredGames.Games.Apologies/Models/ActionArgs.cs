using BoredGames.Core.Game;
using JetBrains.Annotations;

namespace BoredGames.Games.Apologies.Models;

public static class ActionArgs
{
    [UsedImplicitly]
    public record MovePawnArgs(GenericComponents.Move Move, GenericComponents.Move? SplitMove = null) : IGameActionArgs
    {
        public static string ActionName => "move";
    }
    
    [UsedImplicitly]
    public record DrawCardArgs : IGameActionArgs
    {
        public static string ActionName => "draw";
    }
}