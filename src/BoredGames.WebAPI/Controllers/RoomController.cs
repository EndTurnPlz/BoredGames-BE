using System.Diagnostics.CodeAnalysis;
using BoredGames.Common;
using BoredGames.Common.Game;
using BoredGames.Common.Room;
using BoredGames.Models;
using Microsoft.AspNetCore.Mvc;

namespace BoredGames.Controllers;

[ApiController]
[Route("room")]
public class RoomController : ControllerBase
{

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
        
        var roomId = RoomManager.CreateRoom(resolvedType, player);
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
            RoomManager.JoinRoom(roomId, player);
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
            var room = RoomManager.GetRoom(roomId);
            var snapshot = new RoomSnapshot(room.CurrentState, room.GetPlayerNames(), room.GetGameSnapshot());
            return Ok(snapshot);
        } 
        catch (RoomException ex) {
            return BadRequest(ex.Message);
        }
    }

    [Produces("text/event-stream")]
    [HttpGet("{roomId:guid}/stream")]
    [SuppressMessage("ReSharper.DPA", "DPA0011: High execution time of MVC action", MessageId = "time: 148567ms")]
    public async Task ConnectToRoom([FromRoute] Guid roomId, 
        [FromQuery] Guid playerId, 
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.ContentType = "text/event-stream";

        try {
            var room = RoomManager.GetRoom(roomId);
            room.RegisterPlayerConnected(playerId);
            while (!cancellationToken.IsCancellationRequested) {
                var viewNum = RoomManager.GetRoomViewNum(roomId);
                var sseData = $"data: {viewNum}\n\n";
                
                await Response.WriteAsync(sseData, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                await Task.Delay(250, cancellationToken);
            }
        } 
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException) {
            var room = RoomManager.GetRoom(roomId);
            room.RegisterPlayerDisconnected(playerId);
        } 
        catch (Exception ex) when (ex is RoomException) {
            Response.StatusCode = 404;
        }
    }
}