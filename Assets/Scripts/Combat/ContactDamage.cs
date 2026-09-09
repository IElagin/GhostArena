using System.Collections.Generic;
using UnityEngine;

namespace GhostArena
{
    public sealed class ContactDamage : MonoBehaviour
    {
        private readonly Dictionary<Character, int> _targetContacts =
            new Dictionary<Character, int>();
        private Character _owner;
        private ITargetDamagePolicy _targetPolicy;
        private int _damage;
        private bool _isInitialized;

        public int Damage => _damage;

        public void Initialize(
            Character owner,
            int damage,
            ITargetDamagePolicy targetPolicy)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("Contact damage is already initialized.");
            }

            if (damage <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(damage));
            }

            _owner = owner != null ? owner : throw new System.ArgumentNullException(nameof(owner));
            _targetPolicy = targetPolicy
                ?? throw new System.ArgumentNullException(nameof(targetPolicy));
            _damage = damage;
            _isInitialized = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            Character target = collision.collider.GetComponentInParent<Character>();

            if (target == null || _isInitialized == false || _owner.IsGameplayActive == false
                || _targetPolicy.CanDamage(_owner, target) == false)
            {
                return;
            }

            _targetContacts.TryGetValue(target, out int contactCount);
            _targetContacts[target] = contactCount + 1;

            if (contactCount == 0 && target.CanReceiveDamage)
            {
                target.Health.TakeDamage(_damage);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            Character target = collision.collider.GetComponentInParent<Character>();

            if (target == null || _targetContacts.TryGetValue(target, out int contactCount) == false)
            {
                return;
            }

            if (contactCount <= 1)
            {
                _targetContacts.Remove(target);
                return;
            }

            _targetContacts[target] = contactCount - 1;
        }

        private void OnDisable()
        {
            _targetContacts.Clear();
        }
    }
}
