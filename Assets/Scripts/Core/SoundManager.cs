using UnityEngine;
using DG.Tweening;

namespace MiniGameDemo.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        
        [SerializeField] private AudioClip _clickSound;
        [SerializeField] private AudioClip _rewardSound;
        [SerializeField] private AudioClip _bombSound;
        [SerializeField] private AudioClip _introMusic;

        private AudioSource _sfxSource;
        private AudioSource _musicSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Existing AudioSource for SFX
                _sfxSource = GetComponent<AudioSource>();
                
                // Create a second AudioSource purely for Music so they don't interrupt each other
                _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Subscribe to Game State changes to handle intro music automatically
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                // Trigger for the initial state
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.MainMenu)
            {
                PlayIntroMusic();
            }
            else
            {
                FadeOutIntroMusic(0.5f); // Half second fade out when playing starts
            }
        }

        public void PlayClick()
        {
            if (_clickSound != null && _sfxSource != null)
                _sfxSource.PlayOneShot(_clickSound);
        }

        public void PlayReward()
        {
            if (_rewardSound != null && _sfxSource != null)
                _sfxSource.PlayOneShot(_rewardSound);
        }

        public void PlayBomb()
        {
            if (_bombSound != null && _sfxSource != null)
                _sfxSource.PlayOneShot(_bombSound);
        }

        private void PlayIntroMusic()
        {
            if (_introMusic != null && _musicSource != null)
            {
                // Kill any ongoing fade tweens
                _musicSource.DOKill();
                
                if (_musicSource.clip != _introMusic)
                    _musicSource.clip = _introMusic;
                
                _musicSource.volume = 1f;
                if (!_musicSource.isPlaying)
                    _musicSource.Play();
            }
        }

        private void FadeOutIntroMusic(float duration)
        {
            if (_musicSource != null && _musicSource.isPlaying)
            {
                _musicSource.DOFade(0f, duration).OnComplete(() => _musicSource.Stop());
            }
        }
    }
}
