namespace BoredGames.Common.Game;

public abstract class GameBase(IEnumerable<Player> players)
{
    public int ViewNum { get; protected set; }
    public abstract IGameSnapshot GetSnapshot();
    public abstract bool HasEnded();
    public abstract object? ExecuteAction(string action, Player? player = null, IGameActionArgs? args = null);
    
    protected readonly Player[] Players = players.ToArray();
}

public interface IGameSnapshot
{
    int ViewNum { get; }
}

public interface IGameActionArgs;

public enum GameTypes
{
    Apologies
}