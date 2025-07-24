using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using JetBrains.Annotations;

namespace BoredGames.Core.Game;

public abstract class GameBase
{
    private FrozenDictionary<Type, GameAction> ActionMap { get; }
    protected readonly ImmutableList<Player> Players;
    protected int ViewNum { get; set; }

    protected GameBase(ImmutableList<Player> players)
    {
        Players = players;
        ActionMap = DiscoverActions();
    }

    public abstract IGameSnapshot GetSnapshot();
    public abstract bool HasEnded();

    public IGameActionResponse? ExecuteAction(IGameActionArgs args, Player? player = null)
    {
        var action = ActionMap.GetValueOrDefault(args.GetType()) ?? throw new InvalidActionException();
        return action.Execute(args, player);
    }

    private FrozenDictionary<Type, GameAction> DiscoverActions()
    {
        var actionMap = new Dictionary<Type, GameAction>();
        var gameType = GetType(); // The concrete type, e.g., ApologiesGame

        var actionMethods = gameType
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(m => m.GetCustomAttribute<GameActionAttribute>() != null);

        // Pre-filter the factory methods for easier lookup
        var createMethods = typeof(GameAction).GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m is { Name: "Create", IsGenericMethod: true })
            .ToList();

        foreach (var method in actionMethods)
        {
            var parameters = method.GetParameters();
            var argsParam = parameters.Single(p => typeof(IGameActionArgs).IsAssignableFrom(p.ParameterType));
            var argsType = argsParam.ParameterType;
            
            if (argsParam == null)
            {
                throw new InvalidOperationException($"Method '{method.Name}' has [GameAction] but no parameter " +
                                                    $"that implements IGameActionArgs.");
            }

            // Enforce that the IGameActionArgs parameter is the first one.
            if (parameters.Length == 0 || parameters[0].ParameterType != argsType)
            {
                throw new InvalidOperationException(
                    $"Method '{method.Name}' is a [GameAction], but its first parameter is not the one" +
                    $" implementing IGameActionArgs. Convention requires the args parameter to be first."
                );
            }

            var hasPlayerParam = parameters.Any(p => p.ParameterType == typeof(Player));
            var returnsValue = method.ReturnType != typeof(void);

            var createMethodInfo = hasPlayerParam switch
            {
                true when returnsValue => createMethods.Single(m =>
                    m.GetParameters()[0].ParameterType.Name.StartsWith("Func") &&
                    m.GetParameters()[0].ParameterType.GetGenericArguments().Length == 3),
                false when returnsValue => createMethods.Single(m =>
                    m.GetParameters()[0].ParameterType.Name.StartsWith("Func") &&
                    m.GetParameters()[0].ParameterType.GetGenericArguments().Length == 2),
                true when !returnsValue => createMethods.Single(m =>
                    m.GetParameters()[0].ParameterType.Name.StartsWith("Action") &&
                    m.GetParameters()[0].ParameterType.GetGenericArguments().Length == 2),
                _ => throw new NotSupportedException(
                    $"The signature of action method '{method.Name}' is not supported.")
            };

            // Create a closed generic method (e.g., GameAction.Create<ApologiesGame, PlaceTileArgs>)
            var genericCreateMethod = createMethodInfo.MakeGenericMethod(argsType);

            // Dynamically build the correct delegate type to match the method's signature
            var delegateType = GetDelegateTypeForMethod(method);
            var actionDelegate = Delegate.CreateDelegate(delegateType, this, method);

            // Invoke the static Create method to get our GameAction wrapper
            var gameAction = (GameAction)genericCreateMethod.Invoke(null, [actionDelegate])!;

            actionMap.Add(argsType, gameAction);
        }

        return actionMap.ToFrozenDictionary();
    }

    // Helper method to build the correct Action<> or Func<> type
    private static Type GetDelegateTypeForMethod(MethodInfo method)
    {
        var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToList();

        if (method.ReturnType == typeof(void))
        {
            // e.g., Action<Player, PlaceTileArgs>
            return Type.GetType($"System.Action`{parameterTypes.Count}")!.MakeGenericType(parameterTypes.ToArray());
        }
        else
        {
            // e.g., Func<Player, DrawCardArgs, DrawCardResponse>
            parameterTypes.Add(method.ReturnType);
            return Type.GetType($"System.Func`{parameterTypes.Count}")!.MakeGenericType(parameterTypes.ToArray());
        }
    }
}

public interface IGameSnapshot;

public interface IGameActionArgs
{
    [UsedImplicitly] static abstract string ActionName { get; }
}

public interface IGameActionResponse;

public enum GameTypes
{
    Apologies
}