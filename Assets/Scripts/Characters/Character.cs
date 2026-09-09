using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class Character : MonoBehaviour, IDisposable
    {
        private Controller _controller;
        private bool _isDisposed;
        private bool _isInitialized;

        public event Action<Character> Died;

        public CharacterRole Role { get; private set; }

        public Health Health { get; private set; }

        public IDirectionalMover DirectionalMover { get; private set; }

        public IDestinationMover DestinationMover { get; private set; }

        public IDirectionalRotator Rotator { get; private set; }

        public Weapon Weapon { get; private set; }

        public Controller Controller => _controller;

        public Vector3 Position => transform.position;

        public bool IsGameplayActive => _controller != null && _controller.IsEnabled;

        public bool CanReceiveDamage => IsGameplayActive && Health != null && Health.IsAlive;

        public void Initialize(
            CharacterRole role,
            Health health,
            IDirectionalMover directionalMover,
            IDestinationMover destinationMover,
            IDirectionalRotator rotator)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Character is already initialized.");
            }

            if (directionalMover == null && destinationMover == null)
            {
                throw new ArgumentException("Character needs a movement mechanic.");
            }

            Role = role;
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

    public enum CharacterRole
    {
        Player,
        Enemy
    }
}
