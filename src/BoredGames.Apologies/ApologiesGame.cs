using BoredGames.Apologies.Board;
using BoredGames.Apologies.Deck;
using BoredGames.Apologies.Models;
using BoredGames.Common;
using BoredGames.Common.Game;

namespace BoredGames.Apologies;

public sealed class ApologiesGame(IEnumerable<Player> players) : GameBase(players)
{
    private readonly CardDeck _cardDeck = new();
    private readonly GameBoard _gameBoard = new();
    private State GameState { get; set; } = State.P1Draw;
    private MovePawnArgs? _lastCompletedMove;
    private readonly int[] _playerStatsPawnsKilled = Enumerable.Repeat(0, 4).ToArray();
    private readonly int[] _playerStatsMovesMade = Enumerable.Repeat(0, 4).ToArray();
    private readonly long _gameStartTimestamp = DateTime.Now.Ticks;
    public override bool HasEnded() => GameState == State.End;

    public enum State
    {
        P1Draw, P1Move,
        P2Draw, P2Move,
        P3Draw, P3Move,
        P4Draw, P4Move,
        End
    }

    public override ApologiesSnapshot GetSnapshot()
    {
        return new ApologiesSnapshot(ViewNum, GameState, _cardDeck.LastDrawn, _lastCompletedMove,
            Players.Select(p => p.Username).ToArray(), Players.Select(p => p.IsConnected),
            _gameBoard.PawnTiles.Select(playerTiles => playerTiles.Select(pawnTiles => pawnTiles.Name)));
    }

    public override IGameActionResponse? ExecuteAction(string actionType, Player? player = null, IGameActionArgs? args = null)
    {
        
        IReadOnlyDictionary<string, GameAction> actions = new Dictionary<string, GameAction>(StringComparer.OrdinalIgnoreCase)
        {
            ["Draw"] = GameAction.Create(DrawAction), 
            ["Move"] = GameAction.Create<MovePawnArgs>(MoveAction),
            ["Stats"] = GameAction.Create(GetStatsAction)
        };
        
        var action = actions.GetValueOrDefault(actionType) ?? throw new InvalidActionException();
        return action.Execute(player, args);
    }

    private DrawCardResponse DrawAction(Player player)
    {
        if (!IsCorrectPlayerDrawing(player)) throw new InvalidPlayerException();

        var lastDrawn = _cardDeck.DrawCard();
        var validMoves = _gameBoard.GetValidMovesForPlayer(Array.IndexOf(Players, player), lastDrawn);
        
        AdvanceGamePhase(validMoves.Count == 0);
        ViewNum += 1;
        
        return new DrawCardResponse(ViewNum, (int)lastDrawn, validMoves);
    }

    private void MoveAction(Player player, MovePawnArgs req)
    {
        // make sure SplitMove is set only if the last drawn card is a 7
        if (!IsCorrectPlayerMoving(player)) throw new InvalidPlayerException();
        
        var playerIndex = Array.IndexOf(Players, player);
        
        var pawnTilesBeforeMove = _gameBoard.PawnTiles
            .Select(playerTiles => playerTiles.ToArray())
            .ToArray();
        
        if (req.SplitMove is { } splitMove) {
            if (_cardDeck.LastDrawn != CardDeck.CardTypes.Seven ||
                !_gameBoard.TryExecuteSplitMove(req.Move, splitMove, playerIndex)) 
            {
                throw new InvalidMoveException();
            }

        } else if (!_gameBoard.TryExecuteMovePawn(req.Move, _cardDeck.LastDrawn, playerIndex)) {
            throw new InvalidMoveException();
        }
        
        _gameBoard.ExecuteAnyAvailableSlides();

        var killedPawns = 0;
        for (var i = 0; i < _gameBoard.PawnTiles.Length; i++) {
            if (i == playerIndex) continue;
            killedPawns += pawnTilesBeforeMove[i].Count(t => t is StartTile) 
                           - _gameBoard.PawnTiles[i].Count(t => t is StartTile);
        }
        _playerStatsPawnsKilled[playerIndex] += killedPawns;
        _playerStatsMovesMade[playerIndex]++;
        
        AdvanceGamePhase();
        _lastCompletedMove = req;
        ViewNum += 1;
    }

    private EndgameStatsResponse GetStatsAction()
    {
        return new EndgameStatsResponse(_playerStatsMovesMade, _playerStatsPawnsKilled, 
            DateTime.Now.Ticks - _gameStartTimestamp);
    }

    private void AdvanceGamePhase(bool noMoves = false)
    {
        var gameWon = Array.Exists(_gameBoard.PawnTiles, playerPawnTiles =>
            Array.TrueForAll(playerPawnTiles, pawnTile => pawnTile is HomeTile)
        );

        var drawAgain = _cardDeck.LastDrawn == CardDeck.CardTypes.Two;

        var nextGamePhase = gameWon ? State.End : GameState switch {
            State.P1Draw => noMoves ? State.P2Draw : State.P1Move,
            State.P1Move => drawAgain ? State.P1Draw : State.P2Draw,
            State.P2Draw => noMoves ? State.P3Draw : State.P2Move,
            State.P2Move => drawAgain ? State.P2Draw : State.P3Draw,
            State.P3Draw => noMoves ? State.P4Draw : State.P3Move,
            State.P3Move => drawAgain ? State.P3Draw : State.P4Draw,
            State.P4Draw => noMoves ? State.P1Draw : State.P4Move,
            State.P4Move => drawAgain ? State.P4Draw : State.P1Draw,
            _ => GameState
        };
        
        GameState = nextGamePhase;
    }
    
    private bool IsCorrectPlayerDrawing(Player player)
    {
        var playerIndex = Array.IndexOf(Players, player);
        return GameState switch 
        {
            State.P1Draw => playerIndex == 0,
            State.P2Draw => playerIndex == 1,
            State.P3Draw => playerIndex == 2,
            State.P4Draw => playerIndex == 3,
            _ => false
        };
    }
    
    private bool IsCorrectPlayerMoving(Player player)
    {
        var playerIndex = Array.IndexOf(Players, player);
        return GameState switch {
            State.P1Move => playerIndex == 0,
            State.P2Move => playerIndex == 1,
            State.P3Move => playerIndex == 2,
            State.P4Move => playerIndex == 3,
            _ => false
        };
    }
}