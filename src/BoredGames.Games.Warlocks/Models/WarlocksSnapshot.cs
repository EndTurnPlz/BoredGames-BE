using BoredGames.Core.Game;
using BoredGames.Games.Warlocks.Deck;

namespace BoredGames.Games.Warlocks.Models;


public record WarlocksEndSnapshot : IGameSnapshot
{
    public required IEnumerable<string> TurnOrder { get; init; }
    public required string GameState { get; init; }
    public required IEnumerable<int> PlayerPoints { get; init; }
    public required GenericModels.TrickResult? LastTrickResult { get; init; }
}
public abstract record WarlocksPlayingSnapshot : IGameSnapshot
{
    public required IEnumerable<string> TurnOrder { get; init; }
    public required string GameState { get; init; }
    public required int RoundNumber { get; init; }
    public required IEnumerable<int> PlayerPoints { get; init; }
    public required GenericModels.TrickResult? LastTrickResult { get; init; }
    public required WarlocksDeck.Suit TrumpSuite { get; init; }
}

public record WarlocksBidSnapshot : WarlocksPlayingSnapshot
{
    public required int ThisPlayerBid { get; init; }
    public required IEnumerable<WarlocksDeck.Card> ThisPlayerHand { get; init; }
}

public record WarlocksPlayTrickSnapshot : WarlocksPlayingSnapshot
{
    public required IEnumerable<int> PlayerBids { get; init; }
    public required GenericModels.CurrentTrickInfo CurrentTrick { get; init; }
    public required IEnumerable<(WarlocksDeck.Card card, bool isPlayable)> ThisPlayerHand { get; init; }
}

