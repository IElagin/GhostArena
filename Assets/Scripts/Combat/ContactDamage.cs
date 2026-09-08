using System.Collections.Generic;
using UnityEngine;

namespace GhostArena
{
    public sealed class ContactDamage : MonoBehaviour
    {
        [SerializeField] private EnemyController _enemy;
        [SerializeField] private int _damage = 1;

        private readonly Dictionary<PlayerController, int> _playerContacts =
            new Dictionary<PlayerController, int>();

        private void OnCollisionEnter(Collision collision)
        {
            PlayerController player = collision.collider.GetComponentInParent<PlayerController>();

            if (player == null || _enemy.IsGameplayActive == false)
            {
                return;
            }

            _playerContacts.TryGetValue(player, out int contactCount);
            _playerContacts[player] = contactCount + 1;

            if (contactCount == 0 && player.CanReceiveDamage)
            {
                player.Health.TakeDamage(_damage);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            PlayerController player = collision.collider.GetComponentInParent<PlayerController>();

            if (player == null || _playerContacts.TryGetValue(player, out int contactCount) == false)
            {
                return;
            }

            if (contactCount <= 1)
            {
                _playerContacts.Remove(player);
                return;
            }

            _playerContacts[player] = contactCount - 1;
        }

        private void OnDisable()
        {
            _playerContacts.Clear();
        }
    }
}
