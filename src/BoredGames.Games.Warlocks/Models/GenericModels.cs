using BoredGames.Games.Warlocks.Deck;
using JetBrains.Annotations;

namespace BoredGames.Games.Warlocks.Models;

public static class GenericModels
{
    public record TrickResult
    {
        public required int Num { get; init; }
        public required int Leader { get; init; }
        public required int Winner { get; init; }
        public required IEnumerable<WarlocksDeck.Card> Cards { get; init; }
    }
    
    public record CurrentTrickInfo
    {
        public required int TrickLeader { get; init; }
        public required int CurrentPlayerIndex { get; init; }
        public required WarlocksDeck.Suit LeadSuit { get; init; }
        public required IEnumerable<WarlocksDeck.Card> CardsPlayed { get; init; }
    }
}