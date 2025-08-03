using JetBrains.Annotations;

namespace BoredGames.Core.Game;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors)]
public interface IGameSnapshot
{
    public IEnumerable<string> TurnOrder { get; init; }
};

public interface IGameActionArgs;
public interface IGameActionResponse;
public interface IGameConfig;