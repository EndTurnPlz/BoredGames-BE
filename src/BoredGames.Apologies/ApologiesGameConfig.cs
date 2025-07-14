using BoredGames.Common.Game;

namespace BoredGames.Apologies;

public class ApologiesGameConfig : AbstractGameConfig
{
    public override int MinPlayerCount => 4;
    public override int MaxPlayerCount => 4;
    public override Type GameType => typeof(ApologiesGame);
}