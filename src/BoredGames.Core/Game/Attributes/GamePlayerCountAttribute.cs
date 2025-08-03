using JetBrains.Annotations;

namespace BoredGames.Core.Game.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class GamePlayerCountAttribute(int minPlayers, int maxPlayers) : Attribute
{
    public readonly int MaxPlayers = maxPlayers;
    public readonly int MinPlayers = minPlayers;

    public GamePlayerCountAttribute(int numPlayers) : this(numPlayers, numPlayers) { }
}