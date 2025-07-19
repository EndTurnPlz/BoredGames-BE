using BoredGames.Common.Game;
using JetBrains.Annotations;

namespace BoredGames.Apologies.Models;

[UsedImplicitly]
public record EndgameStatsResponse(
    IEnumerable<int> MovesMade,
    IEnumerable<int> PawnsKilled, 
    long GameTimeElapsed) : IGameResponseArgs;