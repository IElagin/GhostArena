using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class GameLoop : MonoBehaviour
    {
        private GameMode _gameMode;

        public void Initialize(GameMode gameMode)
        {
            if (_gameMode != null)
            {
                throw new InvalidOperationException("Game loop is already initialized.");
            }

            _gameMode = gameMode ?? throw new ArgumentNullException(nameof(gameMode));
        }

        private void Update()
        {
            _gameMode?.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            _gameMode?.FixedTick(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            _gameMode?.LateTick(Time.deltaTime);
        }
    }
}
