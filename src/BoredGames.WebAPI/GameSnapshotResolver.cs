using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BoredGames.Common.Game;

namespace BoredGames;

public class GameSnapshotResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var jsonTypeInfo = base.GetTypeInfo(type, options);

        // Check if the type is our base interface
        if (jsonTypeInfo.Type != typeof(IGameSnapshot)) return jsonTypeInfo;
        jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions();

        // Use reflection to find all types that implement the interface
        var implementingTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => typeof(IGameSnapshot).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

        foreach (var derivedType in implementingTypes)
        {
            var typeDiscriminator = derivedType.Name.Replace("Snapshot", "");
            jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(
                new JsonDerivedType(derivedType, typeDiscriminator)
            );
        }

        return jsonTypeInfo;
    }
}