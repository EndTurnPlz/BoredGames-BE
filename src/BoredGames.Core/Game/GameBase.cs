using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BoredGames.Core.Game;

public abstract class GameBase
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true, 
        Converters = { new JsonStringEnumConverter() }
    };
    
    private FrozenDictionary<string, GameAction> ActionMap { get; }
    protected readonly ImmutableList<Player> Players;
    protected int ViewNum { get; set; }

    protected GameBase(ImmutableList<Player> players)
    {
        Players = players;
        ActionMap = DiscoverActions();
    }

    public abstract IGameSnapshot GetSnapshot();
    public abstract bool HasEnded();

    public IGameActionResponse? ExecuteAction(string actionName, Player player, JsonElement? rawArgs = null)
    {
        var action = ActionMap.GetValueOrDefault(actionName) ?? throw new InvalidActionException();

        if (action.ArgsType is null) {
            if (rawArgs is not null) throw new BadActionArgsGameException();
            return action.Execute(this, player);
        }
        
        if (rawArgs is not {} args) throw new BadActionArgsGameException();

        var resolvedArgs = args.Deserialize(action.ArgsType, Options) as IGameActionArgs 
                           ?? throw new BadActionArgsGameException();
        
        return action.Execute(this, player, resolvedArgs);
    }

    private FrozenDictionary<string, GameAction> DiscoverActions()
    {
        var actionMap = new Dictionary<string, GameAction>();
        var gameType = GetType(); 

        var actionMethods = gameType
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(m => m.GetCustomAttribute<GameActionAttribute>() != null);

        foreach (var method in actionMethods)
        {
            // Throw new NotSupportedException("This method signature is not supported.");
            var actionName = method.GetCustomAttribute<GameActionAttribute>()!.Name;
            actionMap.Add(actionName, GameAction.Create(method, this));
        }

        return actionMap.ToFrozenDictionary();
    }

    // // Helper method to build the correct Action<> or Func<> type
    // private static Type GetDelegateTypeForMethod(MethodInfo method)
    // {
    //     var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToList();
    //
    //     if (method.ReturnType == typeof(void))
    //     {
    //         // e.g., Action<Player, PlaceTileArgs>
    //         return Type.GetType($"System.Action`{parameterTypes.Count}")!.MakeGenericType(parameterTypes.ToArray());
    //     }
    //     else
    //     {
    //         // e.g., Func<Player, DrawCardArgs, DrawCardResponse>
    //         parameterTypes.Add(method.ReturnType);
    //         return Type.GetType($"System.Func`{parameterTypes.Count}")!.MakeGenericType(parameterTypes.ToArray());
    //     }
    // }
}

public interface IGameSnapshot;

public interface IGameActionArgs;

public interface IGameActionResponse;

public enum GameTypes
{
    Apologies
}