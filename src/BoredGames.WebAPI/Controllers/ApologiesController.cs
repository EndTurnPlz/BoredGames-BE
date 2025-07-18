using BoredGames.Apologies;
using BoredGames.Apologies.Models;
using BoredGames.Common;
using BoredGames.Common.Game;
using BoredGames.Common.Room;
using Microsoft.AspNetCore.Mvc;

namespace BoredGames.Controllers;

[ApiController]
[Produces("application/json")]
[Route("/games/[controller]/{playerId:guid}")]
public class ApologiesController : ControllerBase
{
    [HttpGet("draw")]
    public ActionResult<DrawCardResponse> DrawCard([FromRoute] Guid playerId)
    {
        if (PlayerValidityErrors(playerId) is { } errResult) return errResult;

        var game = (ApologiesGame)Player.GetPlayer(playerId)!.GameBase;
        if (game.DrawAction(Player.GetPlayer(playerId)!) is not { } moveList) 
            return Problem(statusCode: 422, detail: "Incorrect Player");

        return Ok(moveList);
    }

    [HttpPost("move")]
    public ActionResult MovePawn([FromRoute] Guid playerId, [FromBody] MovePawnArgs req)
    {
        if (PlayerValidityErrors(playerId) is { } errResult) return errResult;

        var game = (ApologiesGame)Player.GetPlayer(playerId)!.GameBase;
        if (!game.MoveAction(req, Player.GetPlayer(playerId)!)) return BadRequest("Invalid move");

        return Ok();
    }

    [Produces("application/json")]
    [HttpGet("{roomId:guid}/snapshot")]
    public ActionResult<ApologiesSnapshot> GetSnapshot([FromRoute] Guid roomId)
    {
        try {
            var room = RoomManager.GetRoom(roomId);
            return Ok(room.GetGameSnapshot());
        } 
        catch (RoomException ex) {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("stats")]
    public ActionResult<IGameSnapshot> GetStats([FromRoute] Guid playerId)
    {
        if (PlayerValidityErrors(playerId) is { } errResult) return errResult;

        var game = (ApologiesGame)Player.GetPlayer(playerId)!.GameBase;
        
        try {
            return Ok(game.GetStats());
        } catch (GameException ex) {
            return BadRequest(ex.Message);
        }
    }

    private BadRequestObjectResult? PlayerValidityErrors(Guid playerId)
    {
        if (Player.GetPlayer(playerId) is not {} client) return BadRequest("Invalid player id");
        if (client.GameBase is not ApologiesGame) return BadRequest("Invalid player id");
        
        return null;
    }
}