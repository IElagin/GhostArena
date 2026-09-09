using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class ControllersFactory
    {
        public CompositeController CreatePlayer(Character character, Camera gameplayCamera)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            if (character.DirectionalMover == null || character.Weapon == null)
            {
                throw new InvalidOperationException("Player mechanics are not configured.");
            }

            return new CompositeController(
                new KeyboardDirectionController(character.DirectionalMover, gameplayCamera),
                new VelocityFacingController(character.DirectionalMover, character.Rotator),
                new RigidbodyMechanicsController(character.DirectionalMover, character.Rotator),
                new WeaponInputController(character.Weapon));
        }

        public CompositeController CreateEnemy(
            Character character,
            Vector2 arenaHalfExtents,
            float directionInterval,
            float minimumDestinationDistance)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            if (character.DestinationMover == null)
            {
                throw new InvalidOperationException("Enemy destination movement is not configured.");
            }

            return new CompositeController(
                new RandomDestinationController(
                    character.DestinationMover,
                    character.transform,
                    arenaHalfExtents,
                    directionInterval,
                    minimumDestinationDistance),
                new VelocityFacingController(character.DestinationMover, character.Rotator),
                new TransformRotationController(character.Rotator));
        }

        public GameModeInputController CreateGameModeInput(GameMode gameMode)
        {
            return new GameModeInputController(gameMode);
        }
    }
}
