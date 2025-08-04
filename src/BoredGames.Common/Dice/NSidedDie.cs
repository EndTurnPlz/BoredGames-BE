namespace BoredGames.Core.Game.Components.Dice;

public class NSidedDie(int numSides) : IDie
{
    public int Roll()
    {
        LastRollValue = new Random().Next(1, numSides + 1);
        return LastRollValue;
    }

    public int LastRollValue {get; private set; } = -1;
}

// Shorthand for a standard six-sided die
public sealed class StandardDie() : NSidedDie(6);