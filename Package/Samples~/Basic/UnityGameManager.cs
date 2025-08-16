using UnityEngine;
using MDI.Extensions;

namespace MDI.Examples.Basic
{
    /// <summary>
    /// Unity'de MDI+ kullanım örneği
    /// </summary>
    public class UnityGameManager : MonoBehaviour
    {
        [Header("Dependency Injection")]
        [Inject] private ILogger _logger;
        [Inject] private IGameService _gameService;
        [Inject] private IAudioService _audioService;

        [Header("Game Settings")]
        [SerializeField] private string playerName = "Player";
        [SerializeField] private int startingHealth = 100;

        private void Awake()
        {
            // Dependency injection yap
            MDIUnityHelper.Inject(this);
            
            // Service'lerin inject edilip edilmediğini kontrol et
            ValidateInjection();
            
            // Game'i başlat
            InitializeGame();
        }

        private void Start()
        {
            // Game logic'i başlat
            StartGame();
        }

        private void ValidateInjection()
        {
            if (_logger == null)
            {
                Debug.LogError("Logger service not injected!");
                return;
            }

            if (_gameService == null)
            {
                Debug.LogError("Game service not injected!");
                return;
            }

            if (_audioService == null)
            {
                Debug.LogError("Audio service not injected!");
                return;
            }

            Debug.Log("All services injected successfully!");
        }

        private void InitializeGame()
        {
            _logger.Log($"Initializing game for player: {playerName}");
            _gameService.InitializePlayer(playerName, startingHealth);
            _audioService.PlayBackgroundMusic("MainTheme");
        }

        private void StartGame()
        {
            _logger.Log("Game started!");
            _gameService.StartGame();
            
            // UI'ı güncelle
            UpdateUI();
        }

        private void UpdateUI()
        {
            var playerHealth = _gameService.GetPlayerHealth();
            var playerScore = _gameService.GetPlayerScore();
            
            _logger.Log($"Player Health: {playerHealth}, Score: {playerScore}");
        }

        // Unity Event'leri
        private void OnPlayerDamaged(int damage)
        {
            _gameService.DamagePlayer(damage);
            _audioService.PlaySound("PlayerHit");
            UpdateUI();
        }

        private void OnPlayerHealed(int healAmount)
        {
            _gameService.HealPlayer(healAmount);
            _audioService.PlaySound("PlayerHeal");
            UpdateUI();
        }

        private void OnPlayerScored(int points)
        {
            _gameService.AddScore(points);
            _audioService.PlaySound("Score");
            UpdateUI();
        }

        // Test için public method'lar
        [ContextMenu("Test Player Damaged")]
        public void TestPlayerDamaged()
        {
            OnPlayerDamaged(10);
        }

        [ContextMenu("Test Player Healed")]
        public void TestPlayerHealed()
        {
            OnPlayerHealed(20);
        }

        [ContextMenu("Test Player Scored")]
        public void TestPlayerScored()
        {
            OnPlayerScored(50);
        }
    }

    // Unity için service interface'leri
    public interface IGameService
    {
        void InitializePlayer(string playerName, int startingHealth);
        void StartGame();
        void DamagePlayer(int damage);
        void HealPlayer(int healAmount);
        void AddScore(int points);
        int GetPlayerHealth();
        int GetPlayerScore();
    }

    public interface IAudioService
    {
        void PlayBackgroundMusic(string musicName);
        void PlaySound(string soundName);
        void StopBackgroundMusic();
        void SetVolume(float volume);
    }

    // Unity için service implementation'ları
    public class UnityGameService : IGameService
    {
        private string _playerName;
        private int _playerHealth;
        private int _playerScore;
        private bool _gameStarted;

        public void InitializePlayer(string playerName, int startingHealth)
        {
            _playerName = playerName;
            _playerHealth = startingHealth;
            _playerScore = 0;
            Debug.Log($"Player {_playerName} initialized with {startingHealth} health");
        }

        public void StartGame()
        {
            _gameStarted = true;
            Debug.Log($"Game started for player {_playerName}");
        }

        public void DamagePlayer(int damage)
        {
            if (!_gameStarted) return;
            
            _playerHealth = Mathf.Max(0, _playerHealth - damage);
            Debug.Log($"Player {_playerName} took {damage} damage. Health: {_playerHealth}");
        }

        public void HealPlayer(int healAmount)
        {
            if (!_gameStarted) return;
            
            _playerHealth = Mathf.Min(100, _playerHealth + healAmount);
            Debug.Log($"Player {_playerName} healed {healAmount}. Health: {_playerHealth}");
        }

        public void AddScore(int points)
        {
            if (!_gameStarted) return;
            
            _playerScore += points;
            Debug.Log($"Player {_playerName} scored {points} points. Total: {_playerScore}");
        }

        public int GetPlayerHealth() => _playerHealth;
        public int GetPlayerScore() => _playerScore;
    }

    public class UnityAudioService : IAudioService
    {
        private AudioSource _backgroundMusicSource;
        private float _volume = 1.0f;

        public void PlayBackgroundMusic(string musicName)
        {
            Debug.Log($"Playing background music: {musicName}");
            // Audio clip loading ve playing logic'i burada olacak
        }

        public void PlaySound(string soundName)
        {
            Debug.Log($"Playing sound: {soundName}");
            // Sound effect playing logic'i burada olacak
        }

        public void StopBackgroundMusic()
        {
            Debug.Log("Stopping background music");
            // Background music'i durdur
        }

        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            Debug.Log($"Volume set to: {_volume}");
            // Volume ayarlama logic'i burada olacak
        }
    }
}
