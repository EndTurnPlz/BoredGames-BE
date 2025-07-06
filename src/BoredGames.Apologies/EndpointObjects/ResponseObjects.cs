using JetBrains.Annotations;

namespace BoredGames.Apologies.EndpointObjects;

[UsedImplicitly]
public record PullGameStateResponse(
    int CurrentView,
    int GamePhase,
    int LastDrawnCard,
    MovePawnRequest? LastCompletedMove,
    int Host,
    IEnumerable<string> TurnOrder,
    IEnumerable<bool> PlayerConnectionStatus,
    IEnumerable<IEnumerable<string>> Pieces);

[UsedImplicitly]
public record DrawCardResponse(
    int CurrentView,
    int CardDrawn,
    IEnumerable<Moveset> Movesets);

[UsedImplicitly]
public record Moveset(
    string Pawn,
    IEnumerable<MoveOpts> Move);

[UsedImplicitly]
public record MoveOpts(
    string From,
    string To, 
    IEnumerable<int> Effects);
    