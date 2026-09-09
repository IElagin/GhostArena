using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class Weapon : IDisposable
    {
        private readonly Character _owner;
        private readonly Transform _muzzle;
        private readonly GameObject _projectilePrefab;
        private readonly Transform _runtimeRoot;
        private readonly ProjectileSettings _settings;
        private bool _isDisposed;

        public Weapon(
            Character owner,
            Transform muzzle,
            GameObject projectilePrefab,
            Transform runtimeRoot,
            ProjectileSettings settings)
        {
            _owner = owner;
            _muzzle = muzzle;
            _projectilePrefab = projectilePrefab;
            _runtimeRoot = runtimeRoot;

            if (settings.IsValid == false)
            {
                throw new ArgumentOutOfRangeException(nameof(settings));
            }

            _settings = settings;
        }

        public event Action<Projectile> Shot;

        public bool TryFire()
        {
            if (_isDisposed || _owner.IsGameplayActive == false || _owner.Health.IsAlive == false)
            {
                return false;
            }

            GameObject projectileObject = UnityEngine.Object.Instantiate(
                _projectilePrefab,
                _muzzle.position,
                _muzzle.rotation,
                _runtimeRoot);
            Projectile projectile = projectileObject.GetComponent<Projectile>();

            if (projectile == null)
            {
                UnityEngine.Object.Destroy(projectileObject);
                throw new InvalidOperationException("Projectile prefab has no Projectile component.");
            }

            projectile.Initialize(_muzzle.forward, _owner.transform, _settings);
            Shot?.Invoke(projectile);
            return true;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Shot = null;
        }
    }
}
