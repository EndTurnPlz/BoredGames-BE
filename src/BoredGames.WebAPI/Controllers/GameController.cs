using BoredGames.Common.Room;
using Microsoft.AspNetCore.Mvc;

namespace BoredGames.Controllers;

[ApiController]
[Route("game/{roomId:guid}")]
public class GameController : ControllerBase
{
    [Produces("application/json")]
    [HttpPost("start")]
    public ActionResult StartGame([FromRoute] Guid roomId, [FromHeader(Name = "X-Player-Key")] Guid playerId)
    {
        try {
            var room = RoomManager.GetRoom(roomId);
            room.StartGame(playerId);
        } catch (RoomException ex) {
            return BadRequest(ex.Message);
        }
        return Ok();
    }
    
    [Produces("application/json")]
    [HttpPost("action")]
    public ActionResult GameAction([FromRoute] Guid roomId, [FromHeader(Name = "X-Player-Key")] Guid playerId, 
        [FromQuery] string action, [FromBody] object? actionArgs)
    {
        try {
            var room = RoomManager.GetRoom(roomId);
            room.StartGame(playerId);
        } catch (RoomException ex) {
            return BadRequest(ex.Message);
        }
        return Ok();
    }
}