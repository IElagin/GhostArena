using System;

namespace GhostArena
{
    public static class ConditionFactory
    {
        public static IGameCondition CreateWin(
            WinRule rule,
            SessionStats stats,
            Health health,
            float surviveDuration,
            int killTarget)
        {
            switch (rule)
            {
                case WinRule.SurviveTime:
                    return new SurviveTimeCondition(stats, health, surviveDuration);

                case WinRule.KillEnemies:
                    return new KillEnemiesCondition(stats, killTarget);

                default:
                    throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown win rule.");
            }
        }

        public static IGameCondition CreateLose(
            LoseRule rule,
            SessionStats stats,
            Health health,
            int totalSpawnsLimit)
        {
            switch (rule)
            {
                case LoseRule.PlayerDeath:
                    return new PlayerDeathCondition(health);

                case LoseRule.TotalSpawnsExceeded:
                    return new TotalSpawnsCondition(stats, totalSpawnsLimit);

                default:
                    throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown lose rule.");
            }
        }
    }
}
