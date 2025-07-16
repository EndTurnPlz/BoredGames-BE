using BoredGames.Apologies;
using BoredGames.Apologies.Models;
using BoredGames.Common;
using BoredGames.Common.Game;
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

        var game = (ApologiesGame)Player.GetPlayer(playerId)!.Game;
        if (game.DrawAction(Player.GetPlayer(playerId)!) is not { } moveList) 
            return Problem(statusCode: 422, detail: "Incorrect Player");

        return Ok(moveList);
    }

    [HttpPost("move")]
    public ActionResult MovePawn([FromRoute] Guid playerId, [FromBody] MovePawnArgs req)
    {
        if (PlayerValidityErrors(playerId) is { } errResult) return errResult;

        var game = (ApologiesGame)Player.GetPlayer(playerId)!.Game;
        if (!game.MoveAction(req, Player.GetPlayer(playerId)!)) return BadRequest("Invalid move");

        return Ok();
    }

    [HttpGet("stats")]
    public ActionResult<IGameSnapshot> GetStats([FromRoute] Guid playerId)
    {
        if (PlayerValidityErrors(playerId) is { } errResult) return errResult;

        var game = (ApologiesGame)Player.GetPlayer(playerId)!.Game;
        
        try {
            return Ok(game.GetStats());
        } catch (GameException ex) {
            return BadRequest(ex.Message);
        }
    }

    private BadRequestObjectResult? PlayerValidityErrors(Guid playerId)
    {
        if (Player.GetPlayer(playerId) is null) return BadRequest("Invalid player id");

        var client = Player.GetPlayer(playerId)!;
        if (client.Game is not ApologiesGame) return BadRequest("Invalid player id");
        if (!client.Game.HasStarted) return BadRequest("Game has not started yet");

        return null;
    }
}