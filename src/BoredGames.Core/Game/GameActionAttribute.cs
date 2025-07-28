using JetBrains.Annotations;

namespace BoredGames.Core.Game;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class GameActionAttribute(string actionName) : Attribute
{
    public string Name { get; } = actionName;
}