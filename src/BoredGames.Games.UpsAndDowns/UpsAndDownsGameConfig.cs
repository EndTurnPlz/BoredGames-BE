using System.Collections.Immutable;
using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Core.Room;
using JetBrains.Annotations;

namespace BoredGames.Games.UpsAndDowns;

[UsedImplicitly]
public class UpsAndDownsGameConfig : IGameConfig
{
    public GameTypes GameType => GameTypes.UpsAndDowns;
    public int MinPlayerCount => 2;
    public int MaxPlayerCount => 8;

    public GameBase CreateGameInstance(ImmutableList<Player> players)
    {
        if (players.Count < MinPlayerCount) throw new RoomCannotStartException();
        return new UpsAndDownsGame(players);
    }
}