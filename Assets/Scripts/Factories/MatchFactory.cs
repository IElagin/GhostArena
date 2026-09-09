using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class MatchFactory
    {
        private readonly GameplayConfig _config;
        private readonly ControllersFactory _controllersFactory;
        private readonly GameObject _playerPrefab;
        private readonly GameObject _enemyPrefab;
        private readonly GameObject _projectilePrefab;
        private readonly Transform _playerSpawn;
        private readonly Transform[] _enemySpawns;
        private readonly Camera _gameplayCamera;
        private readonly Vector2 _arenaHalfExtents;

        public MatchFactory(
            GameplayConfig config,
            ControllersFactory controllersFactory,
            GameObject playerPrefab,
            GameObject enemyPrefab,
            GameObject projectilePrefab,
            Transform playerSpawn,
            Transform[] enemySpawns,
            Camera gameplayCamera,
            Vector2 arenaHalfExtents)
        {
            _config = config;
            _controllersFactory = controllersFactory;
            _playerPrefab = playerPrefab;
            _enemyPrefab = enemyPrefab;
            _projectilePrefab = projectilePrefab;
            _playerSpawn = playerSpawn;
            _gameplayCamera = gameplayCamera;

            if (arenaHalfExtents.x <= 0f || arenaHalfExtents.y <= 0f
                || float.IsNaN(arenaHalfExtents.x) || float.IsInfinity(arenaHalfExtents.x)
                || float.IsNaN(arenaHalfExtents.y) || float.IsInfinity(arenaHalfExtents.y))
            {
                throw new ArgumentOutOfRangeException(nameof(arenaHalfExtents));
            }

            _enemySpawns = (Transform[])enemySpawns.Clone();
            _arenaHalfExtents = arenaHalfExtents;
        }

        public SessionConfiguration CaptureConfiguration()
        {
            GameplaySettings gameplay = _config.CreateSettings();
            GhostVisual playerVisualComponent = _playerPrefab.GetComponentInChildren<GhostVisual>(true);
            GhostVisual enemyVisualComponent = _enemyPrefab.GetComponentInChildren<GhostVisual>(true);

            if (playerVisualComponent == null || enemyVisualComponent == null)
            {
                string missingPrefab = playerVisualComponent == null ? _playerPrefab.name : _enemyPrefab.name;
                throw new InvalidOperationException(missingPrefab + " prefab has no GhostVisual component.");
            }

            GhostVisualSettings playerVisual = playerVisualComponent.CreateSettings();
            GhostVisualSettings enemyVisual = enemyVisualComponent.CreateSettings();
            return new SessionConfiguration(gameplay, playerVisual, enemyVisual);
        }

        public MatchRuntime Create(SessionConfiguration configuration)
        {
            Transform runtimeRoot = new GameObject("Session Runtime").transform;
            ControllersUpdateService controllers = new ControllersUpdateService();
            Character player = null;
            GameSession session = null;
            EnemySpawner spawner = null;

            try
            {
                CharactersFactory charactersFactory = new CharactersFactory(
                    _controllersFactory,
                    controllers,
                    _playerPrefab,
                    _enemyPrefab,
                    _projectilePrefab,
                    runtimeRoot,
                    _gameplayCamera,
                    _arenaHalfExtents);
                player = charactersFactory.CreatePlayer(
                    _playerSpawn,
                    configuration.Gameplay,
                    configuration.PlayerVisual);
                EntityRegistry<Character> enemies = new EntityRegistry<Character>();
                SessionStats stats = new SessionStats();
                IGameCondition winCondition = ConditionFactory.CreateWin(
                    configuration.Gameplay.WinRule,
                    stats,
                    player.Health,
                    configuration.Gameplay.SurviveDuration,
                    configuration.Gameplay.KillTarget);
                IGameCondition loseCondition = ConditionFactory.CreateLose(
                    configuration.Gameplay.LoseRule,
                    stats,
                    player.Health,
                    configuration.Gameplay.TotalSpawnsLimit);
                session = new GameSession(stats, winCondition, loseCondition);
                spawner = new EnemySpawner(
                    charactersFactory,
                    controllers,
                    enemies,
                    stats,
                    _enemySpawns,
                    configuration.Gameplay.SpawnInterval,
                    configuration.Gameplay.Enemy,
                    configuration.EnemyVisual);
                return new MatchRuntime(
                    runtimeRoot,
                    configuration.Gameplay,
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

    public readonly struct SessionConfiguration
    {
        public SessionConfiguration(
            GameplaySettings gameplay,
            GhostVisualSettings playerVisual,
            GhostVisualSettings enemyVisual)
        {
            Gameplay = gameplay;
            PlayerVisual = playerVisual;
            EnemyVisual = enemyVisual;
        }

        public GameplaySettings Gameplay { get; }

        public GhostVisualSettings PlayerVisual { get; }

        public GhostVisualSettings EnemyVisual { get; }
    }
}
