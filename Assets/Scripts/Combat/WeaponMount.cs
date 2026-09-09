using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class WeaponMount : MonoBehaviour
    {
        [SerializeField] private Transform _muzzle;

        public Transform Muzzle => _muzzle;

        public void Validate()
        {
            if (_muzzle == null)
            {
                throw new InvalidOperationException("Weapon muzzle is not configured.");
            }
        }
    }
}
