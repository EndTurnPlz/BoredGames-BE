namespace BoredGames.Common.Game;

public abstract class AbstractGame
{
    public int ViewNum { get; protected set; }
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