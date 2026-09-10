using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class Character : MonoBehaviour, IContactDamageable, IDisposable
    {
        private Controller _controller;
        private bool _isDisposed;
        private bool _isInitialized;
        private float _contactGracePeriod;
        private double _nextContactDamageTime;

        public event Action<Character> Died;

        public Health Health { get; private set; }

        public IDirectionalMover DirectionalMover { get; private set; }

        public IDestinationMover DestinationMover { get; private set; }

        public IDirectionalRotator Rotator { get; private set; }

        public Weapon Weapon { get; private set; }

        public Controller Controller => _controller;

        public bool IsGameplayActive => _controller != null && _controller.IsEnabled;

        public void Initialize(
            Health health,
            IDirectionalMover directionalMover,
            IDestinationMover destinationMover,
            IDirectionalRotator rotator,
            float contactGracePeriod = 0f)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Character is already initialized.");
            }

            if (directionalMover == null && destinationMover == null)
            {
                throw new ArgumentException("Character needs a movement mechanic.");
            }

            if (contactGracePeriod < 0f || float.IsNaN(contactGracePeriod)
                || float.IsInfinity(contactGracePeriod))
            {
                throw new ArgumentOutOfRangeException(nameof(contactGracePeriod));
            }

            _contactGracePeriod = contactGracePeriod;
            Health = health ?? throw new ArgumentNullException(nameof(health));
            DirectionalMover = directionalMover;
            DestinationMover = destinationMover;
            Rotator = rotator ?? throw new ArgumentNullException(nameof(rotator));
            Health.Died += OnDied;
            _isInitialized = true;
        }

        public void BindController(Controller controller)
        {
            if (_controller != null)
            {
                throw new InvalidOperationException("Character controller is already bound.");
            }

            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public void BindWeapon(Weapon weapon)
        {
            if (Weapon != null)
            {
                throw new InvalidOperationException("Character weapon is already bound.");
            }

            Weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
        }

        public void SetGameplayActive(bool isActive)
        {
            if (_controller == null)
            {
                return;
            }

            if (isActive && Health.IsAlive)
            {
                DestinationMover?.Resume();
                _controller.Enable();
                return;
            }

            _controller.Disable();
            DirectionalMover?.Stop();
            DestinationMover?.Stop();
        }

        public bool TryTakeDamage(int damage)
        {
            if (IsGameplayActive == false || Health == null || Health.IsAlive == false)
            {
                return false;
            }

            Health.TakeDamage(damage);
            return true;
        }

        public bool TryTakeContactDamage(int damage)
        {
            if (IsGameplayActive == false || Health == null || Health.IsAlive == false
                || Time.timeAsDouble < _nextContactDamageTime)
            {
                return false;
            }

            if (damage <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            _nextContactDamageTime = Time.timeAsDouble + _contactGracePeriod;
            return TryTakeDamage(damage);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            SetGameplayActive(false);

            if (Health != null)
            {
                Health.Died -= OnDied;
            }

            _controller?.Dispose();
            Weapon?.Dispose();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnDied()
        {
            SetGameplayActive(false);
            Died?.Invoke(this);
        }
    }
}
