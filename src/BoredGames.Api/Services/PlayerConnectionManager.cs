using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using BoredGames.Core.Room;

namespace BoredGames.Services;

public sealed class PlayerConnectionManager : IDisposable
{
    
    private static readonly JsonSerializerOptions SnapshotSerializerOpts = new()
    {
        TypeInfoResolver = new GameTypeInfoResolver(),
        Converters = { new JsonStringEnumConverter() }
    };
    
    private readonly ConcurrentDictionary<Guid, HttpResponse> _connections = [];
    private readonly CancellationTokenSource _tickerCts = new();
    
    public void Dispose()
    {
        _tickerCts.Cancel();
        _tickerCts.Dispose();
    }
    
    public bool AddConnection(Guid playerId, HttpResponse response)
    {
        return _connections.TryAdd(playerId, response);
    }

    public void RemoveConnection(Guid playerId)
    {
        _connections.TryRemove(playerId, out _);
    }

    public async Task PushSnapshotToPlayersAsync(IEnumerable<Guid> playerIds, RoomSnapshot snapshot)
    {
        foreach (var playerId in playerIds) {
            if (!_connections.TryGetValue(playerId, out var response)) continue;
            try {
                // The 'data:' prefix is part of the SSE protocol.
                var sseMessage = $"data: {JsonSerializer.Serialize(snapshot, SnapshotSerializerOpts)}\n\n";
                await response.WriteAsync(sseMessage);
                await response.Body.FlushAsync();
            }
            catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException) {
                RemoveConnection(playerId);
            }
        }
        
    }
}
