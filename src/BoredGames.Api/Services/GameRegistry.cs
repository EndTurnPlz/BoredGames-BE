using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Core.Game.Attributes;
using JetBrains.Annotations;
using GameConstructor = System.Func<BoredGames.Core.Game.IGameConfig, 
                                    System.Collections.Immutable.ImmutableList<BoredGames.Core.Player>, 
                                    BoredGames.Core.Game.GameBase>;

namespace BoredGames.Services;

public sealed class GameRegistry : IDisposable
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public record GameInfoEntry
    {
        public required Type GameType { get; init; } 
        public required string Name { get; init; }
        public required int MinPlayers { get; init; }
        public required int MaxPlayers { get; init; }
        public required Type ConfigType { get; init; }
        public required GameConstructor Constructor { get; init; }
    }
    
    private readonly CancellationTokenSource _tickerCts = new();
    
    public readonly FrozenDictionary<string, GameInfoEntry> ByName;
    public GameRegistry()
    {
        Console.WriteLine("Scanning for games...");
        
        var appBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var gameAssemblies = DiscoverGameAssemblies(appBaseDirectory);
        var gameTypes = FindGameTypesInAssemblies(gameAssemblies);
        
        Dictionary<string, GameInfoEntry> byName = new();
        
        // Gather info on discovered games
        foreach (var gameType in gameTypes) {
            var gameName = gameType.GetCustomAttribute<BoredGameAttribute>()?.Name ?? 
                               throw new InvalidOperationException($"Game '{gameType.FullName}' is ");

            if (byName.ContainsKey(gameName)) {
                throw new InvalidOperationException($"Duplicate game name for '{gameType.FullName}'");
            }
            
            var gamePlayerCountInfo = gameType.GetCustomAttribute<GamePlayerCountAttribute>() ?? 
                           throw new InvalidOperationException($"Game '{gameType.FullName}' is ");

            var configType = gameType
                .GetConstructors()
                .Single()
                .GetParameters()
                .Select(p => p.ParameterType)
                .Single(arg => arg.IsAssignableTo(typeof(IGameConfig)));

            var gameInfoEntry = new GameInfoEntry
            {
                Name = gameName,
                MinPlayers = gamePlayerCountInfo.MinPlayers,
                MaxPlayers = gamePlayerCountInfo.MaxPlayers,
                GameType = gameType,
                Constructor = BuildGameConstructorLambda(gameType, configType),
                ConfigType = configType
            };
            
            byName.Add(gameName, gameInfoEntry);
            Console.WriteLine($"\tRegistered Game: {gameName} - {gameType}");
        }

        ByName = byName.ToFrozenDictionary();
    }

    private static IEnumerable<Type> FindGameTypesInAssemblies(List<Assembly> gameAssemblies)
    {
        var gameTypes = gameAssemblies
            .SelectMany(assembly => 
            {
                try {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex) {
                    Console.WriteLine($"[ERROR] Error loading types from assembly '{assembly.FullName}': {ex.Message}");
                    foreach (var loaderEx in ex.LoaderExceptions)
                    {
                        Console.WriteLine($"Loader Exception: {loaderEx?.Message}");
                    }
                    return [];
                }
                catch (Exception ex) {
                    Console.WriteLine($"[ERROR] General error getting types from assembly '{assembly.FullName}': {ex.Message}");
                    return [];
                }
            })
            .Where(t =>
                typeof(GameBase).IsAssignableFrom(t) &&
                t is { IsInterface: false, IsAbstract: false, IsPublic: true }
            );
        return gameTypes;
    }

    private List<Assembly> DiscoverGameAssemblies(string appBaseDirectory)
    {
        const string gameAssemblyFilter = "BoredGames.Games.*.dll";
        List<Assembly> gameAssemblies = [];
        var loadedAssemblyFullNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        var dllFiles = Directory.GetFiles(appBaseDirectory, gameAssemblyFilter, SearchOption.TopDirectoryOnly);

        foreach (var dllPath in dllFiles) {
            try {
                var assemblyName = AssemblyName.GetAssemblyName(dllPath);
                if (!loadedAssemblyFullNames.Add(assemblyName.FullName)) continue;

                var loadedAssembly = AppDomain.CurrentDomain.Load(assemblyName);
                gameAssemblies.Add(loadedAssembly);
            }
            catch (BadImageFormatException) {
                /* Ignore non-.NET DLLs */
            }
            catch (FileLoadException ex) {
                Console.WriteLine($"[Warning] Could not load '{Path.GetFileName(dllPath)}': {ex.Message}");
            }
            catch (Exception ex) {
                Console.WriteLine($"[Error] Unexpected issue with '{Path.GetFileName(dllPath)}': {ex.Message}");
            }
        }
        
        return gameAssemblies;
    }

    private GameConstructor BuildGameConstructorLambda(Type gameType, Type configType)
    {
        var constructorInfo = gameType.GetConstructors().SingleOrDefault();

        if (constructorInfo is null) {
            throw new InvalidOperationException($"{gameType} does not have a public constructor.");
        }
                
        // Use Expression Trees to create a fast, compiled delegate for the constructor.
        var configParam = Expression.Parameter(typeof(IGameConfig), "cfg");
        var playersParam = Expression.Parameter(typeof(ImmutableList<Player>), "p");
                
        var newExpression = Expression.New(constructorInfo, Expression.Convert(configParam, configType), playersParam);
                
        var exp = Expression.Lambda<GameConstructor>(newExpression, configParam, playersParam);
        return exp.Compile();
    }
    
    public void Dispose()
    {
        _tickerCts.Cancel();
        _tickerCts.Dispose();
    }
}