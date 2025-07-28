using System.Collections.Immutable;
using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Games.Apologies.Board;
using BoredGames.Games.Apologies.Deck;
using BoredGames.Games.Apologies.Models;
using JetBrains.Annotations;

namespace BoredGames.Games.Apologies;

public sealed class ApologiesGame(ImmutableList<Player> players) : GameBase(players)
{

    private readonly CardDeck _cardDeck = new();
    private readonly GameBoard _gameBoard = new();
    private State GameState { get; set; } = State.P1Draw;
    private ActionArgs.MovePawnArgs? _lastCompletedMove;
    
    private readonly GameStats _stats = new();
        
    public override bool HasEnded() => GameState is State.End;
    
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]    
    public enum State
    {
        P1Draw, P1Move,
        P2Draw, P2Move,
        P3Draw, P3Move,
        P4Draw, P4Move,
        End
    }
    
    private class GameStats
    {
        private readonly DateTime _gameStartTimestamp = DateTime.Now;
        private DateTime? _gameEndTimestamp;
        private int[] PlayerPawnsKilled { get; } = Enumerable.Repeat(0, 4).ToArray();
        private int[] PlayerMovesMade { get; } = Enumerable.Repeat(0, 4).ToArray();
        public void LogGameEnd()
        {
            _gameEndTimestamp = DateTime.Now;
        }

        public void LogPlayerMove(int playerIndex, int pawnsKilled)
        {
            PlayerPawnsKilled[playerIndex] += pawnsKilled;
            PlayerMovesMade[playerIndex]++;
        }

        public GenericComponents.GameStats GetStats()
        {
            var timeSpan = (_gameEndTimestamp ?? DateTime.Now) - _gameStartTimestamp;
            return new GenericComponents.GameStats(PlayerMovesMade, PlayerPawnsKilled, (int)timeSpan.TotalSeconds);
        }
    }

    public override ApologiesSnapshot GetSnapshot()
    {
        var turnOrder = Players.Select(p => p.Username).ToArray();
        var pieces = _gameBoard.PawnTiles.Select(playerTiles => playerTiles.Select(pawnTiles => pawnTiles.Name));
        
        return new ApologiesSnapshot(GameState, _cardDeck.LastDrawn, _lastCompletedMove, 
            _stats.GetStats(), turnOrder, pieces);
    }

    [GameAction("draw")]
    private ActionResponses.DrawCardResponse DrawCardAction(Player player)
    {
        var playerIndex = Players.IndexOf(player);
        var isCorrectPlayerDrawing = playerIndex * 2 == (int)GameState; 
        if (!isCorrectPlayerDrawing) throw new InvalidPlayerException();
        
        var lastDrawn = _cardDeck.DrawCard();
        var validMoves = _gameBoard.GetValidMovesForPlayer(Players.IndexOf(player), lastDrawn);
        
        AdvanceGamePhase(validMoves.Count == 0);
        ViewNum++;
        
        return new ActionResponses.DrawCardResponse((int)lastDrawn, validMoves);
    }

    [GameAction("move")]
    private void MovePawnAction(Player player, ActionArgs.MovePawnArgs req)
    {
        var playerIndex = Players.IndexOf(player);
        var isCorrectPlayerMoving = playerIndex * 2 + 1 == (int)GameState; 
        if (!isCorrectPlayerMoving) throw new InvalidPlayerException();
        
        var pawnTilesBeforeMove = _gameBoard.PawnTiles
            .Select(playerTiles => playerTiles.ToArray())
            .ToArray();
        
        // make sure SplitMove is set only if the last drawn card is a 7
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
            killedPawns += _gameBoard.PawnTiles[i].Count(t => t is StartTile) 
                           - pawnTilesBeforeMove[i].Count(t => t is StartTile);
        }
        _stats.LogPlayerMove(playerIndex, killedPawns);
        
        AdvanceGamePhase();
        if (HasEnded()) _stats.LogGameEnd();
        
        _lastCompletedMove = req;
        ViewNum++;
    }

    private void AdvanceGamePhase(bool noMoves = false)
    {
        if (GameState is State.End) return;

        var isDrawState = (int)GameState % 2 == 0;
        var isMoveState = (int)GameState % 2 == 1;
        
        var nextGameState = (State)(((int)GameState + 1) % 8);

        var drawAgain = _cardDeck.LastDrawn == CardDeck.CardTypes.Two;
        if (drawAgain && isMoveState) {
            nextGameState = (State)(((int)GameState - 1) % 8);
        }

        if (noMoves && isDrawState) {
            nextGameState = (State)(((int)GameState + 2) % 8);
        }
        
        if (_gameBoard.PlayerExistsWithAllPawnsHome) {
            nextGameState = State.End;
        }
        
        GameState = nextGameState;
    }
}