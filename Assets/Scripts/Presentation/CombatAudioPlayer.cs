using System;
using UnityEngine;

namespace GhostArena
{
    public sealed class CombatAudioPlayer
    {
        private readonly AudioSource _resultSource;
        private readonly AudioSource[] _sfxVoices;
        private int _nextSfxVoice;

        public CombatAudioPlayer(AudioSource resultSource, AudioSource[] sfxVoices)
        {
            _resultSource = resultSource != null
                ? resultSource
                : throw new ArgumentNullException(nameof(resultSource));

            if (sfxVoices == null)
            {
                throw new ArgumentNullException(nameof(sfxVoices));
            }

            if (sfxVoices.Length == 0)
            {
                throw new ArgumentException("At least one SFX voice is required.", nameof(sfxVoices));
            }

            _sfxVoices = sfxVoices;
        }

        public void Configure(float sfxSourceGain, float resultSourceGain)
        {
            _resultSource.volume = resultSourceGain;

            foreach (AudioSource sfxVoice in _sfxVoices)
            {
                sfxVoice.volume = sfxSourceGain;
            }
        }

        public void Play(AudioClip clip, float gain)
        {
            AudioSource sfxVoice = _sfxVoices[_nextSfxVoice];
            _nextSfxVoice = (_nextSfxVoice + 1) % _sfxVoices.Length;
            sfxVoice.Stop();
            sfxVoice.PlayOneShot(clip, gain);
        }

        public void PlayResult(AudioClip clip, float gain)
        {
            _resultSource.Stop();
            _resultSource.PlayOneShot(clip, gain);
        }

        public void SetPaused(bool isPaused)
        {
            AudioListener.pause = isPaused;
        }

        public void Clear()
        {
            if (_resultSource != null)
            {
                _resultSource.Stop();
            }

            foreach (AudioSource sfxVoice in _sfxVoices)
            {
                if (sfxVoice != null)
                {
                    sfxVoice.Stop();
                }
            }

            _nextSfxVoice = 0;
            SetPaused(false);
        }
    }
}
