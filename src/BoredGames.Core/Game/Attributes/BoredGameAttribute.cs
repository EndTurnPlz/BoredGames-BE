using JetBrains.Annotations;

namespace BoredGames.Core.Game.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class BoredGameAttribute(string gameName) : Attribute
{
    public string Name { get; } = gameName;
}