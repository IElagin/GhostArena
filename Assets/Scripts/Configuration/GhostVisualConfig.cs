using System;
using UnityEngine;

namespace GhostArena
{
    [CreateAssetMenu(fileName = "GhostVisualConfig", menuName = "Ghost Arena/Ghost Visual Config")]
    public sealed class GhostVisualConfig : ScriptableObject
    {
        [Header("Motion")]
        [Tooltip("Maximum local vertical offset of the ghost body.")]
        [SerializeField] private float _bobHeight = 0.06f;
        [Tooltip("Speed of the bob and squash wave.")]
        [SerializeField] private float _bobSpeed = 2.4f;
        [Tooltip("Maximum vertical squash and stretch amount.")]
        [SerializeField] private float _squashAmount = 0.025f;

        public float BobHeight => _bobHeight;

        public float BobSpeed => _bobSpeed;

        public float SquashAmount => _squashAmount;

        public GhostVisualSettings CreateSettings()
        {
            Validate();
            return new GhostVisualSettings(_bobHeight, _bobSpeed, _squashAmount);
        }

        public void Validate()
        {
            ValidateNonNegativeFinite(_bobHeight, "Bob height");

            if (_bobSpeed <= 0f || float.IsNaN(_bobSpeed) || float.IsInfinity(_bobSpeed))
            {
                throw new InvalidOperationException("Bob speed must be finite and positive.");
            }

            ValidateNonNegativeFinite(_squashAmount, "Squash amount");
        }

        private static void ValidateNonNegativeFinite(float value, string name)
        {
            if (value < 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidOperationException(name + " must be finite and non-negative.");
            }
        }
    }

    public readonly struct GhostVisualSettings
    {
        public GhostVisualSettings(float bobHeight, float bobSpeed, float squashAmount)
        {
            BobHeight = bobHeight;
            BobSpeed = bobSpeed;
            SquashAmount = squashAmount;
        }

        public float BobHeight { get; }

        public float BobSpeed { get; }

        public float SquashAmount { get; }
    }
}
