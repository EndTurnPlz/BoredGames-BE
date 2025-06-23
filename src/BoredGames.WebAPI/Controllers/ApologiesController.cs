using Microsoft.AspNetCore.Mvc;
using BoredGames.GamesLib.Apologies;
using BoredGames.GamesLib.Common;

namespace BoredGames.Controllers;

[ApiController]
[Produces("application/json")]
[Route("/games/[controller]/{playerId:guid}")]
public class ApologiesController : ControllerBase
{
    [HttpGet("drawCard")]
    public IActionResult DrawCard([FromRoute] Guid playerId)
    {
        if (PlayerValidityErrors(playerId) is not null and var errResult)
        {
            return errResult;
        }
        
        var game = (ApologiesGame) Player.GetPlayer(playerId)!.Game;
        return Ok(game.DrawCard());
    }
    
    [HttpPost("movePawn")]
    public IActionResult MovePawn([FromRoute] Guid playerId)
    {
        if (PlayerValidityErrors(playerId) is not null and var errResult)
        {
            return errResult;
        }

        var game = (ApologiesGame)Player.GetPlayer(playerId)!.Game;
        return Ok(game.MovePawn());
    }
    
    [HttpGet("pullGameState")]
    public IActionResult FetchGameState([FromRoute] Guid playerId)
    {
        if (PlayerValidityErrors(playerId) is not null and var errResult)
        {
            return errResult;
        }
        
        var game = (ApologiesGame)Player.GetPlayer(playerId)!.Game;
        return Ok(game.GetCurrentState());
    }
    
    [HttpPut("playerHeartbeat")]
    public IActionResult PlayerHeartbeat([FromRoute] Guid playerId, [FromBody] int view)
    {
        if (PlayerValidityErrors(playerId) is not null and var errResult)
        {
            return errResult;
        }
        
        Player.GetPlayer(playerId)!.Heartbeat(view);
        return Ok();
    }

    private IActionResult? PlayerValidityErrors(Guid playerId)
    {
        if (Player.GetPlayer(playerId) is null)
        {
            return BadRequest("Invalid player id");
        }

        var client = Player.GetPlayer(playerId)!;
        if (client.Game is not ApologiesGame)
        {
            return BadRequest("Invalid player id");
        }
        
        if (!client.HasCurrentView()) return Conflict(client.Game.CurrentView);

        return null;
    }
}