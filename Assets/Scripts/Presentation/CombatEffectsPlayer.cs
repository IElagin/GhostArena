using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhostArena
{
    public sealed class CombatEffectsPlayer
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly MonoBehaviour _coroutineOwner;
        private readonly Transform _effectRoot;
        private readonly Dictionary<GameObject, FlashState> _flashes =
            new Dictionary<GameObject, FlashState>();
        private readonly List<ParticleSystem> _activeEffects = new List<ParticleSystem>();

        public CombatEffectsPlayer(MonoBehaviour coroutineOwner, Transform effectRoot)
        {
            _coroutineOwner = coroutineOwner != null
                ? coroutineOwner
                : throw new ArgumentNullException(nameof(coroutineOwner));
            _effectRoot = effectRoot != null
                ? effectRoot
                : throw new ArgumentNullException(nameof(effectRoot));
        }

        public void Play(ParticleSystem effectPrefab, Vector3 position)
        {
            RemoveDestroyedEffects();
            ParticleSystem effect = UnityEngine.Object.Instantiate(
                effectPrefab,
                position,
                Quaternion.identity,
                _effectRoot);
            _activeEffects.Add(effect);
            effect.Play(true);
            float lifetime = effect.main.duration + effect.main.startLifetime.constantMax;
            UnityEngine.Object.Destroy(effect.gameObject, lifetime);
        }

        public void Flash(
            GameObject actor,
            Color flashColor,
            Color flashEmission,
            float duration)
        {
            if (actor == null)
            {
                return;
            }

            if (_flashes.TryGetValue(actor, out FlashState flashState))
            {
                _coroutineOwner.StopCoroutine(flashState.Coroutine);
            }
            else
            {
                flashState = CaptureFlashState(actor);

                if (flashState.Slots.Count == 0)
                {
                    return;
                }

                _flashes.Add(actor, flashState);
            }

            ApplyFlash(flashState, flashColor, flashEmission);
            flashState.Coroutine = _coroutineOwner.StartCoroutine(
                RestoreFlashAfterDelay(actor, flashState, duration));
        }

        public void TickTerminal(float unscaledDeltaTime)
        {
            for (int index = _activeEffects.Count - 1; index >= 0; index--)
            {
                ParticleSystem effect = _activeEffects[index];

                if (effect == null)
                {
                    _activeEffects.RemoveAt(index);
                    continue;
                }

                effect.Simulate(unscaledDeltaTime, true, false, false);

                if (effect.IsAlive(true) == false)
                {
                    effect.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(effect.gameObject);
                    _activeEffects.RemoveAt(index);
                }
            }
        }

        public void RestoreFlashes()
        {
            foreach (FlashState flashState in _flashes.Values)
            {
                if (_coroutineOwner != null && flashState.Coroutine != null)
                {
                    _coroutineOwner.StopCoroutine(flashState.Coroutine);
                }

                RestoreFlash(flashState);
            }

            _flashes.Clear();
        }

        public void Clear()
        {
            RestoreFlashes();

            foreach (ParticleSystem effect in _activeEffects)
            {
                if (effect != null)
                {
                    effect.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(effect.gameObject);
                }
            }

            _activeEffects.Clear();
        }

        private static FlashState CaptureFlashState(GameObject actor)
        {
            FlashState flashState = new FlashState();

            foreach (Renderer renderer in actor.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];

                    if (material == null)
                    {
                        continue;
                    }

                    bool hasBaseColor = material.HasProperty(BaseColorId);
                    bool hasEmission = material.HasProperty(EmissionColorId);

                    if (hasBaseColor == false && hasEmission == false)
                    {
                        continue;
                    }

                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block, materialIndex);
                    Color baseColor = hasBaseColor
                        ? block.HasColor(BaseColorId)
                            ? block.GetColor(BaseColorId)
                            : material.GetColor(BaseColorId)
                        : Color.clear;
                    Color emissionColor = hasEmission
                        ? block.HasColor(EmissionColorId)
                            ? block.GetColor(EmissionColorId)
                            : material.GetColor(EmissionColorId)
                        : Color.clear;
                    flashState.Slots.Add(new MaterialSlotState(
                        renderer,
                        materialIndex,
                        block,
                        hasBaseColor,
                        baseColor,
                        hasEmission,
                        emissionColor));
                }
            }

            return flashState;
        }

        private static void ApplyFlash(
            FlashState flashState,
            Color flashColor,
            Color flashEmission)
        {
            foreach (MaterialSlotState slot in flashState.Slots)
            {
                if (slot.Renderer == null)
                {
                    continue;
                }

                if (slot.HasBaseColor)
                {
                    slot.Block.SetColor(BaseColorId, flashColor);
                }

                if (slot.HasEmission)
                {
                    slot.Block.SetColor(EmissionColorId, flashEmission);
                }

                slot.Renderer.SetPropertyBlock(slot.Block, slot.MaterialIndex);
            }
        }

        private static void RestoreFlash(FlashState flashState)
        {
            foreach (MaterialSlotState slot in flashState.Slots)
            {
                if (slot.Renderer == null)
                {
                    continue;
                }

                if (slot.HasBaseColor)
                {
                    slot.Block.SetColor(BaseColorId, slot.BaseColor);
                }

                if (slot.HasEmission)
                {
                    slot.Block.SetColor(EmissionColorId, slot.EmissionColor);
                }

                slot.Renderer.SetPropertyBlock(slot.Block, slot.MaterialIndex);
            }
        }

        private void RemoveDestroyedEffects()
        {
            for (int index = _activeEffects.Count - 1; index >= 0; index--)
            {
                if (_activeEffects[index] == null)
                {
                    _activeEffects.RemoveAt(index);
                }
            }
        }

        private IEnumerator RestoreFlashAfterDelay(
            GameObject actor,
            FlashState flashState,
            float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            RestoreFlash(flashState);
            _flashes.Remove(actor);
        }

        private sealed class FlashState
        {
            public readonly List<MaterialSlotState> Slots = new List<MaterialSlotState>();
            public Coroutine Coroutine;
        }

        private sealed class MaterialSlotState
        {
            public MaterialSlotState(
                Renderer renderer,
                int materialIndex,
                MaterialPropertyBlock block,
                bool hasBaseColor,
                Color baseColor,
                bool hasEmission,
                Color emissionColor)
            {
                Renderer = renderer;
                MaterialIndex = materialIndex;
                Block = block;
                HasBaseColor = hasBaseColor;
                BaseColor = baseColor;
                HasEmission = hasEmission;
                EmissionColor = emissionColor;
            }

            public Renderer Renderer { get; }

            public int MaterialIndex { get; }

            public MaterialPropertyBlock Block { get; }

            public bool HasBaseColor { get; }

            public Color BaseColor { get; }

            public bool HasEmission { get; }

            public Color EmissionColor { get; }
        }
    }
}
