namespace BoredGames.Games.Apologies.Board;

public abstract class BoardTile (string name)
{
    public string Name { get; } = name;
}

public abstract class WalkableTile (string name, BoardTile nextTile, BoardTile prevTile) 
    : BoardTile(name)
{
    public BoardTile NextTile { protected get; set; } = nextTile;
    public BoardTile PrevTile { get; set; } = prevTile;
    
    public virtual BoardTile EvaluateNextTile(int playerSide) => NextTile;
}

public class BasicTile(string name, BasicTile nextTile, BasicTile prevTile)
    : WalkableTile(name, nextTile, prevTile);

public class SafetyZoneTile(string name, BoardTile nextTile, BoardTile prevTile)
    : WalkableTile(name, nextTile, prevTile);

public class HomeTile(string name) : BoardTile(name);

public class StartTile(string name, BasicTile nextTile) : BoardTile(name)
{
    public BasicTile NextTile { get; set; } = nextTile;
}

public class JunctionTile (
    string name,
    BasicTile nextTile,
    BasicTile prevTile,
    SafetyZoneTile safetyZoneTile,
    int playerSide
) :
    BasicTile(name, nextTile, prevTile)
{
    private SafetyZoneTile SafetyZone { get; } = safetyZoneTile;
    private int PlayerSide { get; } = playerSide;

    public override BoardTile EvaluateNextTile(int playerSide)
    {
        if (playerSide == -1) throw new Exception("Player side must be specified for junction tile");
        return playerSide == PlayerSide ? SafetyZone : NextTile;
    }
}

public class SliderTile(string name, BasicTile nextTile, BasicTile prevTile, BasicTile targetTile, int playerSide)
    : BasicTile(name, nextTile, prevTile)
{
    public BasicTile TargetTile { get; set; } = targetTile;
    public readonly int PlayerSide = playerSide;
}