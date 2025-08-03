using BoredGames.Core.Game;
using JetBrains.Annotations;

namespace BoredGames.Games.Apologies;

[UsedImplicitly]
public class ApologiesGameConfig : IGameConfig
{
    public bool ShuffleTurnOrder { get; init; } = true;
}