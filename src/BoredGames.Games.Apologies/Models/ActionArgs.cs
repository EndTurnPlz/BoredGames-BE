using BoredGames.Core.Game;

namespace BoredGames.Games.Apologies.Models;

public static class ActionArgs
{
    public record MovePawnArgs(GenericModels.Move Move, GenericModels.Move? SplitMove = null) : IGameActionArgs;
}