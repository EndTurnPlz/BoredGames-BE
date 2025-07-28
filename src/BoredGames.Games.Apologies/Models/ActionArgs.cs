using BoredGames.Core.Game;

namespace BoredGames.Games.Apologies.Models;

public static class ActionArgs
{
    public record MovePawnArgs(GenericComponents.Move Move, GenericComponents.Move? SplitMove = null) : IGameActionArgs;
}