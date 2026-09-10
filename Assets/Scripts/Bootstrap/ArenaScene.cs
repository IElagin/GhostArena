using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class ArenaScene : MonoBehaviour
    {
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private Transform[] _enemySpawnPoints;
        [SerializeField] private Camera _gameplayCamera;
        [SerializeField] private Vector2 _arenaHalfExtents = new Vector2(8.3f, 6.3f);

        public Transform PlayerSpawnPoint => _playerSpawnPoint;

        public Transform[] EnemySpawnPoints => _enemySpawnPoints;

        public Camera GameplayCamera => _gameplayCamera;

        public Vector2 ArenaHalfExtents => _arenaHalfExtents;

        public void Validate()
        {
            if (_playerSpawnPoint == null || _gameplayCamera == null)
            {
                throw new InvalidOperationException("Arena scene references are not configured.");
            }

            if (_enemySpawnPoints == null || _enemySpawnPoints.Length == 0)
            {
                throw new InvalidOperationException("Enemy spawn points are not configured.");
            }

            foreach (Transform spawnPoint in _enemySpawnPoints)
            {
                if (spawnPoint == null)
                {
                    throw new InvalidOperationException("Enemy spawn points cannot contain null.");
                }
            }

            if (_arenaHalfExtents.x <= 0f || _arenaHalfExtents.y <= 0f
                || float.IsNaN(_arenaHalfExtents.x) || float.IsInfinity(_arenaHalfExtents.x)
                || float.IsNaN(_arenaHalfExtents.y) || float.IsInfinity(_arenaHalfExtents.y))
            {
                throw new InvalidOperationException("Arena half-extents must be finite and positive.");
            }
        }
    }
}
