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
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _controllersFactory = controllersFactory
                ?? throw new ArgumentNullException(nameof(controllersFactory));
            _playerPrefab = playerPrefab != null
                ? playerPrefab
                : throw new ArgumentNullException(nameof(playerPrefab));
            _enemyPrefab = enemyPrefab != null
                ? enemyPrefab
                : throw new ArgumentNullException(nameof(enemyPrefab));
            _projectilePrefab = projectilePrefab != null
                ? projectilePrefab
                : throw new ArgumentNullException(nameof(projectilePrefab));
            _playerSpawn = playerSpawn != null
                ? playerSpawn
                : throw new ArgumentNullException(nameof(playerSpawn));
            _gameplayCamera = gameplayCamera != null
                ? gameplayCamera
                : throw new ArgumentNullException(nameof(gameplayCamera));

            if (enemySpawns == null || enemySpawns.Length == 0)
            {
                throw new ArgumentException("Enemy spawn points are not configured.", nameof(enemySpawns));
            }

            foreach (Transform spawnPoint in enemySpawns)
            {
                if (spawnPoint == null)
                {
                    throw new ArgumentException("Enemy spawn points cannot contain null.", nameof(enemySpawns));
                }
            }

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
            ValidatePrefab<Character>(_playerPrefab, "Player");
            ValidatePrefab<Rigidbody>(_playerPrefab, "Player");
            ValidatePrefab<WeaponMount>(_playerPrefab, "Player");
            ValidateChildPrefab<GhostVisual>(_playerPrefab, "Player");
            _playerPrefab.GetComponent<WeaponMount>().Validate();
            ValidatePrefab<Character>(_enemyPrefab, "Enemy");
            ValidatePrefab<Rigidbody>(_enemyPrefab, "Enemy");
            ValidatePrefab<UnityEngine.AI.NavMeshAgent>(_enemyPrefab, "Enemy");
            ValidatePrefab<ContactDamage>(_enemyPrefab, "Enemy");
            ValidateChildPrefab<GhostVisual>(_enemyPrefab, "Enemy");
            ValidatePrefab<Projectile>(_projectilePrefab, "Projectile");
            GameplaySettings gameplay = _config.CreateSettings();
            GhostVisualSettings playerVisual = _playerPrefab
                .GetComponentInChildren<GhostVisual>(true)
                .CreateSettings();
            GhostVisualSettings enemyVisual = _enemyPrefab
                .GetComponentInChildren<GhostVisual>(true)
                .CreateSettings();
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

        private static void ValidatePrefab<T>(GameObject prefab, string prefabName)
            where T : Component
        {
            if (prefab.GetComponent<T>() == null)
            {
                throw new InvalidOperationException(
                    prefabName + " prefab has no " + typeof(T).Name + " component.");
            }
        }

        private static void ValidateChildPrefab<T>(GameObject prefab, string prefabName)
            where T : Component
        {
            if (prefab.GetComponentInChildren<T>(true) == null)
            {
                throw new InvalidOperationException(
                    prefabName + " prefab has no " + typeof(T).Name + " component.");
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
