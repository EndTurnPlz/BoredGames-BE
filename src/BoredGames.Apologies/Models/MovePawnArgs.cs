using JetBrains.Annotations;

namespace BoredGames.Apologies.Models;

[UsedImplicitly]
public record MovePawnArgs(
    Move Move,
    Move? SplitMove);
    
[UsedImplicitly]
public record Move(
    string From,
    string To,
    int Effect);