using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class GhostVisual : MonoBehaviour
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private GhostVisualConfig _config;

        private Vector3 _baseLocalPosition;
        private Vector3 _baseLocalScale;
        private GhostVisualSettings _settings;
        private float _elapsed;
        private bool _isInitialized;

        public GhostVisualSettings CreateSettings()
        {
            if (_config == null)
            {
                throw new InvalidOperationException("Ghost visual config is not assigned.");
            }

            return _config.CreateSettings();
        }

        public void Initialize(GhostVisualSettings settings)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Ghost visual is already initialized.");
            }

            if (settings.BobHeight < 0f
                || float.IsNaN(settings.BobHeight)
                || float.IsInfinity(settings.BobHeight)
                || settings.BobSpeed <= 0f
                || float.IsNaN(settings.BobSpeed)
                || float.IsInfinity(settings.BobSpeed)
                || settings.SquashAmount < 0f
                || float.IsNaN(settings.SquashAmount)
                || float.IsInfinity(settings.SquashAmount))
            {
                throw new ArgumentOutOfRangeException(nameof(settings));
            }

            _settings = settings;
            _elapsed = 0f;
            _isInitialized = true;
        }

        private void Awake()
        {
            if (_visual == null)
            {
                throw new InvalidOperationException("Ghost visual transform is not configured.");
            }

            _baseLocalPosition = _visual.localPosition;
            _baseLocalScale = _visual.localScale;
        }

        private void Update()
        {
            if (_isInitialized == false)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float wave = Mathf.Sin(_elapsed * _settings.BobSpeed);
            float verticalScale = 1f + wave * _settings.SquashAmount;
            float horizontalScale = 1f - wave * _settings.SquashAmount * 0.5f;
            _visual.localPosition = _baseLocalPosition + Vector3.up * (wave * _settings.BobHeight);
            _visual.localScale = Vector3.Scale(
                _baseLocalScale,
                new Vector3(horizontalScale, verticalScale, horizontalScale));
        }

        private void OnDisable()
        {
            if (_isInitialized == false)
            {
                return;
            }

            _visual.localPosition = _baseLocalPosition;
            _visual.localScale = _baseLocalScale;
        }
    }
}
