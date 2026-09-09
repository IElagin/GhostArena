using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class EnemySpawner : IDisposable
    {
        private readonly CharactersFactory _charactersFactory;
        private readonly ControllersUpdateService _controllers;
        private readonly EntityRegistry<Character> _enemies;
        private readonly SessionStats _stats;
        private readonly Transform[] _spawnPoints;
        private readonly float _spawnInterval;
        private readonly EnemySettings _enemySettings;
        private readonly GhostVisualSettings _visualSettings;
        private float _elapsed;
        private bool _isGameplayActive;
        private bool _isDisposed;

        public EnemySpawner(
            CharactersFactory charactersFactory,
            ControllersUpdateService controllers,
            EntityRegistry<Character> enemies,
            SessionStats stats,
            Transform[] spawnPoints,
            float spawnInterval,
            EnemySettings enemySettings,
            GhostVisualSettings visualSettings)
        {
            _charactersFactory = charactersFactory
                ?? throw new ArgumentNullException(nameof(charactersFactory));
            _controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));

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

            _spawnPoints = (Transform[])spawnPoints.Clone();
            _spawnInterval = spawnInterval;
            _enemySettings = enemySettings;
            _visualSettings = visualSettings;
        }

        public void SetGameplayActive(bool isActive)
        {
            _isGameplayActive = isActive && _isDisposed == false;

            foreach (Character enemy in _enemies.Items)
            {
                if (enemy != null)
                {
                    enemy.SetGameplayActive(_isGameplayActive);
                }
            }
        }

        public void Tick(float deltaTime)
        {
            if (_isGameplayActive == false || _isDisposed)
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
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _isGameplayActive = false;
            Character[] enemies = new Character[_enemies.Count];

            for (int index = 0; index < _enemies.Count; index++)
            {
                enemies[index] = _enemies.Items[index];
            }

            foreach (Character enemy in enemies)
            {
                Release(enemy, false);
            }
        }

        private void Spawn()
        {
            int spawnIndex = UnityEngine.Random.Range(0, _spawnPoints.Length);
            Character enemy = _charactersFactory.CreateEnemy(
                _spawnPoints[spawnIndex],
                _enemySettings,
                _visualSettings);
            enemy.Died += OnEnemyDied;

            if (_enemies.Add(enemy) == false)
            {
                enemy.Died -= OnEnemyDied;
                _controllers.Remove(enemy.Controller);
                enemy.Dispose();
                UnityEngine.Object.Destroy(enemy.gameObject);
                return;
            }

            _stats.RecordSpawn();
            enemy.SetGameplayActive(_isGameplayActive);
        }

        private void Release(Character enemy, bool wasKilled)
        {
            if (ReferenceEquals(enemy, null))
            {
                return;
            }

            enemy.Died -= OnEnemyDied;

            if (_enemies.Remove(enemy) == false)
            {
                return;
            }

            _controllers.Remove(enemy.Controller);
            enemy.Dispose();
            _stats.RecordRemoval(wasKilled);

            if (enemy != null)
            {
                UnityEngine.Object.Destroy(enemy.gameObject);
            }
        }

        private void OnEnemyDied(Character enemy)
        {
            Release(enemy, true);
        }
    }
}
