namespace BoredGames.Common.Game;

public abstract class AbstractGame(IEnumerable<Player> players)
{
    public int ViewNum { get; protected set; }
    public abstract IGameSnapshot GetSnapshot();
    
    public abstract bool HasEnded();

    public void ExecuteAction(string action, Guid playerId, IEnumerable<object>? args = null)
    {
        // Maybe?
    }

    protected readonly Player[] Players = players.ToArray();
}

public interface IGameSnapshot
{
    int ViewNum { get; }
}

public enum GameTypes
{
    Apologies
}