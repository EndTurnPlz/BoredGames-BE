using BoredGames.Apologies.Deck;
using BoredGames.Common.Game;
using JetBrains.Annotations;

namespace BoredGames.Apologies.Models;

[UsedImplicitly]
public record ApologiesSnapshot(   
    int ViewNum,
    ApologiesGame.State GameState,
    CardDeck.CardTypes LastDrawnCard,
    MovePawnArgs? LastCompletedMove,
    IEnumerable<string> TurnOrder,
    IEnumerable<bool> PlayerConnectionStatus,
    IEnumerable<IEnumerable<string>> Pieces) : IGameSnapshot;