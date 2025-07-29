using JetBrains.Annotations;

namespace BoredGames.Games.UpsAndDowns.Models;

public class GenericModels
{
    [UsedImplicitly]
    public record WarpTileInfo(int Tile, int Dest);
}