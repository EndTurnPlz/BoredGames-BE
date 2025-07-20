using System.Collections.Frozen;
using System.Collections.Immutable;
using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Games.Apologies.Board;
using BoredGames.Games.Apologies.Deck;
using BoredGames.Games.Apologies.Models;

namespace BoredGames.Games.Apologies;

public sealed class ApologiesGame : GameBase
{

    private readonly CardDeck _cardDeck = new();
    private readonly GameBoard _gameBoard = new();
    private State GameState { get; set; } = State.P1Draw;
    private ActionArgs.MovePawnArgs? _lastCompletedMove;
    private readonly int[] _playerStatsPawnsKilled = Enumerable.Repeat(0, 4).ToArray();
    private readonly int[] _playerStatsMovesMade = Enumerable.Repeat(0, 4).ToArray();
    private readonly long _gameStartTimestamp = DateTime.Now.Ticks;
    public override bool HasEnded() => GameState == State.End;
    
    protected override FrozenDictionary<Type, GameAction> ActionMap { get; }
    
    public enum State
    {
        P1Draw, P1Move,
        P2Draw, P2Move,
        P3Draw, P3Move,
        P4Draw, P4Move,
        End
    }
    
    public ApologiesGame(ImmutableList<Player> players) : base(players)
    {
        ActionMap = new List<GameAction> {
            GameAction.Create<ActionArgs.DrawCardArgs>(DrawCard_Action),
            GameAction.Create<ActionArgs.MovePawnArgs>(MovePawn_Action),
            GameAction.Create<ActionArgs.GetStatsArgs>(GetStats_Action)
        }.ToFrozenDictionary(action => action.ArgType, action => action);
    }
    
    public override ApologiesSnapshot GetSnapshot()
    {
        return new ApologiesSnapshot(ViewNum, GameState, _cardDeck.LastDrawn, _lastCompletedMove,
            Players.Select(p => p.Username).ToArray(), Players.Select(p => p.IsConnected),
            _gameBoard.PawnTiles.Select(playerTiles => playerTiles.Select(pawnTiles => pawnTiles.Name)));
    }

    private ActionResponses.DrawCardResponse DrawCard_Action(ActionArgs.DrawCardArgs _, Player player)
    {
        if (!IsCorrectPlayerDrawing(player)) throw new InvalidPlayerException();

        var lastDrawn = _cardDeck.DrawCard();
        var validMoves = _gameBoard.GetValidMovesForPlayer(Players.IndexOf(player), lastDrawn);
        
        AdvanceGamePhase(validMoves.Count == 0);
        ViewNum += 1;
        
        return new ActionResponses.DrawCardResponse((int)lastDrawn, validMoves);
    }

    private void MovePawn_Action(ActionArgs.MovePawnArgs req, Player player)
    {
        // make sure SplitMove is set only if the last drawn card is a 7
        if (!IsCorrectPlayerMoving(player)) throw new InvalidPlayerException();
        
        var playerIndex = Players.IndexOf(player);
        
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

    private ActionResponses.EndgameStatsResponse GetStats_Action(ActionArgs.GetStatsArgs _)
    {
        return new ActionResponses.EndgameStatsResponse(_playerStatsMovesMade, _playerStatsPawnsKilled, 
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
        var playerIndex = Players.IndexOf(player);
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
        var playerIndex = Players.IndexOf(player);
        return GameState switch {
            State.P1Move => playerIndex == 0,
            State.P2Move => playerIndex == 1,
            State.P3Move => playerIndex == 2,
            State.P4Move => playerIndex == 3,
            _ => false
        };
    }
}