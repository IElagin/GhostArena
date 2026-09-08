using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class EnemySpawner : IDisposable
    {
        private readonly GameObject _enemyPrefab;
        private readonly Transform[] _spawnPoints;
        private readonly Transform _runtimeRoot;
        private readonly Vector2 _arenaHalfExtents;
        private readonly float _spawnInterval;
        private readonly EnemySettings _enemySettings;
        private readonly GhostVisualSettings _visualSettings;
        private float _elapsed;
        private bool _isDisposed;

        public EnemySpawner(
            GameObject enemyPrefab,
            Transform[] spawnPoints,
            Transform runtimeRoot,
            Vector2 arenaHalfExtents,
            float spawnInterval,
            EnemySettings enemySettings,
            GhostVisualSettings visualSettings)
        {
            _enemyPrefab = enemyPrefab != null
                ? enemyPrefab
                : throw new ArgumentNullException(nameof(enemyPrefab));
            _runtimeRoot = runtimeRoot != null
                ? runtimeRoot
                : throw new ArgumentNullException(nameof(runtimeRoot));

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                throw new ArgumentException("At least one enemy spawn point is required.", nameof(spawnPoints));
            }

            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint == null)
                {
                    throw new ArgumentException("Enemy spawn points cannot contain null.", nameof(spawnPoints));
                }
            }

            if (spawnInterval <= 0f || float.IsNaN(spawnInterval) || float.IsInfinity(spawnInterval))
            {
                throw new ArgumentOutOfRangeException(nameof(spawnInterval));
            }

            if (enemySettings.MaximumHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemySettings));
            }

            if (arenaHalfExtents.x <= 0f || arenaHalfExtents.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(arenaHalfExtents));
            }

            _spawnPoints = (Transform[])spawnPoints.Clone();
            _arenaHalfExtents = arenaHalfExtents;
            _spawnInterval = spawnInterval;
            _enemySettings = enemySettings;
            _visualSettings = visualSettings;
        }

        public event Action<EnemyController> Spawned;

        public float SpawnInterval => _spawnInterval;

        public void Tick(float deltaTime)
        {
            if (_isDisposed)
            {
                return;
            }

            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            _elapsed += deltaTime;

            if (_elapsed < _spawnInterval)
            {
                return;
            }

            _elapsed -= _spawnInterval;
            Spawn();
        }

        public void Dispose()
        {
            _isDisposed = true;
            Spawned = null;
        }

        private void Spawn()
        {
            int spawnIndex = UnityEngine.Random.Range(0, _spawnPoints.Length);
            Transform spawnPoint = _spawnPoints[spawnIndex];
            GameObject enemyObject = UnityEngine.Object.Instantiate(
                _enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                _runtimeRoot);
            EnemyController enemy = enemyObject.GetComponent<EnemyController>();

            if (enemy == null)
            {
                UnityEngine.Object.Destroy(enemyObject);
                throw new InvalidOperationException("Enemy prefab has no EnemyController component.");
            }

            GhostVisual visual = enemyObject.GetComponentInChildren<GhostVisual>(true);

            if (visual == null)
            {
                UnityEngine.Object.Destroy(enemyObject);
                throw new InvalidOperationException("Enemy prefab has no GhostVisual component.");
            }

            enemy.Initialize(_arenaHalfExtents, _enemySettings);
            visual.Initialize(_visualSettings);
            Spawned?.Invoke(enemy);
        }
    }
}
