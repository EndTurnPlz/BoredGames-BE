using JetBrains.Annotations;

namespace BoredGames.Apologies.Models;

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

[UsedImplicitly]
public record GetEndgameStatsResponse(
    IEnumerable<int> MovesMade,
    IEnumerable<int> PawnsKilled, 
    long GameTimeElapsed);
    