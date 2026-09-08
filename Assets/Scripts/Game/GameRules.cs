namespace GhostArena
{
    public enum WinRule
    {
        SurviveTime,
        KillEnemies
    }

    public enum LoseRule
    {
        PlayerDeath,
        TotalSpawnsExceeded
    }

    public enum GameState
    {
        Ready,
        Running,
        Paused,
        Finished
    }

    public enum GameResult
    {
        Victory,
        Defeat
    }
}
