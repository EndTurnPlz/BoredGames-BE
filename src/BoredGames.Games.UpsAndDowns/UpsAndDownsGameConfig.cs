using BoredGames.Core.Game;
using JetBrains.Annotations;

namespace BoredGames.Games.UpsAndDowns;

[UsedImplicitly]
public class UpsAndDownsGameConfig : IGameConfig
{
    public bool ShuffleTurnOrder { get; init; } = true;
}