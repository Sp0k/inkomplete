using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace GameManagement
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Sounds list")]
        [SerializeField] private List<Sound> _sfxClips;
        [SerializeField] private List<Sound> _musicClips;

        private static AudioManager _instace;
        public static AudioManager Instance
        {
            get
            {
                if (_instace == null)
                {
                    _instace = FindAnyObjectByType<AudioManager>();

                    if (_instace == null)
                    {
                        GameObject singletonObj = new GameObject("AudioManager");
                        _instace = singletonObj.AddComponent<AudioManager>();
                    }
                }

                return _instace;
            }
        }
    
        #region Unity Functions

        private void Awake()
        {
            if (_instace == null)
            {
                _instace = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            InitializeClips(_sfxClips);
            InitializeClips(_musicClips);
        }

        #endregion

        #region Audio Functions

        private void InitializeClips(List<Sound> clipList)
        {
            foreach(Sound s in clipList)
            {
                InializeSound(s);
            }
        }

        private void InializeSound(Sound s)
        {

            s.Source = gameObject.AddComponent<AudioSource>();
            s.Source.clip = s.Clip;
            s.Source.volume = s.Volume;
            s.Source.pitch = s.Pitch;
            s.Source.loop = s.Loop;
        }

        public void PlaySfx(string sfxId, bool randomDelay = false, bool randomPitch = false)
        {
            StartCoroutine(PlaySfxCoroutine(sfxId, randomDelay, randomPitch));
        }


        public void PlaySong(string songId)
        {

        }

        #endregion

        #region Helper functions

        private bool FindClip(List<Sound> audioList, string clipId, out Sound entry)
        {
            entry = new();

            int idx = audioList.FindIndex(clip => clip.Id == clipId);
            if (idx == -1) return false;

            entry = audioList[idx];
            return true;
        }

        #endregion

        #region Coroutines

        public IEnumerator PlaySfxCoroutine(string sfxId, bool randomDelay = false, bool randomPitch = false)
        {
            if (!FindClip(_sfxClips, sfxId, out Sound clip))
            {
                Debug.LogWarning($"Sfx clip with id: {sfxId} doesn't exist");
                yield return null;
            }

            Sound clone = clip.Clone();
            InializeSound(clone);

            if (randomDelay)
            {
                float randomValue = UnityEngine.Random.Range(0f, 0.4f);
                yield return new WaitForSeconds(randomValue);
            }

            if (randomPitch)
            {
                float randomValue = UnityEngine.Random.Range(0.9f, 1.1f);
                clip.Source.pitch = randomValue;
            }

            clone.Source.Play();

            yield return null;
        }

        #endregion
    }

    [Serializable]
    public class Sound
    {
        public string Id;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 1f;
        [Range(0.1f, 3f)] public float Pitch = 1f;
        public bool Loop;

        [HideInInspector] public AudioSource Source;

        public Sound Clone()
        {
            return new()
            {
                Id = Id,
                Clip = Clip,
                Volume = Volume,
                Pitch = Pitch,
                Loop = Loop,
            };
        }
    }
}
