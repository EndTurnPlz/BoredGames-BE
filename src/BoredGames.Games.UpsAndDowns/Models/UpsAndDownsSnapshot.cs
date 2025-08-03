using BoredGames.Core.Game;

namespace BoredGames.Games.UpsAndDowns.Models;

public record UpsAndDownsSnapshot : IGameSnapshot
{
    public required IEnumerable<string> TurnOrder { get; init; }
    public UpsAndDownsGame.State GameState { get; init; }
    public required IEnumerable<int> PlayerLocations { get; init; }
    public required IEnumerable<GenericModels.WarpTileInfo> BoardLayout { get; init; }
    public int LastDieRoll { get; init; }
}


    