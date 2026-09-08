using System;

namespace GhostArena
{
    public sealed class Health
    {
        public Health(int maximum)
        {
            if (maximum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum));
            }

            Maximum = maximum;
            Current = maximum;
        }

        public event Action Changed;
        public event Action Died;

        public int Maximum { get; }

        public int Current { get; private set; }

        public bool IsAlive => Current > 0;

        public void TakeDamage(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (IsAlive == false)
            {
                return;
            }

            Current = Math.Max(0, Current - amount);
            Changed?.Invoke();

            if (IsAlive == false)
            {
                Died?.Invoke();
            }
        }
    }
}
