namespace BoredGames.Common.Game;

public abstract class AbstractGameConfig
{
    public abstract int MinPlayerCount { get; }
    public abstract int MaxPlayerCount { get; }

    public abstract Type GameType { get; }
}