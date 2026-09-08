using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GhostArena
{
    public sealed class PlayerShooter : MonoBehaviour
    {
        private const string PressOnlyInteraction = "Press(behavior=0)";

        [SerializeField] private Transform _muzzle;
        [SerializeField] private ActorHealth _health;

        private GameObject _projectilePrefab;
        private Transform _runtimeRoot;
        private InputAction _fireAction;
        private bool _isGameplayActive;

        public event Action<Projectile> Shot;

        public int ShotsFired { get; private set; }

        public void Initialize(GameObject projectilePrefab, Transform runtimeRoot)
        {
            if (_fireAction != null)
            {
                throw new InvalidOperationException("Player shooter is already initialized.");
            }

            _projectilePrefab = projectilePrefab != null
                ? projectilePrefab
                : throw new ArgumentNullException(nameof(projectilePrefab));
            _runtimeRoot = runtimeRoot != null
                ? runtimeRoot
                : throw new ArgumentNullException(nameof(runtimeRoot));

            if (_muzzle == null || _health == null)
            {
                throw new InvalidOperationException("Player shooter references are not configured.");
            }

            _fireAction = new InputAction(
                "Fire",
                InputActionType.Button,
                "<Keyboard>/space",
                PressOnlyInteraction);
            _fireAction.performed += OnFirePerformed;
        }

        public void SetGameplayActive(bool isActive)
        {
            _isGameplayActive = isActive && _health.IsAlive;

            if (_isGameplayActive)
            {
                _fireAction.Enable();
                return;
            }

            _fireAction.Disable();
        }

        private void OnDestroy()
        {
            if (_fireAction == null)
            {
                return;
            }

            _fireAction.performed -= OnFirePerformed;
            _fireAction.Disable();
            _fireAction.Dispose();
        }

        private void OnFirePerformed(InputAction.CallbackContext context)
        {
            if (_isGameplayActive == false || _health.IsAlive == false)
            {
                return;
            }

            GameObject projectileObject = Instantiate(
                _projectilePrefab,
                _muzzle.position,
                _muzzle.rotation,
                _runtimeRoot);
            Projectile projectile = projectileObject.GetComponent<Projectile>();

            if (projectile == null)
            {
                Destroy(projectileObject);
                throw new InvalidOperationException("Projectile prefab has no Projectile component.");
            }

            projectile.Initialize(transform.forward, gameObject);
            ShotsFired++;
            Shot?.Invoke(projectile);
        }
    }
}
