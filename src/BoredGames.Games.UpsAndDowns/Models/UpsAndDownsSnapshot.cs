using BoredGames.Core.Game;
using JetBrains.Annotations;

namespace BoredGames.Games.UpsAndDowns.Models;

[UsedImplicitly]
public record UpsAndDownsSnapshot(
    UpsAndDownsGame.State GameState,
    IEnumerable<int> PlayerLocations,
    IEnumerable<GenericModels.WarpTileInfo> BoardLayout,
    int LastRollValue) : IGameSnapshot;
    