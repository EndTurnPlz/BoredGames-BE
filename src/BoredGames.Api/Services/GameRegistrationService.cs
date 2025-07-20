using BoredGames.Core.Game;
using System.Reflection;

namespace BoredGames.Services;

public static class GameConfigServiceExtensions
{
   public static IServiceCollection AddGameConfigs(this IServiceCollection services)
    {
        var appBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        const string gameAssemblyFilter = "BoredGames.Games.*.dll";

        Console.WriteLine($"Scanning for game assemblies...");

        var allRelevantAssemblies = new List<Assembly>();
        var loadedAssemblyFullNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Scan for game assemblies
        var dllFiles = Directory.GetFiles(appBaseDirectory, gameAssemblyFilter, SearchOption.TopDirectoryOnly);

        foreach (var dllPath in dllFiles) {
            try {
                var assemblyName = AssemblyName.GetAssemblyName(dllPath);
                if (!loadedAssemblyFullNames.Add(assemblyName.FullName)) continue;
                
                var loadedAssembly = AppDomain.CurrentDomain.Load(assemblyName);
                allRelevantAssemblies.Add(loadedAssembly);
            }
            catch (BadImageFormatException) { /* Ignore non-.NET DLLs */ }
            catch (FileLoadException ex) { Console.WriteLine($"[Warning] Could not load '{Path.GetFileName(dllPath)}': {ex.Message}"); }
            catch (Exception ex) { Console.WriteLine($"[Error] Unexpected issue with '{Path.GetFileName(dllPath)}': {ex.Message}"); }
        }

        // 3. Now, perform the reflection scan across all *filtered* and discovered/loaded assemblies.
        var configTypes = allRelevantAssemblies
            .SelectMany(assembly => 
            {
                try {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex) {
                    Console.WriteLine($"[ERROR] Error loading types from assembly '{assembly.FullName}': {ex.Message}");
                    foreach (var loaderEx in ex.LoaderExceptions)
                    {
                        Console.WriteLine($"  Loader Exception: {loaderEx?.Message}");
                    }
                    return [];
                }
                catch (Exception ex) {
                    Console.WriteLine($"[ERROR] General error getting types from assembly '{assembly.FullName}': {ex.Message}");
                    return [];
                }
            })
            .Where(t =>
                typeof(IGameConfig).IsAssignableFrom(t) &&
                t is { IsInterface: false, IsAbstract: false, IsPublic: true }
            );

        foreach (var type in configTypes) {
            services.AddSingleton(typeof(IGameConfig), type);
            Console.WriteLine($"Registered IGameConfig implementation: {type.FullName}");
        }

        return services;
    }
}