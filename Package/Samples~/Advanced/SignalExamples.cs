using UnityEngine;
using MDI.Patterns.Signal;
using System.Collections;

namespace MDI.Examples.Advanced
{
    /// <summary>
    /// Signal system kullanım örnekleri
    /// </summary>
    public class SignalExamples : MonoBehaviour
    {
        [Header("Signal Examples")]
        [SerializeField] private bool enableExamples = true;
        
        private string _playerDeathSubscriptionId;
        private string _scoreSubscriptionId;
        private string _gameStateSubscriptionId;

        private void Start()
        {
            if (enableExamples)
            {
                SetupSignalSubscriptions();
                StartCoroutine(DemoSignals());
            }
        }

        /// <summary>
        /// Signal subscription'larını kurar
        /// </summary>
        private void SetupSignalSubscriptions()
        {
            Debug.Log("Setting up signal subscriptions...");

            // Player death signal'a subscribe ol
            _playerDeathSubscriptionId = UnitySignalBusManager.Subscribe<PlayerDeathSignal>(
                OnPlayerDeath, priority: 10);

            // Score signal'a subscribe ol
            _scoreSubscriptionId = UnitySignalBusManager.Subscribe<ScoreSignal>(
                OnScoreChanged, priority: 5);

            // Game state signal'a subscribe ol
            _gameStateSubscriptionId = UnitySignalBusManager.Subscribe<GameStateSignal>(
                OnGameStateChanged, priority: 15);

            Debug.Log("Signal subscriptions setup completed!");
        }

        /// <summary>
        /// Demo signal'ları başlatır
        /// </summary>
        private IEnumerator DemoSignals()
        {
            yield return new WaitForSeconds(1f);

            Debug.Log("=== Starting Signal Demo ===");

            // 1. Immediate signal publish
            Debug.Log("1. Publishing immediate signals...");
            UnitySignalBusManager.Publish(new PlayerDeathSignal
            {
                PlayerId = "Player1",
                DeathPosition = new Vector3(10, 0, 5),
                DeathReason = "Enemy Attack"
            });

            UnitySignalBusManager.Publish(new ScoreSignal
            {
                PlayerId = "Player1",
                Points = 100,
                ScoreType = "Enemy Kill"
            });

            yield return new WaitForSeconds(2f);

            // 2. Delayed signal publish
            Debug.Log("2. Publishing delayed signals...");
            UnitySignalBusManager.PublishDelayed(new GameStateSignal
            {
                NewState = GameState.LevelComplete,
                LevelNumber = 1,
                CompletionTime = 120.5f
            }, 3000); // 3 saniye sonra

            yield return new WaitForSeconds(1f);

            // 3. Conditional signal publish
            Debug.Log("3. Publishing conditional signals...");
            UnitySignalBusManager.PublishWhen(new PowerUpSignal
            {
                PowerUpType = "Shield",
                Duration = 10f,
                PlayerId = "Player1"
            }, () => Input.GetKeyDown(KeyCode.Space));

            Debug.Log("Press SPACE to trigger conditional power-up signal!");

            yield return new WaitForSeconds(2f);

            // 4. High priority signal
            Debug.Log("4. Publishing high priority signal...");
            UnitySignalBusManager.Publish(new EmergencySignal
            {
                EmergencyType = "Boss Spawn",
                Location = new Vector3(0, 0, 0),
                Priority = 100
            });

            yield return new WaitForSeconds(2f);

            // 5. Async signal publish
            Debug.Log("5. Publishing async signals...");
            _ = UnitySignalBusManager.PublishAsync(new AchievementSignal
            {
                AchievementId = "FirstBlood",
                PlayerId = "Player1",
                UnlockTime = System.DateTime.UtcNow
            });

            Debug.Log("=== Signal Demo Completed ===");
        }

        #region Signal Handlers

        /// <summary>
        /// Player death signal handler
        /// </summary>
        private void OnPlayerDeath(PlayerDeathSignal signal)
        {
            Debug.Log($"🚨 PLAYER DEATH: {signal.PlayerId} died at {signal.DeathPosition} due to {signal.DeathReason}");
            
            // Game logic burada olacak
            // - UI güncelleme
            // - Sound effect
            // - Particle effect
            // - Respawn logic
        }

        /// <summary>
        /// Score signal handler
        /// </summary>
        private void OnScoreChanged(ScoreSignal signal)
        {
            Debug.Log($"🎯 SCORE: {signal.PlayerId} gained {signal.Points} points for {signal.ScoreType}");
            
            // Game logic burada olacak
            // - Score UI güncelleme
            // - Achievement check
            // - Sound effect
        }

        /// <summary>
        /// Game state signal handler
        /// </summary>
        private void OnGameStateChanged(GameStateSignal signal)
        {
            Debug.Log($"🎮 GAME STATE: Changed to {signal.NewState} (Level {signal.LevelNumber})");
            
            // Game logic burada olacak
            // - UI state değişikliği
            // - Music değişikliği
            // - Level loading
        }

        #endregion

        /// <summary>
        /// Test için public method'lar
        /// </summary>
        [ContextMenu("Test Player Death Signal")]
        public void TestPlayerDeathSignal()
        {
            UnitySignalBusManager.Publish(new PlayerDeathSignal
            {
                PlayerId = "TestPlayer",
                DeathPosition = transform.position,
                DeathReason = "Test"
            });
        }

        [ContextMenu("Test Score Signal")]
        public void TestScoreSignal()
        {
            UnitySignalBusManager.Publish(new ScoreSignal
            {
                PlayerId = "TestPlayer",
                Points = 50,
                ScoreType = "Test"
            });
        }

        [ContextMenu("Test Emergency Signal")]
        public void TestEmergencySignal()
        {
            UnitySignalBusManager.Publish(new EmergencySignal
            {
                EmergencyType = "Test Emergency",
                Location = transform.position,
                Priority = 50
            });
        }

        [ContextMenu("Clear All Signals")]
        public void ClearAllSignals()
        {
            UnitySignalBusManager.Clear();
            Debug.Log("All signals cleared!");
        }

        private void OnDestroy()
        {
            // Subscription'ları temizle
            if (!string.IsNullOrEmpty(_playerDeathSubscriptionId))
                UnitySignalBusManager.Unsubscribe(_playerDeathSubscriptionId);
                
            if (!string.IsNullOrEmpty(_scoreSubscriptionId))
                UnitySignalBusManager.Unsubscribe(_scoreSubscriptionId);
                
            if (!string.IsNullOrEmpty(_gameStateSubscriptionId))
                UnitySignalBusManager.Unsubscribe(_gameStateSubscriptionId);
        }
    }

    #region Signal Definitions

    /// <summary>
    /// Player death signal
    /// </summary>
    public class PlayerDeathSignal : BaseSignal
    {
        public string PlayerId { get; set; }
        public Vector3 DeathPosition { get; set; }
        public string DeathReason { get; set; }

        public PlayerDeathSignal() : base(priority: 10) { }
    }

    /// <summary>
    /// Score signal
    /// </summary>
    public class ScoreSignal : BaseSignal
    {
        public string PlayerId { get; set; }
        public int Points { get; set; }
        public string ScoreType { get; set; }

        public ScoreSignal() : base(priority: 5) { }
    }

    /// <summary>
    /// Game state signal
    /// </summary>
    public class GameStateSignal : BaseSignal
    {
        public GameState NewState { get; set; }
        public int LevelNumber { get; set; }
        public float CompletionTime { get; set; }

        public GameStateSignal() : base(priority: 15) { }
    }

    /// <summary>
    /// Power-up signal
    /// </summary>
    public class PowerUpSignal : BaseSignal
    {
        public string PowerUpType { get; set; }
        public float Duration { get; set; }
        public string PlayerId { get; set; }

        public PowerUpSignal() : base(priority: 8) { }
    }

    /// <summary>
    /// Emergency signal
    /// </summary>
    public class EmergencySignal : BaseSignal
    {
        public string EmergencyType { get; set; }
        public Vector3 Location { get; set; }

        public EmergencySignal() : base(priority: 100) { }
    }

    /// <summary>
    /// Achievement signal
    /// </summary>
    public class AchievementSignal : BaseSignal
    {
        public string AchievementId { get; set; }
        public string PlayerId { get; set; }
        public System.DateTime UnlockTime { get; set; }

        public AchievementSignal() : base(priority: 20) { }
    }

    /// <summary>
    /// Game state enum
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        LevelComplete,
        GameOver,
        Victory
    }

    #endregion
}
