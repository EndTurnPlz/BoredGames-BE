using BoredGames.Core.Game;
using JetBrains.Annotations;

namespace BoredGames.Games.Apologies;

[UsedImplicitly]
public class ApologiesGameConfig : IGameConfig
{
    public Type GameType => typeof(ApologiesGame);
    public int MinPlayerCount => 4;
    public int MaxPlayerCount => 4;
}