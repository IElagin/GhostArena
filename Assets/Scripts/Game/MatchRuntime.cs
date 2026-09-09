using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class MatchRuntime : IDisposable
    {
        private readonly Transform _runtimeRoot;
        private bool _isDisposed;

        public MatchRuntime(
            Transform runtimeRoot,
            GameplaySettings settings,
            GameSession session,
            Character player,
            EntityRegistry<Character> enemies,
            EnemySpawner spawner,
            ControllersUpdateService controllers)
        {
            _runtimeRoot = runtimeRoot != null
                ? runtimeRoot
                : throw new ArgumentNullException(nameof(runtimeRoot));
            Settings = settings;
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Player = player != null ? player : throw new ArgumentNullException(nameof(player));
            Enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            Spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            Controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
        }

        public GameplaySettings Settings { get; }

        public GameSession Session { get; }

        public Character Player { get; }

        public EntityRegistry<Character> Enemies { get; }

        public EnemySpawner Spawner { get; }

        public Weapon Weapon => Player.Weapon;

        public ControllersUpdateService Controllers { get; }

        public void SetGameplayActive(bool isActive)
        {
            if (Player != null)
            {
                Player.SetGameplayActive(isActive && Player.Health.IsAlive);
            }

            Spawner.SetGameplayActive(isActive);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            SetGameplayActive(false);

            if (_runtimeRoot != null)
            {
                _runtimeRoot.gameObject.SetActive(false);
            }

            Spawner.Dispose();

            if (ReferenceEquals(Player, null) == false)
            {
                Controllers.Remove(Player.Controller);
                Player.Dispose();
            }

            Session.Dispose();
            Controllers.Dispose();

            if (_runtimeRoot != null)
            {
                UnityEngine.Object.Destroy(_runtimeRoot.gameObject);
            }
        }
    }
}
