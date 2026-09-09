using UnityEngine;

namespace GhostArena
{
    public sealed class WeaponMount : MonoBehaviour
    {
        [SerializeField] private Transform _muzzle;

        public Transform Muzzle => _muzzle;
    }
}
