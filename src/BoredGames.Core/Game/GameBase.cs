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
        if (HasEnded()) throw new InvalidOperationException("Game has ended.");
        
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
}

public interface IGameSnapshot;

public interface IGameActionArgs;

public interface IGameActionResponse;

public enum GameTypes
{
    Apologies,
    UpsAndDowns
}