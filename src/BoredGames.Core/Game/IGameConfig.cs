using System.Collections.Immutable;

namespace BoredGames.Core.Game;

public interface IGameConfig
{
    public GameTypes GameType { get; }
    public int MinPlayerCount { get; }
    public int MaxPlayerCount { get; }
    
    public GameBase CreateGameInstance(ImmutableList<Player> players);
}