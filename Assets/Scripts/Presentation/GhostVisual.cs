using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class GhostVisual : MonoBehaviour
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private float _bobHeight = 0.06f;
        [SerializeField] private float _bobSpeed = 2.4f;
        [SerializeField] private float _squashAmount = 0.025f;

        private Vector3 _baseLocalPosition;
        private Vector3 _baseLocalScale;
        private float _elapsed;
        private bool _isInitialized;

        public Transform Visual => _visual;

        private void Awake()
        {
            if (_visual == null)
            {
                throw new InvalidOperationException("Ghost visual transform is not configured.");
            }

            if (_bobHeight < 0f || _bobSpeed <= 0f || _squashAmount < 0f)
            {
                throw new InvalidOperationException("Ghost visual animation values are invalid.");
            }

            _baseLocalPosition = _visual.localPosition;
            _baseLocalScale = _visual.localScale;
            _isInitialized = true;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float wave = Mathf.Sin(_elapsed * _bobSpeed);
            float verticalScale = 1f + wave * _squashAmount;
            float horizontalScale = 1f - wave * _squashAmount * 0.5f;
            _visual.localPosition = _baseLocalPosition + Vector3.up * (wave * _bobHeight);
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
