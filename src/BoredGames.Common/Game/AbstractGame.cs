namespace BoredGames.Common.Game;

public abstract class AbstractGame
{
    public int ViewNum { get; protected set; }
    public bool HasStarted => ViewNum > 0;

    public abstract IGameSnapshot GetSnapshot();
    
    public abstract bool HasEnded();
}

public interface IGameSnapshot
{
    int ViewNum { get; }
}

public enum GameTypes
{
    Apologies
}