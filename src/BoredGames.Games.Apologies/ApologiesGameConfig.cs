using System.Collections.Immutable;
using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Core.Room;
using JetBrains.Annotations;

namespace BoredGames.Games.Apologies;

[UsedImplicitly]
public class ApologiesGameConfig : IGameConfig
{
    public Type GameType => typeof(ApologiesGame);
    public int MinPlayerCount => 4;
    public int MaxPlayerCount => 4;

    public GameBase CreateGameInstance(ImmutableList<Player> players)
    {
        if (players.Count < MinPlayerCount) throw new RoomCannotStartException();
        return new ApologiesGame(players);
    }
}