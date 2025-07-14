using BoredGames.Apologies.Deck;
using BoredGames.Common.Room.Models;

namespace BoredGames.Apologies.Models;

public record ApologiesSnapshot(   
    int ViewNum,
    ApologiesGame.State GameState,
    CardDeck.CardTypes LastDrawnCard,
    MovePawnRequest? LastCompletedMove,
    IEnumerable<string> TurnOrder,
    IEnumerable<bool> PlayerConnectionStatus,
    IEnumerable<IEnumerable<string>> Pieces) : IGameSnapshot;