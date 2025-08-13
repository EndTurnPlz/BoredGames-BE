using JetBrains.Annotations;

namespace BoredGames.Core.Game;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors)]
public interface IGameSnapshot
{
    public IEnumerable<string> TurnOrder { get; init; }
};

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors)]
public interface IGameActionArgs;
public interface IGameActionResponse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors)]
public interface IGameConfig
{
    public bool ShuffleTurnOrder { get; init; }
};