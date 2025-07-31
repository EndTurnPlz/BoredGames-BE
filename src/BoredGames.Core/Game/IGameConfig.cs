using System.Collections.Immutable;
using System.Reflection;
using BoredGames.Core.Game.Attributes;

namespace BoredGames.Core.Game;

public interface IGameConfig
{
    public Type GameType { get; }
    public int MinPlayerCount { get; }
    public int MaxPlayerCount { get; }
    
    public GameBase CreateGameInstance(ImmutableList<Player> players);

    public string GetGameName()
    {
        return GameType.GetCustomAttribute<BoredGameAttribute>()!.Name;
    }
}