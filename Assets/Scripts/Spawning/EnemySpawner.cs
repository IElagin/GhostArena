using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class EnemySpawner : IDisposable
    {
        private readonly CharactersFactory _charactersFactory;
        private readonly Transform[] _spawnPoints;
        private readonly float _spawnInterval;
        private float _elapsed;
        private bool _isGameplayActive;
        private bool _isDisposed;

        public EnemySpawner(
            CharactersFactory charactersFactory,
            Transform[] spawnPoints,
            float spawnInterval)
        {
            _charactersFactory = charactersFactory;
            _spawnPoints = (Transform[])spawnPoints.Clone();
            _spawnInterval = spawnInterval;
        }

        public event Action<Character> Spawned;

        public void SetGameplayActive(bool isActive)
        {
            _isGameplayActive = isActive && _isDisposed == false;
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
            int spawnIndex = UnityEngine.Random.Range(0, _spawnPoints.Length);
            Character enemy = _charactersFactory.CreateEnemy(_spawnPoints[spawnIndex]);
            Spawned?.Invoke(enemy);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _isGameplayActive = false;
            Spawned = null;
        }
    }
}
