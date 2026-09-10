namespace GhostArena
{
    public interface IContactDamageable : IDamageable
    {
        bool TryTakeContactDamage(int damage);
    }
}
