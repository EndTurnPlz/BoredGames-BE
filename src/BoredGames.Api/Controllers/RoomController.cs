using System.Text.Json;
using System.Text.Json.Serialization;
using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Core.Room;
using BoredGames.Models;
using BoredGames.Services;
using Microsoft.AspNetCore.Mvc;

namespace BoredGames.Controllers;

[ApiController]
[Route("room")]
public class RoomController(RoomManager roomManager) : ControllerBase
{
    private static readonly JsonSerializerOptions SnapshotSerializerOpts = new()
    {
        TypeInfoResolver = new GameTypeInfoResolver(),
        Converters = { new JsonStringEnumConverter() }
    };

    [Produces("application/json")]
    [HttpPost("create")]
    public ActionResult CreateRoom([FromBody] string playerName, [FromQuery] string gameType)
    {
        Player player = new(out var playerId)
        {
            Username = playerName
        };
        
        var ok = Enum.TryParse<GameTypes>(gameType, out var resolvedType);
        if (!ok) {
            return NotFound("Invalid game type");
        }
        
        var roomId = roomManager.CreateRoom(resolvedType, player);
        return Ok(new { playerId, roomId } );
    }

    [Produces("application/json")]
    [HttpPost("{roomId:guid}/join")]
    public ActionResult JoinRoom([FromRoute] Guid roomId, [FromBody] string playerName)
    {
        Player player = new(out var playerId)
        {
            Username = playerName
        };
        
        try {
            roomManager.JoinRoom(roomId, player);
        } 
        catch (RoomException ex) {
            return BadRequest(ex.Message);
        }
        
        return Ok(new { playerId });
    }
    
    [Produces("application/json")]
    [HttpGet("{roomId:guid}/snapshot")]
    public ActionResult<RoomSnapshot> GetSnapshot([FromRoute] Guid roomId)
    {
        try {
            var room = roomManager.GetRoom(roomId);
            var snapshot = new RoomSnapshot(room.ViewNum, room.CurrentState, room.GetPlayerNames(), room.GetGameSnapshot());
            return Ok(snapshot);
        } 
        catch (RoomException ex) {
            return BadRequest(ex.Message);
        }
    }

    [Produces("text/event-stream")]
    [HttpGet("{roomId:guid}/stream")]
    public async Task ConnectToRoom([FromRoute] Guid roomId, 
        [FromQuery] Guid playerId, 
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.ContentType = "text/event-stream";

        try {
            var room = roomManager.GetRoom(roomId);
            room.RegisterPlayerConnected(playerId);
            while (!cancellationToken.IsCancellationRequested) {
                var snapshot = roomManager.GetRoomSnapshot(room);
                var sseData = $"data: {JsonSerializer.Serialize(snapshot, SnapshotSerializerOpts)}\n\n";
                
                await Response.WriteAsync(sseData, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                await Task.Delay(250, cancellationToken);
            }
        } 
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException) {
            try {
                var room = roomManager.GetRoom(roomId);
                room.RegisterPlayerDisconnected(playerId);
            }
            finally {
                Response.Body.Close();
            }
        } 
        catch (Exception ex) when (ex is RoomException) {
            Response.StatusCode = 404;
        }
    }
}