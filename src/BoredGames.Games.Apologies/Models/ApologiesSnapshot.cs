using BoredGames.Core.Game;
using BoredGames.Games.Apologies.Deck;
using JetBrains.Annotations;

namespace BoredGames.Games.Apologies.Models;

[UsedImplicitly]
public record ApologiesSnapshot(
    int ViewNum,
    ApologiesGame.State GameState,
    CardDeck.CardTypes LastDrawnCard,
    ActionArgs.MovePawnArgs? LastCompletedMove,
    GenericComponents.GameStats GameStats,
    IEnumerable<string> TurnOrder,
    IEnumerable<bool> PlayerConnectionStatus,
    IEnumerable<IEnumerable<string>> Pieces) : IGameSnapshot;