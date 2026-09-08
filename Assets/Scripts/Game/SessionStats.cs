using System;

namespace GhostArena
{
    public sealed class SessionStats
    {
        public event Action Changed;

        public float Elapsed { get; private set; }

        public int Kills { get; private set; }

        public int TotalSpawned { get; private set; }

        public int AliveCount { get; private set; }

        public void Advance(float deltaTime)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (deltaTime == 0f)
            {
                return;
            }

            Elapsed += deltaTime;
            Changed?.Invoke();
        }

        public void RecordSpawn()
        {
            TotalSpawned++;
            AliveCount++;
            Changed?.Invoke();
        }

        public void RecordRemoval(bool wasKilled)
        {
            if (AliveCount <= 0)
            {
                throw new InvalidOperationException("Cannot remove an entity when none are alive.");
            }

            AliveCount--;

            if (wasKilled)
            {
                Kills++;
            }

            Changed?.Invoke();
        }
    }
}
