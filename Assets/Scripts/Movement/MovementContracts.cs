using UnityEngine;

namespace GhostArena
{
    public interface IVelocitySource
    {
        Vector3 CurrentVelocity { get; }
    }

    public interface IDirectionalMover : IVelocitySource
    {
        void SetDirection(Vector3 direction);

        void Move(float fixedDeltaTime);

        void Stop();
    }

    public interface IDestinationMover : IVelocitySource
    {
        int AreaMask { get; }

        bool TrySetDestination(Vector3 destination);

        void Stop();

        void Resume();
    }

    public interface IDirectionalRotator
    {
        void SetDirection(Vector3 direction);

        void Rotate(float deltaTime);
    }
}
