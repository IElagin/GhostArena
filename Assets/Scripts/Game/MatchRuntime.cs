using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class MatchRuntime : IDisposable
    {
        private readonly Transform _runtimeRoot;
        private bool _isGameplayActive;
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
            _runtimeRoot = runtimeRoot;
            Settings = settings;
            Session = session;
            Player = player;
            Enemies = enemies;
            Spawner = spawner;
            Controllers = controllers;
            Spawner.Spawned += OnEnemySpawned;
            Controllers.Add(Player.Controller);
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
            _isGameplayActive = isActive && _isDisposed == false;

            if (Player != null)
            {
                Player.SetGameplayActive(_isGameplayActive && Player.Health.IsAlive);
            }

            foreach (Character enemy in Enemies.Items)
            {
                if (enemy != null)
                {
                    enemy.SetGameplayActive(_isGameplayActive);
                }
            }

            Spawner.SetGameplayActive(_isGameplayActive);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            SetGameplayActive(false);
            _isDisposed = true;

            if (_runtimeRoot != null)
            {
                _runtimeRoot.gameObject.SetActive(false);
            }

            Spawner.Spawned -= OnEnemySpawned;
            Spawner.Dispose();
            Character[] enemies = new Character[Enemies.Count];

            for (int index = 0; index < Enemies.Count; index++)
            {
                enemies[index] = Enemies.Items[index];
            }

            foreach (Character enemy in enemies)
            {
                ReleaseEnemy(enemy, false);
            }

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

        private void OnEnemySpawned(Character enemy)
        {
            enemy.Died += OnEnemyDied;

            if (Enemies.Add(enemy) == false)
            {
                enemy.Died -= OnEnemyDied;
                enemy.Dispose();
                enemy.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(enemy.gameObject);
                return;
            }

            Controllers.Add(enemy.Controller);
            Session.Stats.RecordSpawn();
            enemy.SetGameplayActive(_isGameplayActive);
        }

        private void ReleaseEnemy(Character enemy, bool wasKilled)
        {
            if (ReferenceEquals(enemy, null))
            {
                return;
            }

            enemy.Died -= OnEnemyDied;

            if (Enemies.Remove(enemy) == false)
            {
                return;
            }

            Controllers.Remove(enemy.Controller);
            enemy.Dispose();
            Session.Stats.RecordRemoval(wasKilled);

            if (enemy != null)
            {
                UnityEngine.Object.Destroy(enemy.gameObject);
            }
        }

        private void OnEnemyDied(Character enemy)
        {
            ReleaseEnemy(enemy, true);
        }
    }
}
