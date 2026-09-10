using System;
using System.Collections;
using UnityEngine;

namespace GhostArena
{
    public sealed class CharacterAnimation : MonoBehaviour
    {
        private const int BaseLayerIndex = 0;

        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int ShootState = Animator.StringToHash("Base Layer.Shoot");
        private static readonly int HitState = Animator.StringToHash("Base Layer.Hit");
        private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");

        [SerializeField] private Animator _animator;

        private Character _character;
        private IVelocitySource _velocitySource;
        private bool _isDying;
        private bool _isInitialized;

        public void Initialize(Character character)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Character animation is already initialized.");
            }

            if (_animator == null)
            {
                throw new InvalidOperationException("Character Animator is not configured.");
            }

            _character = character ?? throw new ArgumentNullException(nameof(character));

            if (transform == _character.transform || transform.IsChildOf(_character.transform) == false)
            {
                throw new InvalidOperationException(
                    "CharacterAnimation must be placed on the character Visual child.");
            }

            if (_animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException("Character Animator has no controller.");
            }

            _velocitySource = _character.DirectionalMover;

            if (_velocitySource == null)
            {
                _velocitySource = _character.DestinationMover;
            }

            if (_velocitySource == null)
            {
                throw new InvalidOperationException("Character animation needs a velocity source.");
            }

            _character.Health.Changed += OnHealthChanged;
            _character.Died += OnDied;

            if (_character.Weapon != null)
            {
                _character.Weapon.Shot += OnShot;
            }

            _animator.SetFloat(SpeedParameter, 0f);
            _isInitialized = true;
        }

        public void SetPaused(bool isPaused)
        {
            if (_animator != null)
            {
                _animator.speed = isPaused ? 0f : 1f;
            }
        }

        private void Update()
        {
            if (_isInitialized == false || _isDying || _character == null)
            {
                return;
            }

            _animator.SetFloat(SpeedParameter, _velocitySource.CurrentVelocity.magnitude);
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnHealthChanged()
        {
            if (_isDying || _character.Health.IsAlive == false)
            {
                return;
            }

            Play(HitState);
        }

        private void OnShot(Projectile projectile)
        {
            if (_isDying == false)
            {
                Play(ShootState);
            }
        }

        private void OnDied(Character character)
        {
            if (_isDying)
            {
                return;
            }

            _isDying = true;
            Transform runtimeRoot = character.transform.parent;
            transform.SetParent(runtimeRoot, true);
            Unbind();
            _animator.SetFloat(SpeedParameter, 0f);
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            Play(DeathState);
            _animator.Update(0f);
            StartCoroutine(DestroyAfterDeath());
        }

        private void Play(int state)
        {
            _animator.Play(state, BaseLayerIndex, 0f);
        }

        private IEnumerator DestroyAfterDeath()
        {
            while (_animator != null)
            {
                AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(BaseLayerIndex);

                if (state.fullPathHash == DeathState && state.normalizedTime >= 1f)
                {
                    break;
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private void Unbind()
        {
            if (ReferenceEquals(_character, null))
            {
                return;
            }

            if (ReferenceEquals(_character.Health, null) == false)
            {
                _character.Health.Changed -= OnHealthChanged;
            }

            _character.Died -= OnDied;

            if (ReferenceEquals(_character.Weapon, null) == false)
            {
                _character.Weapon.Shot -= OnShot;
            }

            _character = null;
            _velocitySource = null;
        }
    }
}
