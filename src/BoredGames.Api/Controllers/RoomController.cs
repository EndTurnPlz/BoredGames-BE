using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Core.Room;
using BoredGames.Services;
using Microsoft.AspNetCore.Mvc;

namespace BoredGames.Controllers;

[ApiController]
[Route("api/room")]
public class RoomController(RoomManager roomManager, PlayerConnectionManager playerConnectionManager) : ControllerBase
{

    [Produces("application/json")]
    [HttpPost("create")]
    public ActionResult CreateRoom([FromBody] string playerName, [FromQuery] string gameType)
    {
        var player = new Player(playerName);
        
        var ok = Enum.TryParse<GameTypes>(gameType, out var resolvedType);
        if (!ok) {
            return NotFound("Invalid game type");
        }
        
        var playerId = player.Id;
        var roomId = roomManager.CreateRoom(resolvedType, player);
        return Ok(new { playerId, roomId } );
    }

    [Produces("application/json")]
    [HttpPost("{roomId:guid}/join")]
    public ActionResult JoinRoom([FromRoute] Guid roomId, [FromBody] string playerName)
    {
        var player = new Player(playerName);
        
        try {
            roomManager.JoinRoom(roomId, player);
        } 
        catch (RoomException ex) {
            return BadRequest(ex.Message);
        }
        
        var playerId = player.Id;
        return Ok(new { playerId });
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
            var ok = playerConnectionManager.AddConnection(playerId, Response);
            if (!ok) {
                Response.StatusCode = 409;
                Response.Body.Close();
                return;
            }
            room.RegisterPlayerConnected(playerId);
            
            while (!cancellationToken.IsCancellationRequested) {
                const string sseHeartbeat = ": heartbeat\n\n";
                await Response.WriteAsync(sseHeartbeat, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }
        }
        catch (Exception ex) when (ex is RoomException) {
            Response.Body.Close();
            Response.StatusCode = 404;
        }
        finally {
            if (Response.StatusCode != 409) {
                playerConnectionManager.RemoveConnection(playerId);

                try {
                    var room = roomManager.GetRoom(roomId);
                    room.RegisterPlayerDisconnected(playerId);
                }
                catch (RoomException) {}
                finally {
                    Response.Body.Close();
                }
            }
        }
    }
}