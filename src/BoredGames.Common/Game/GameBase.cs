namespace BoredGames.Common.Game;

public abstract class GameBase(IEnumerable<Player> players)
{
    public int ViewNum { get; protected set; }
    public abstract IGameSnapshot GetSnapshot();
    public abstract bool HasEnded();
    public abstract IGameActionResponse? ExecuteAction(string actionType, Player? player = null, IGameActionArgs? args = null);
    
    protected readonly Player[] Players = players.ToArray();
}

public interface IGameSnapshot
{
    int ViewNum { get; }
}

public interface IGameActionArgs;

public interface IGameActionResponse;

public enum GameTypes
{
    Apologies
}