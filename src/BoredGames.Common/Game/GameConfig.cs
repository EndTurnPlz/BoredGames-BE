namespace BoredGames.Common.Game;

public abstract class GameConfig
{
    public abstract int MinPlayerCount { get; }
    public abstract int MaxPlayerCount { get; }

    public abstract GameBase CreateGameInstance(IReadOnlyList<Player> players);
}