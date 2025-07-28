using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BoredGames.Core.Game;

namespace BoredGames;

public class GameTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var jsonTypeInfo = base.GetTypeInfo(type, options);

        // Handle IGameSnapshot Polymorphism 
        if (jsonTypeInfo.Type == typeof(IGameSnapshot))
        {
            ConfigureResponseTypePolymorphism<IGameSnapshot>(jsonTypeInfo);
        }
        
        // Handle IGameResponseArgs Polymorphism
        if (jsonTypeInfo.Type == typeof(IGameActionResponse))
        {
            ConfigureResponseTypePolymorphism<IGameActionResponse>(jsonTypeInfo);
        }

        return jsonTypeInfo;
    }
    
    private static void ConfigureResponseTypePolymorphism<TBase>(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "$type",
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
        };

        // Use reflection to find all concrete types that implement the base interface.
        var derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => typeof(TBase).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

        foreach (var derivedType in derivedTypes)
        {
            // Convention: Use the lowercase class name as the discriminator string.
            var typeDiscriminator = derivedType.Name.ToLowerInvariant();
            jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(
                new JsonDerivedType(derivedType, typeDiscriminator)
            );
        }
    }
}