using System;

namespace GhostArena
{
    public interface ITargetDamagePolicy
    {
        bool CanDamage(Character owner, Character target);
    }

    public sealed class CharacterRoleDamagePolicy : ITargetDamagePolicy
    {
        private readonly CharacterRole _targetRole;

        public CharacterRoleDamagePolicy(CharacterRole targetRole)
        {
            _targetRole = targetRole;
        }

        public bool CanDamage(Character owner, Character target)
        {
            if (owner == null || target == null)
            {
                return false;
            }

            return ReferenceEquals(owner, target) == false && target.Role == _targetRole;
        }
    }
}
