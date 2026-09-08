using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class ActorHealth : MonoBehaviour
    {
        private Health _model;

        public event Action Changed;
        public event Action Died;

        public Health Model => _model;

        public int Maximum => _model == null ? 0 : _model.Maximum;

        public int Current => _model == null ? 0 : _model.Current;

        public bool IsAlive => _model != null && _model.IsAlive;

        public void Initialize(int maximum)
        {
            if (_model != null)
            {
                throw new InvalidOperationException("Actor health is already initialized.");
            }

            _model = new Health(maximum);
            _model.Changed += OnChanged;
            _model.Died += OnDied;
        }

        public void TakeDamage(int amount)
        {
            if (_model == null)
            {
                throw new InvalidOperationException("Actor health is not initialized.");
            }

            _model.TakeDamage(amount);
        }

        private void OnDestroy()
        {
            if (_model == null)
            {
                return;
            }

            _model.Changed -= OnChanged;
            _model.Died -= OnDied;
        }

        private void OnChanged()
        {
            Changed?.Invoke();
        }

        private void OnDied()
        {
            Died?.Invoke();
        }
    }
}
