using System;

namespace GhostArena
{
    public interface IGameCondition : IDisposable
    {
        event Action Satisfied;

        bool IsSatisfied { get; }

        void Start();

        void Tick(float deltaTime);
    }
}
