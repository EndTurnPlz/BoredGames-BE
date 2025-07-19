using BoredGames.Common.Game;
using JetBrains.Annotations;

namespace BoredGames.Apologies.Models;

[UsedImplicitly]
public record MovePawnArgs(
    Move Move,
    Move? SplitMove) : IGameActionArgs;
    
[UsedImplicitly]
public record Move(
    string From,
    string To,
    int Effect);