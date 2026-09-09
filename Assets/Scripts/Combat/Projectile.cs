using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody _body;

        private Character _owner;
        private ITargetDamagePolicy _targetPolicy;
        private float _movementSpeed;
        private int _damage;
        private float _lifetime;
        private float _timeRemaining;
        private bool _isConsumed;
        private bool _isInitialized;

        public event Action<Projectile, Character> Hit;

        public bool IsConsumed => _isConsumed;

        public float MovementSpeed => _movementSpeed;

        public int Damage => _damage;

        public float Lifetime => _lifetime;

        public void Initialize(
            Vector3 direction,
            Character owner,
            ProjectileSettings settings,
            ITargetDamagePolicy targetPolicy)
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

            if (settings.IsValid == false)
            {
                throw new ArgumentOutOfRangeException(nameof(settings));
            }

            _owner = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
            _targetPolicy = targetPolicy ?? throw new ArgumentNullException(nameof(targetPolicy));
            _movementSpeed = settings.MovementSpeed;
            _damage = settings.Damage;
            _lifetime = settings.Lifetime;
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

            Character target = collision.collider.GetComponentInParent<Character>();

            if (target != null)
            {
                if (target.Health.IsAlive == false)
                {
                    IgnoreDeadEnemy(collision.collider);
                    return;
                }

                if (_targetPolicy.CanDamage(_owner, target) == false)
                {
                    Consume();
                    return;
                }

                _isConsumed = true;
                _body.linearVelocity = Vector3.zero;
                target.Health.TakeDamage(_damage);
                Hit?.Invoke(this, target);
                Destroy(gameObject);
                return;
            }

            Consume();
        }

        private void IgnoreDeadEnemy(Collider enemyCollider)
        {
            Collider projectileCollider = GetComponent<Collider>();

            if (projectileCollider != null)
            {
                Physics.IgnoreCollision(projectileCollider, enemyCollider, true);
            }

            _body.linearVelocity = transform.forward * _movementSpeed;
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
