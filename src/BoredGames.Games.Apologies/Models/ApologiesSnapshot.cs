using BoredGames.Core.Game;
using BoredGames.Games.Apologies.Deck;

namespace BoredGames.Games.Apologies.Models;

public record ApologiesSnapshot : IGameSnapshot
{
    public required IEnumerable<string> TurnOrder { get; init; }
    public required ApologiesGame.State GameState { get; init; }
    public required CardDeck.CardTypes LastDrawnCard { get; init; }
    public required ActionArgs.MovePawnArgs? LastCompletedMove { get; init; }
    public required GenericModels.GameStats GameStats { get; init; }
    public required IEnumerable<IEnumerable<string>> Pieces { get; init; }
    public required IEnumerable<GenericModels.Moveset>? CurrentMoveset { get; init; }
}