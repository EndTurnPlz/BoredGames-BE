using BoredGames.Common;
using Microsoft.AspNetCore.Mvc;

namespace BoredGames.Controllers;

[ApiController]
[Route("")]
public class PlayerController : ControllerBase
{
    private static readonly Dictionary<Guid, AbstractGame> Lobbies = new();

    [Produces("application/json")]
    [HttpPut("createGame")]
    public ActionResult CreateGame([FromBody] string playerName)
    {
        var lobbyId = Guid.NewGuid();
        Player player = new(out var playerId)
        {
            Username = playerName
        };
        Lobbies[lobbyId] = GameFactory.CreateNewGame(GameTypes.Apologies, player);
        return Ok(new { playerId, lobbyId });
    }

    [Produces("application/json")]
    [HttpPut("joinGame")]
    public ActionResult JoinGame([FromQuery] Guid lobbyId, [FromBody] string playerName)
    {
        if (!Lobbies.TryGetValue(lobbyId, out var game)) return NotFound("Lobby not found");
        Player player = new(out var playerId)
        {
            Username = playerName
        };
        game.JoinGame(player);
        return Ok(new { playerId });
    }

    [Produces("application/json")]
    [HttpPut("{playerId:guid}/startGame")]
    public ActionResult StartGame([FromRoute] Guid playerId, [FromBody] Guid lobbyId)
    {
        if (!Lobbies.TryGetValue(lobbyId, out var value)) return NotFound();
        value.StartGame(Player.GetPlayer(playerId)!);
        Lobbies.Remove(lobbyId);
        return Ok();
    }

    [Produces("text/event-stream")]
    [HttpGet("{playerId:guid}/gameViewStream")]
    public async Task GameViewStream([FromRoute] Guid playerId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.ContentType = "text/event-stream";
        
        if (Player.GetPlayer(playerId) is not { } player) 
        {
            Response.StatusCode = 404;
            return;
        }

        try
        {
            player.IsConnected = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                var viewNum = player.Game.CurrentView;
                var sseData = $"data: {viewNum}\n\n";
                await Response.WriteAsync(sseData, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                await Task.Delay(250, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException || 
                                   ex is InvalidOperationException)
        {
            player.IsConnected = false;
        }
    }
}