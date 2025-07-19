using BoredGames.Common.Game;
using JetBrains.Annotations;

namespace BoredGames.Apologies.Models;

[UsedImplicitly]
public record DrawCardResponse(
    int CurrentView,
    int CardDrawn,
    IEnumerable<Moveset> Movesets) : IGameResponseArgs;
    
[UsedImplicitly]
public record Moveset(
    string Pawn,
    IEnumerable<MoveOpts> Move);
    
[UsedImplicitly]
public record MoveOpts(
    string From,
    string To, 
    IEnumerable<int> Effects);