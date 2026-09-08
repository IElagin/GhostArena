using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody _body;
        [SerializeField] private float _movementSpeed = 14f;
        [SerializeField] private int _damage = 1;
        [SerializeField] private float _lifetime = 2f;

        private GameObject _owner;
        private float _timeRemaining;
        private bool _isConsumed;
        private bool _isInitialized;

        public event Action<Projectile, EnemyController> Hit;

        public bool IsConsumed => _isConsumed;

        public void Initialize(Vector3 direction, GameObject owner)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Projectile is already initialized.");
            }

            if (_body == null)
            {
                throw new InvalidOperationException("Projectile rigidbody is not configured.");
            }

            if (direction.sqrMagnitude <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(direction));
            }

            _owner = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
            _isInitialized = true;
            _timeRemaining = _lifetime;
            transform.forward = direction.normalized;
            _body.useGravity = false;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.linearVelocity = transform.forward * _movementSpeed;
        }

        private void Update()
        {
            if (_isInitialized == false || _isConsumed)
            {
                return;
            }

            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0f)
            {
                Consume();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isInitialized == false || _isConsumed)
            {
                return;
            }

            Transform collisionTransform = collision.collider.transform;

            if (collisionTransform == _owner.transform || collisionTransform.IsChildOf(_owner.transform))
            {
                return;
            }

            EnemyController enemy = collision.collider.GetComponentInParent<EnemyController>();

            if (enemy != null && enemy.Health.IsAlive)
            {
                _isConsumed = true;
                _body.linearVelocity = Vector3.zero;
                enemy.Health.TakeDamage(_damage);
                Hit?.Invoke(this, enemy);
                Destroy(gameObject);
                return;
            }

            Consume();
        }

        private void Consume()
        {
            if (_isConsumed)
            {
                return;
            }

            _isConsumed = true;
            _body.linearVelocity = Vector3.zero;
            Destroy(gameObject);
        }
    }
}
