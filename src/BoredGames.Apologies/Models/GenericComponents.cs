using JetBrains.Annotations;

namespace BoredGames.Apologies.Models;


public static class GenericComponents
{
    [UsedImplicitly]
    public record Move(
        string From,
        string To,
        int Effect);
    
    [UsedImplicitly]
    public record Moveset(
        string Pawn,
        IEnumerable<MoveOpts> Opts);
    
    [UsedImplicitly]
    public record MoveOpts(
        string From,
        string To, 
        IEnumerable<int> Effects);
}

