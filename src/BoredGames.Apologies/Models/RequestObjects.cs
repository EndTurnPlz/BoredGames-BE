namespace BoredGames.Apologies.Models;

public record MovePawnRequest(
    Move Move,
    Move? SplitMove);
    
public record Move(
    string From,
    string To,
    int Effect);