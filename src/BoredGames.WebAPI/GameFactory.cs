using BoredGames.Apologies;
using BoredGames.Common;

namespace BoredGames;

public static class GameFactory
{
    public static AbstractGame CreateNewGame(GameTypes gameType, Player host)
    {
        AbstractGame newGame;
        switch (gameType)
        {
            case GameTypes.Apologies:
                newGame = new ApologiesGame(host);
                break;
            case GameTypes.NGameTypes:
            default:
                throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null);
        }

        host.Game = newGame;
        return newGame;
    }
}