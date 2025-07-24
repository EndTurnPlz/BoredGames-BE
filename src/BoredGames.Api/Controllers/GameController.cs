using BoredGames.Core.Game;
using BoredGames.Core.Room;
using BoredGames.Services;
using Microsoft.AspNetCore.Mvc;

namespace BoredGames.Controllers;

[ApiController]
[Route("game/{roomId:guid}")]
public class GameController(RoomManager roomManager) : ControllerBase
{
    [Produces("application/json")]
    [HttpPost("start")]
    public ActionResult StartGame([FromRoute] Guid roomId, [FromHeader(Name="X-Player-Key")] Guid playerId)
    {
        try {
            var room = roomManager.GetRoom(roomId);
            room.StartGame(playerId);
            return Ok();
        } catch (RoomException ex) {
            return BadRequest(ex.Message);
        }
    }
    
    [Produces("application/json")]
    [HttpPost("action")]
    public ActionResult GameAction([FromRoute] Guid roomId, [FromHeader(Name = "X-Player-Key")] Guid? playerId, 
        [FromBody] IGameActionArgs actionArgs)
    {
        try {
            var room = roomManager.GetRoom(roomId);
            return Ok(room.ExecuteGameAction(actionArgs, playerId));
        } catch (Exception ex) when (ex is RoomException or GameException) {
            return BadRequest(ex.Message);
        }
    }
}