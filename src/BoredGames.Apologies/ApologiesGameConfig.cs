using BoredGames.Common;
using BoredGames.Common.Game;
using BoredGames.Common.Room;

namespace BoredGames.Apologies;

public class ApologiesGameConfig : GameConfig
{
    public override int MinPlayerCount => 4;
    public override int MaxPlayerCount => 4;

    public override GameBase CreateGameInstance(IReadOnlyList<Player> players)
    {
        if (players.Count < MinPlayerCount) throw new RoomCannotStartException();
        return new ApologiesGame(players);
    }
}