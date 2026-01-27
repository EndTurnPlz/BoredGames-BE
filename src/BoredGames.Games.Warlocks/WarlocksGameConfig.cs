using BoredGames.Core.Game;

namespace BoredGames.Games.Warlocks;

public class WarlocksGameConfig : IGameConfig
{
    public bool ShuffleTurnOrder { get; init; } = true;
    public int NumRounds { get; init; } = 10;
    public bool RevealBids { get; init; } = true;
}