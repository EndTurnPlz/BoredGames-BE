using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BoredGames.Core.Game.Attributes;

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

    public abstract IGameSnapshot GetSnapshot(Player player);
    public abstract bool HasEnded();

    public void ExecuteAction(string actionName, Player player, JsonElement? rawArgs = null)
    {
        if (HasEnded()) throw new InvalidOperationException("Game has ended.");
        
        var action = ActionMap.GetValueOrDefault(actionName) ?? throw new InvalidActionException();

        if (action.ArgsType is null) {
            if (rawArgs is not null) throw new BadActionArgsGameException();
            action.Execute(this, player);
            return;
        }
        
        if (rawArgs is not {} args) throw new BadActionArgsGameException();

        var resolvedArgs = args.Deserialize(action.ArgsType!, Options) as IGameActionArgs 
                           ?? throw new BadActionArgsGameException();
        
        action.Execute(this, player, resolvedArgs);
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
            var actionName = method.GetCustomAttribute<GameActionAttribute>()!.Name;
            actionMap.Add(actionName, GameAction.Create(method, this));
        }

        return actionMap.ToFrozenDictionary();
    }
}