using JetBrains.Annotations;

namespace BoredGames.Games.UpsAndDowns.Models;

public static class GenericModels
{
    [UsedImplicitly]
    public record WarpTileInfo(int Tile, int Dest);
}