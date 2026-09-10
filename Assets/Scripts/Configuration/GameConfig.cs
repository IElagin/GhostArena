using System;
using UnityEngine;

namespace GhostArena
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Ghost Arena/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField] private GameplayConfig _gameplay;
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private GameObject _projectilePrefab;

        public GameplayConfig Gameplay => _gameplay;

        public GameObject PlayerPrefab => _playerPrefab;

        public GameObject EnemyPrefab => _enemyPrefab;

        public GameObject ProjectilePrefab => _projectilePrefab;

        public void Validate()
        {
            if (_gameplay == null)
            {
                throw new InvalidOperationException("Gameplay config is not assigned.");
            }

            if (_playerPrefab == null || _enemyPrefab == null || _projectilePrefab == null)
            {
                throw new InvalidOperationException("Gameplay prefabs are not configured.");
            }
        }
    }
}
