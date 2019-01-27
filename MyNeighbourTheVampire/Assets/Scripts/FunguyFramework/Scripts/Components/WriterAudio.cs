// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Fungus
{
    /// <summary>
    /// Type of audio effect to play.
    /// </summary>
    public enum AudioMode
    {
        /// <summary> Use short beep sound effects. </summary>
        Beeps,
        /// <summary> Use long looping sound effect. </summary>
        SoundEffect,
        /// <summary> Use per vowel boop sounds. </summary>
        Boopy,
    }

    /// <summary>
    /// Manages audio effects for Dialogs.
    /// </summary>
    public class WriterAudio : MonoBehaviour, IWriterListener
    {
        [Tooltip("Volume level of writing sound effects")]
        [Range(0,1)]
        [SerializeField] protected float volume = 1f;

        [Tooltip("Loop the audio when in Sound Effect mode. Has no effect in Beeps mode.")]
        [SerializeField] protected bool loop = true;

        // If none is specifed then we use any AudioSource on the gameobject, and if that doesn't exist we create one.
        [Tooltip("AudioSource to use for playing sound effects. If none is selected then one will be created.")]
        [SerializeField] protected AudioSource targetAudioSource;

        [Tooltip("Type of sound effect to play when writing text")]
        [SerializeField] protected AudioMode audioMode = AudioMode.Beeps;

        [Tooltip("List of beeps to randomly select when playing beep sound effects. Will play maximum of one beep per character, with only one beep playing at a time.")]
        [SerializeField] protected List<AudioClip> beepSounds = new List<AudioClip>();

        [Tooltip("List of beeps to randomly select when playing custom boop sound effects. Will play maximum of one beep per character, with only one beep playing at a time.")]
        [SerializeField] protected List<AudioClip> boopSounds = new List<AudioClip>();

        [Tooltip("Long playing sound effect to play when writing text")]
        [SerializeField] protected AudioClip soundEffect;

        [Tooltip("Sound effect to play on user input (e.g. a click)")]
        [SerializeField] protected AudioClip inputSound;

        protected float targetVolume = 0f;

        // When true, a beep will be played on every written character glyph
        protected bool playBeeps;

        protected bool playBoops;

        // True when a voiceover clip is playing
        protected bool playingVoiceover = false;

        public bool IsPlayingVoiceOver { get { return playingVoiceover; } }

        // Time when current beep will have finished playing
        protected float nextBeepTime;


        public float GetSecondsRemaining()
        {
            if (IsPlayingVoiceOver)
            {
                return targetAudioSource.clip.length - targetAudioSource.time;
            }
            else
            {
                return 0F;
            }
        }

        protected virtual void SetAudioMode(AudioMode mode)
        {
            audioMode = mode;
        }

        protected virtual void Awake()
        {
            // Need to do this in Awake rather than Start due to init order issues
            if (targetAudioSource == null)
            {
                targetAudioSource = GetComponent<AudioSource>();
                if (targetAudioSource == null)
                {
                    targetAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            targetAudioSource.volume = 0f;
        }

        protected virtual void Play(AudioClip audioClip)
        {
            if (targetAudioSource == null ||
                (audioMode == AudioMode.SoundEffect && soundEffect == null && audioClip == null) ||
                (audioMode == AudioMode.Beeps && beepSounds.Count == 0))
            {
                return;
            }

            playingVoiceover = false;
            targetAudioSource.volume = 0f;
            targetVolume = volume;

            if (audioClip != null)
            {
                // Voice over clip provided
                targetAudioSource.clip = audioClip;
                targetAudioSource.loop = loop;
                targetAudioSource.Play();
            }
            else if (audioMode == AudioMode.SoundEffect &&
                     soundEffect != null)
            {
                // Use sound effects defined in WriterAudio
                targetAudioSource.clip = soundEffect;
                targetAudioSource.loop = loop;
                targetAudioSource.Play();
            }
            else if (audioMode == AudioMode.Beeps)
            {
                // Use beeps defined in WriterAudio
                targetAudioSource.clip = null;
                targetAudioSource.loop = false;
                playBeeps = true;
            }
            else if (audioMode == AudioMode.Boopy)
            {
                // Use beeps defined in WriterAudio
                targetAudioSource.clip = null;
                targetAudioSource.loop = false;
                playBoops = true;
            }
        }

        protected virtual void Pause()
        {
            if (targetAudioSource == null)
            {
                return;
            }

            // There's an audible click if you call audioSource.Pause() so instead just drop the volume to 0.
            targetVolume = 0f;
        }

        protected virtual void Stop()
        {
            if (targetAudioSource == null)
            {
                return;
            }

            // There's an audible click if you call audioSource.Stop() so instead we just switch off
            // looping and let the audio stop automatically at the end of the clip
            targetVolume = 0f;
            targetAudioSource.loop = false;
            playBeeps = false;
            playBoops = false;
            playingVoiceover = false;
        }

        protected virtual void Resume()
        {
            if (targetAudioSource == null)
            {
                return;
            }

            targetVolume = volume;
        }

        protected virtual void Update()
        {
            targetAudioSource.volume = Mathf.MoveTowards(targetAudioSource.volume, targetVolume, Time.deltaTime * 5f);
        }

        #region IWriterListener implementation

        public virtual void OnInput()
        {
            if (inputSound != null)
            {
                // Assumes we're playing a 2D sound
                AudioSource.PlayClipAtPoint(inputSound, Vector3.zero);
            }
        }

        public virtual void OnStart(AudioClip audioClip)
        {
            if (playingVoiceover)
            {
                return;
            }
            Play(audioClip);
        }
        
        public virtual void OnPause()
        {
            if (playingVoiceover)
            {
                return;
            }
            Pause();
        }
        
        public virtual void OnResume()
        {
            if (playingVoiceover)
            {
                return;
            }
            Resume();
        }
        
        public virtual void OnEnd(bool stopAudio)
        {
            if (stopAudio)
            {
                Stop();
            }
        }

        public virtual void OnGlyph(string glyphs)
        {
            if (playingVoiceover)
            {
                return;
            }

            if (playBoops && boopSounds.Count >= 7 )
            {
                // Read glpyh for mapped sounds
                int boopIndex = -1;
                string[] keys = new string[] { "a", "o", "e", "u", "i", "r", "p" };
                string sKeyResult = keys.FirstOrDefault(s => glyphs.Contains(s));
                switch (sKeyResult)
                {
                    case "a":
                        boopIndex = 0;
                        break;
                    case "o":
                        boopIndex = 1;
                        break;
                    case "e":
                        boopIndex = 2;
                        break;
                    case "u":
                        boopIndex = 3;
                        break;
                    case "i":
                        boopIndex = 4;
                        break;
                    case "r":
                        boopIndex = 5;
                        break;
                    case "p":
                        boopIndex = 6;
                        break;
                }
                if (boopIndex == -1) { return; }
                if(targetAudioSource.clip !=  null)
                {
                    targetAudioSource.Stop();
                }
                targetAudioSource.clip = boopSounds[boopIndex];
                if (targetAudioSource.clip != null)
                {
                    targetAudioSource.loop = false;
                    targetVolume = volume;
                    targetAudioSource.Play();

                    float extend = targetAudioSource.clip.length;
                    nextBeepTime = Time.realtimeSinceStartup + extend;
                }
            }
            else if (playBeeps && beepSounds.Count > 0)
            {
                if (!targetAudioSource.isPlaying)

                {
                    if (nextBeepTime < Time.realtimeSinceStartup)
                    {
                        targetAudioSource.clip = beepSounds[Random.Range(0, beepSounds.Count - 1)];

                        if (targetAudioSource.clip != null)
                        {
                            targetAudioSource.loop = false;
                            targetVolume = volume;
                            targetAudioSource.Play();

                            float extend = targetAudioSource.clip.length;
                            nextBeepTime = Time.realtimeSinceStartup + extend;
                        }
                    }
                }
            }
        }

        public virtual void OnVoiceover(AudioClip voiceOverClip)
        {
            if (targetAudioSource == null)
            {
                return;
            }

            playingVoiceover = true;

            targetAudioSource.volume = volume;
            targetVolume = volume;
            targetAudioSource.loop = false;
            targetAudioSource.clip = voiceOverClip;
            targetAudioSource.Play();
        }
            
        #endregion
    }
}