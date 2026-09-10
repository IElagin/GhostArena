using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class MatchFactory
    {
        private readonly GameConfig _config;
        private readonly ArenaScene _arenaScene;
        private readonly ControllersFactory _controllersFactory;

        public MatchFactory(
            GameConfig config,
            ArenaScene arenaScene,
            ControllersFactory controllersFactory)
        {
            _config = config;
            _arenaScene = arenaScene;
            _controllersFactory = controllersFactory;
        }

        public GameplaySettings CaptureConfiguration()
        {
            return _config.Gameplay.CreateSettings();
        }

        public MatchRuntime Create(GameplaySettings settings)
        {
            Transform runtimeRoot = new GameObject("Session Runtime").transform;
            ControllersUpdateService controllers = new ControllersUpdateService();
            Character player = null;
            GameSession session = null;
            EnemySpawner spawner = null;

            try
            {
                CharactersFactory charactersFactory = new CharactersFactory(
                    _config,
                    settings,
                    _arenaScene,
                    runtimeRoot,
                    _controllersFactory);
                player = charactersFactory.CreatePlayer(_arenaScene.PlayerSpawnPoint);
                EntityRegistry<Character> enemies = new EntityRegistry<Character>();
                SessionStats stats = new SessionStats();
                IGameCondition winCondition = ConditionFactory.CreateWin(
                    settings.WinRule,
                    stats,
                    player.Health,
                    settings.SurviveDuration,
                    settings.KillTarget);
                IGameCondition loseCondition = ConditionFactory.CreateLose(
                    settings.LoseRule,
                    stats,
                    player.Health,
                    settings.EnemyLimit);
                session = new GameSession(stats, winCondition, loseCondition, player.Health);
                spawner = new EnemySpawner(
                    charactersFactory,
                    _arenaScene.EnemySpawnPoints,
                    settings.SpawnInterval);
                return new MatchRuntime(
                    runtimeRoot,
                    settings,
                    session,
                    player,
                    enemies,
                    spawner,
                    controllers);
            }
            catch
            {
                spawner?.Dispose();
                player?.Dispose();
                session?.Dispose();
                controllers.Dispose();
                runtimeRoot.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(runtimeRoot.gameObject);
                throw;
            }
        }

    }
}
