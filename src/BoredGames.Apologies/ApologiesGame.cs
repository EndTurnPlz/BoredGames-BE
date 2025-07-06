using BoredGames.Apologies.Board;
using BoredGames.Apologies.EndpointObjects;
using BoredGames.Apologies.Deck;
using BoredGames.Common;
using BoredGames.Common.Exceptions;

namespace BoredGames.Apologies;

public sealed class ApologiesGame : AbstractGame
{
    private readonly CardDeck _cardDeck = new();
    private readonly GameBoard _gameBoard = new();
    private readonly List<Player> _players = [];
    private Phase GamePhase { get; set; } = Phase.Lobby;
    private MovePawnRequest? _lastCompletedMove = null;

    public ApologiesGame(Player host) : base(host)
    {
        JoinGame(host);
    }
    
    public override void JoinGame(Player player)
    {
        if (GamePhase != Phase.Lobby) throw new JoinGameException("Game already started");
        if (_players.Contains(player)) throw new JoinGameException("Player already joined");
        if (_players.Count == 4) throw new JoinGameException("Game is full");
        
        _players.Add(player);
        player.Game = this;
        CurrentView += 1;
    }
    
    public override void LeaveGame(Player player)
    {
        if (GamePhase != Phase.Lobby) return;
        if (!_players.Contains(player)) return;
        _players.Remove(player);

        if (_players.Count == 0) return;
        if (player == Host) Host = _players[0];
    }

    public override bool StartGame(Player player)
    {
        if (player != Host) return false;
        if (_players.Count < 4) return false;
        AdvanceGamePhase();
        CurrentView += 1;
        return true;
    }
    
    private bool IsCorrectPlayerDrawing(Player player)
    {
        var playerIndex = _players.IndexOf(player);
        return GamePhase switch
        {
            Phase.P1Draw => playerIndex == 0,
            Phase.P2Draw => playerIndex == 1,
            Phase.P3Draw => playerIndex == 2,
            Phase.P4Draw => playerIndex == 3,
            _ => false
        };
    }
    
    private bool IsCorrectPlayerMoving(Player player)
    {
        var playerIndex = _players.IndexOf(player);
        return GamePhase switch
        {
            Phase.P1Move => playerIndex == 0,
            Phase.P2Move => playerIndex == 1,
            Phase.P3Move => playerIndex == 2,
            Phase.P4Move => playerIndex == 3,
            _ => false
        };
    }

    public DrawCardResponse? DrawCard(Player player)
    {
        if (!IsCorrectPlayerDrawing(player)) return null;

        var lastDrawn = _cardDeck.DrawCard();
        var validMoves = _gameBoard.GetValidMovesForPlayer(_players.IndexOf(player), lastDrawn);
        
        AdvanceGamePhase(validMoves.Count == 0);
        CurrentView += 1;
        
        return new DrawCardResponse(CurrentView, (int)lastDrawn, validMoves);
    }

    public bool MovePawn(MovePawnRequest req, Player player)
    {
        // make sure SplitMove is set only if the last drawn card is a 7
        if (!IsCorrectPlayerMoving(player)) return false;
        
        var playerNum = _players.IndexOf(player);
        
        if (req.SplitMove is { } splitMove)
        {
            if (_cardDeck.LastDrawn != CardDeck.CardTypes.Seven) return false;
            if (!_gameBoard.TryExecuteSplitMove(req.Move, splitMove, playerNum)) return false;
        }
        else
        {
            if (!_gameBoard.TryExecuteMovePawn(req.Move, _cardDeck.LastDrawn, playerNum)) return false;
        }
        _gameBoard.ExecuteAnyAvailableSlides();
        
        AdvanceGamePhase();
        CurrentView += 1;
        return true;
    }

    public PullGameStateResponse PullCurrentState()
    {
        return new PullGameStateResponse(
            CurrentView,
            (int)GamePhase,
            (int)_cardDeck.LastDrawn,
            _lastCompletedMove,
            _players.IndexOf(Host),
            _players.Select(p => p.Username),
            _players.Select(p => p.IsConnected),
            _gameBoard.PawnTiles.Select(playerTiles => 
                playerTiles.Select(
                    pawnTiles => pawnTiles.Name
                )
            )
        );
    }

    private void AdvanceGamePhase(bool noMoves = false)
    {
        var gameWon = Array.Exists(_gameBoard.PawnTiles, playerPawnTiles =>
            Array.TrueForAll(playerPawnTiles, pawnTile => pawnTile is HomeTile)
        );

        var drawAgain = _cardDeck.LastDrawn == CardDeck.CardTypes.Two;

        var nextGamePhase = gameWon ? Phase.End : GamePhase switch
        {
            Phase.Lobby => Phase.P1Draw,
            Phase.P1Draw => noMoves ? Phase.P2Draw : Phase.P1Move,
            Phase.P1Move => drawAgain ? Phase.P1Draw : Phase.P2Draw,
            Phase.P2Draw => noMoves ? Phase.P3Draw : Phase.P2Move,
            Phase.P2Move => drawAgain ? Phase.P2Draw : Phase.P3Draw,
            Phase.P3Draw => noMoves ? Phase.P4Draw : Phase.P3Move,
            Phase.P3Move => drawAgain ? Phase.P3Draw : Phase.P4Draw,
            Phase.P4Draw => noMoves ? Phase.P1Draw : Phase.P4Move,
            Phase.P4Move => drawAgain ? Phase.P4Draw : Phase.P1Draw,
            _ => GamePhase
        };
        
        GamePhase = nextGamePhase;
    }

    private enum Phase
    {
        P1Draw, P1Move,
        P2Draw, P2Move,
        P3Draw, P3Move,
        P4Draw, P4Move,
        Lobby,
        End,
    }
}