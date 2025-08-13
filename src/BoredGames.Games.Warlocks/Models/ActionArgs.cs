using BoredGames.Core.Game;
using BoredGames.Games.Warlocks.Deck;

namespace BoredGames.Games.Warlocks.Models;

public static class ActionArgs
{
    public record BidArgs : IGameActionArgs
    {
        public int Bid { get; init; }
    }
    
    public record PlayCardArgs : IGameActionArgs
    {
        public required WarlocksDeck.Card Card { get; init; }
    }
}