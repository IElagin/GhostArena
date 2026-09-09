using System;
using UnityEngine;
using UnityEngine.AI;

namespace GhostArena
{
    public sealed class AgentMover : IDestinationMover
    {
        private readonly NavMeshAgent _agent;
        private readonly NavMeshPath _path = new NavMeshPath();

        public AgentMover(NavMeshAgent agent, float movementSpeed)
        {
            _agent = agent != null ? agent : throw new ArgumentNullException(nameof(agent));

            if (movementSpeed <= 0f || float.IsNaN(movementSpeed) || float.IsInfinity(movementSpeed))
            {
                throw new ArgumentOutOfRangeException(nameof(movementSpeed));
            }

            _agent.speed = movementSpeed;
            _agent.updatePosition = true;
            _agent.updateRotation = false;
            _agent.isStopped = true;
        }

        public Vector3 CurrentVelocity => _agent == null ? Vector3.zero : _agent.desiredVelocity;

        public int AreaMask => _agent == null ? NavMesh.AllAreas : _agent.areaMask;

        public bool TrySetDestination(Vector3 destination)
        {
            return _agent != null
                && _agent.enabled
                && _agent.isOnNavMesh
                && _agent.CalculatePath(destination, _path)
                && _path.status == NavMeshPathStatus.PathComplete
                && _agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }
        }

        public void Resume()
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
            }
        }
    }
}
