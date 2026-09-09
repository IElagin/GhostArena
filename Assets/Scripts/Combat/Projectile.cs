using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody _body;
        [SerializeField] private LayerMask _damageLayers = ~0;

        private Transform _owner;
        private int _damage;
        private float _timeRemaining;
        private bool _isConsumed;
        private bool _isInitialized;

        public event Action<Projectile, Vector3> Hit;

        public void Initialize(
            Vector3 direction,
            Transform owner,
            ProjectileSettings settings)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Projectile is already initialized.");
            }

            if (_body == null)
            {
                throw new InvalidOperationException("Projectile rigidbody is not configured.");
            }

            _owner = owner;
            _damage = settings.Damage;
            _isInitialized = true;
            _timeRemaining = settings.Lifetime;
            transform.forward = direction.normalized;
            _body.linearVelocity = transform.forward * settings.MovementSpeed;
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

        private void OnTriggerEnter(Collider other)
        {
            if (_isInitialized == false || _isConsumed)
            {
                return;
            }

            Transform collisionTransform = other.transform;

            if (other.isTrigger || collisionTransform == _owner || collisionTransform.IsChildOf(_owner))
            {
                return;
            }

            IDamageable target = other.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                if (IsDamageLayer(other.gameObject.layer) == false)
                {
                    return;
                }

                Vector3 hitPosition = other.ClosestPoint(transform.position);

                if (target.TryTakeDamage(_damage) == false)
                {
                    return;
                }

                Consume();
                Hit?.Invoke(this, hitPosition);
                return;
            }

            Consume();
        }

        private bool IsDamageLayer(int layer)
        {
            return (_damageLayers.value & (1 << layer)) != 0;
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
