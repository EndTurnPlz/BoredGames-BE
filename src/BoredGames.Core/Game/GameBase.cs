using System.Collections.Frozen;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace BoredGames.Core.Game;

public abstract class GameBase(ImmutableList<Player> players)
{
    protected abstract FrozenDictionary<Type, GameAction> ActionMap { get; }
    protected readonly ImmutableList<Player> Players = players;

    protected int ViewNum { get; set; }
    
    public abstract IGameSnapshot GetSnapshot();
    public abstract bool HasEnded();

    public IGameActionResponse? ExecuteAction(IGameActionArgs args, Player? player = null)
    {
        var action = ActionMap.GetValueOrDefault(args.GetType()) ?? throw new InvalidActionException();
        return action.Execute(args, player);
    }
}

public interface IGameSnapshot;

public interface IGameActionArgs
{
    [UsedImplicitly] static abstract string ActionName { get; }
}

public interface IGameActionResponse;

public enum GameTypes
{
    Apologies
}