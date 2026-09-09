using System.Collections.Generic;
using UnityEngine;

namespace GhostArena
{
    public sealed class ContactDamage : MonoBehaviour
    {
        [SerializeField] private LayerMask _damageLayers = ~0;

        private readonly Dictionary<Collider, Contact> _contacts = new Dictionary<Collider, Contact>();
        private Character _owner;
        private int _damage;
        private bool _isInitialized;

        public void Initialize(Character owner, int damage)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("Contact damage is already initialized.");
            }

            if (damage <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(damage));
            }

            _owner = owner;
            _damage = damage;
            _isInitialized = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            Collider collider = collision.collider;

            if (_contacts.TryGetValue(collider, out Contact existingContact))
            {
                existingContact.PairCount++;
                return;
            }

            if (_isInitialized == false || _owner.IsGameplayActive == false
                || (_damageLayers.value & (1 << collider.gameObject.layer)) == 0)
            {
                return;
            }

            IDamageable target = collider.GetComponentInParent<IDamageable>();

            if (target == null)
            {
                return;
            }

            bool alreadyTouching = IsTouching(target);
            _contacts.Add(collider, new Contact(target));

            if (alreadyTouching == false)
            {
                target.TryTakeDamage(_damage);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (_contacts.TryGetValue(collision.collider, out Contact contact) == false)
            {
                return;
            }

            contact.PairCount--;

            if (contact.PairCount == 0)
            {
                _contacts.Remove(collision.collider);
            }
        }

        private bool IsTouching(IDamageable target)
        {
            foreach (Contact contact in _contacts.Values)
            {
                if (ReferenceEquals(contact.Target, target))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDisable()
        {
            _contacts.Clear();
        }

        private sealed class Contact
        {
            public readonly IDamageable Target;
            public int PairCount = 1;

            public Contact(IDamageable target)
            {
                Target = target;
            }
        }
    }
}
