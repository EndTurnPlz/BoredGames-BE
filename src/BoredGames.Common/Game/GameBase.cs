using JetBrains.Annotations;

namespace BoredGames.Common.Game;

public abstract class GameBase(IEnumerable<Player> players)
{
    public int ViewNum { get; protected set; }
    public abstract IGameSnapshot GetSnapshot();
    public abstract bool HasEnded();
    public abstract IGameActionResponse? ExecuteAction(IGameActionArgs args, Player? player = null);
    
    protected readonly Player[] Players = players.ToArray();
}

public interface IGameSnapshot
{
    [UsedImplicitly] int ViewNum { get; }
}

public interface IGameActionArgs
{
    [UsedImplicitly] static abstract string ActionName { get; }
}

public interface IGameActionResponse;

public enum GameTypes
{
    Apologies
}