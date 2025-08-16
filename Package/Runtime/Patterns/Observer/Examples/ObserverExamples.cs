using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MDI.Patterns.Observer;

namespace MDI.Patterns.Observer.Examples
{
    /// <summary>
    /// Observer Pattern örnek kullanımları
    /// </summary>
    public class ObserverExamples : MonoBehaviour
    {
        [Header("Example Settings")]
        [SerializeField] private bool runExamplesOnStart = true;
        [SerializeField] private float exampleDelay = 1f;

        #region Unity Lifecycle

        private void Start()
        {
            if (runExamplesOnStart)
            {
                StartCoroutine(RunExamples());
            }
        }

        #endregion

        #region Example Runner

        private IEnumerator RunExamples()
        {
            Debug.Log("=== Observer Pattern Examples Started ===");

            yield return new WaitForSeconds(exampleDelay);
            ReactivePropertyExample();

            yield return new WaitForSeconds(exampleDelay);
            SubjectExample();

            yield return new WaitForSeconds(exampleDelay);
            BehaviorSubjectExample();

            yield return new WaitForSeconds(exampleDelay);
            ReplaySubjectExample();

            yield return new WaitForSeconds(exampleDelay);
            GameplayExample();

            yield return new WaitForSeconds(exampleDelay);
            UIBindingExample();

            yield return new WaitForSeconds(exampleDelay);
            FilterAndThrottleExample();

            Debug.Log("=== Observer Pattern Examples Completed ===");
        }

        #endregion

        #region Basic Examples

        /// <summary>
        /// ReactiveProperty temel kullanım örneği
        /// </summary>
        private void ReactivePropertyExample()
        {
            Debug.Log("--- ReactiveProperty Example ---");

            // ReactiveProperty oluştur
            var playerHealth = new ReactiveProperty<int>(100, "PlayerHealth");

            // Observer ekle
            playerHealth.Subscribe(health =>
            {
                Debug.Log($"Player health changed: {health}");
                
                if (health <= 0)
                {
                    Debug.Log("Player died!");
                }
                else if (health <= 20)
                {
                    Debug.Log("Player health is critical!");
                }
            });

            // Değer değişikliklerini test et
            playerHealth.Value = 80;
            playerHealth.Value = 50;
            playerHealth.Value = 15;
            playerHealth.Value = 0;

            playerHealth.Dispose();
        }

        /// <summary>
        /// Subject temel kullanım örneği
        /// </summary>
        private void SubjectExample()
        {
            Debug.Log("--- Subject Example ---");

            // Subject oluştur
            var gameEvents = new Subject<string>(default(string), "GameEvents");

            // Observer'lar ekle
            gameEvents.Subscribe(eventName =>
            {
                Debug.Log($"UI received event: {eventName}");
            });

            gameEvents.Subscribe(eventName =>
            {
                Debug.Log($"Audio system received event: {eventName}");
            });

            gameEvents.Subscribe(eventName =>
            {
                Debug.Log($"Analytics received event: {eventName}");
            });

            // Event'leri yayınla
            gameEvents.SetValue("PlayerJumped");
            gameEvents.SetValue("EnemyDefeated");
            gameEvents.SetValue("LevelCompleted");

            gameEvents.Dispose();
        }

        /// <summary>
        /// BehaviorSubject kullanım örneği
        /// </summary>
        private void BehaviorSubjectExample()
        {
            Debug.Log("--- BehaviorSubject Example ---");

            // BehaviorSubject oluştur (başlangıç değeri ile)
            var gameState = new BehaviorSubject<GameState>(GameState.MainMenu, "GameState");

            // İlk observer - mevcut değeri hemen alır
            gameState.Subscribe(state =>
            {
                Debug.Log($"Game state observer 1: {state}");
            });

            // State değiştir
            gameState.SetValue(GameState.Playing);
            gameState.SetValue(GameState.Paused);

            // İkinci observer - mevcut değeri (Paused) hemen alır
            gameState.Subscribe(state =>
            {
                Debug.Log($"Game state observer 2 (late subscriber): {state}");
            });

            gameState.SetValue(GameState.GameOver);

            gameState.Dispose();
        }

        /// <summary>
        /// ReplaySubject kullanım örneği
        /// </summary>
        private void ReplaySubjectExample()
        {
            Debug.Log("--- ReplaySubject Example ---");

            // ReplaySubject oluştur (son 3 değeri saklar)
            var playerActions = new ReplaySubject<string>(3, "PlayerActions");

            // Bazı aksiyonlar gerçekleştir
            playerActions.SetValue("Jump");
            playerActions.SetValue("Attack");
            playerActions.SetValue("Defend");
            playerActions.SetValue("Run");

            // Yeni observer - son 3 aksiyonu hemen alır
            playerActions.Subscribe(action =>
            {
                Debug.Log($"Action replay observer: {action}");
            });

            playerActions.SetValue("Heal");

            playerActions.Dispose();
        }

        #endregion

        #region Advanced Examples

        /// <summary>
        /// Oyun içi kullanım örneği
        /// </summary>
        private void GameplayExample()
        {
            Debug.Log("--- Gameplay Example ---");

            // Oyuncu durumu
            var playerData = new PlayerData();

            // Skor değişikliklerini dinle
            playerData.Score.Subscribe(score =>
            {
                Debug.Log($"Score updated: {score}");
                
                // Başarım kontrolü
                if (score >= 1000)
                {
                    Debug.Log("Achievement unlocked: Score Master!");
                }
            });

            // Seviye değişikliklerini dinle
            playerData.Level.Subscribe(level =>
            {
                Debug.Log($"Level up! New level: {level}");
                
                // Yeni seviyede sağlık yenile
                playerData.Health.Value = 100;
            });

            // Sağlık değişikliklerini dinle
            playerData.Health.Subscribe(health =>
            {
                Debug.Log($"Health: {health}/100");
                
                if (health <= 0)
                {
                    Debug.Log("Game Over!");
                }
            });

            // Oyun simülasyonu
            playerData.Score.Value = 250;
            playerData.Score.Value = 500;
            playerData.Health.Value = 75;
            playerData.Score.Value = 1000; // Achievement tetiklenir
            playerData.Level.Value = 2; // Level up, sağlık yenilenir
            playerData.Health.Value = 0; // Game over

            playerData.Dispose();
        }

        /// <summary>
        /// UI binding örneği
        /// </summary>
        private void UIBindingExample()
        {
            Debug.Log("--- UI Binding Example ---");

            // UI verisi
            var uiData = new UIData();

            // UI elementlerini simüle et
            var healthBar = new HealthBarUI();
            var scoreText = new ScoreTextUI();
            var inventoryUI = new InventoryUI();

            // UI binding'leri kur
            uiData.PlayerHealth.Subscribe(health => healthBar.UpdateHealth(health));
            uiData.PlayerScore.Subscribe(score => scoreText.UpdateScore(score));
            uiData.InventoryItems.Subscribe(items => inventoryUI.UpdateItems(items));

            // UI güncellemelerini test et
            uiData.PlayerHealth.Value = 80;
            uiData.PlayerScore.Value = 1500;
            uiData.InventoryItems.Value = new List<string> { "Sword", "Potion", "Key" };

            uiData.PlayerHealth.Value = 60;
            uiData.PlayerScore.Value = 2000;
            uiData.InventoryItems.Value = new List<string> { "Sword", "Potion", "Key", "Shield" };

            uiData.Dispose();
        }

        /// <summary>
        /// Filtre ve throttle örneği
        /// </summary>
        private void FilterAndThrottleExample()
        {
            Debug.Log("--- Filter and Throttle Example ---");

            // Throttle ile ReactiveProperty (100ms)
            var mousePosition = new ReactiveProperty<Vector2>(Vector2.zero, "MousePosition", null, 100);

            // Sadece belirli aralıktaki pozisyonları filtrele
            mousePosition.AddFilter("ScreenBounds", pos => 
                pos.x >= 0 && pos.x <= Screen.width && 
                pos.y >= 0 && pos.y <= Screen.height);

            // Sadece önemli değişiklikleri filtrele (5 piksel üzeri)
            var lastPosition = Vector2.zero;
            mousePosition.AddFilter("SignificantChange", pos =>
            {
                var distance = Vector2.Distance(pos, lastPosition);
                if (distance > 5f)
                {
                    lastPosition = pos;
                    return true;
                }
                return false;
            });

            mousePosition.Subscribe(pos =>
            {
                Debug.Log($"Significant mouse movement: {pos}");
            });

            // Filtre testleri
            mousePosition.Value = new Vector2(10, 10); // Geçer
            mousePosition.Value = new Vector2(12, 12); // Filtreler (küçük değişiklik)
            mousePosition.Value = new Vector2(20, 20); // Geçer
            mousePosition.Value = new Vector2(-10, 10); // Filtreler (ekran dışı)
            mousePosition.Value = new Vector2(100, 100); // Geçer

            mousePosition.Dispose();
        }

        #endregion

        #region Unity Manager Examples

        /// <summary>
        /// UnityObserverManager kullanım örneği
        /// </summary>
        [ContextMenu("Unity Manager Example")]
        public void UnityManagerExample()
        {
            Debug.Log("--- Unity Manager Example ---");

            // Global ReactiveProperty oluştur
            var globalHealth = UnityObserverManager.CreatePropertyStatic("GlobalPlayerHealth", 100);
            var globalScore = UnityObserverManager.CreatePropertyStatic("GlobalPlayerScore", 0);

            // Global Subject oluştur
            var globalEvents = UnityObserverManager.CreateSubjectStatic<string>("GlobalGameEvents");

            // Observer'lar ekle
            globalHealth.Subscribe(health => Debug.Log($"Global health: {health}"));
            globalScore.Subscribe(score => Debug.Log($"Global score: {score}"));
            globalEvents.Subscribe(eventName => Debug.Log($"Global event: {eventName}"));

            // Değerleri güncelle
            globalHealth.Value = 80;
            globalScore.Value = 500;
            globalEvents.SetValue("PlayerLevelUp");

            // Başka yerden aynı observable'lara eriş
            var sameHealth = UnityObserverManager.GetObservableStatic<ReactiveProperty<int>>("GlobalPlayerHealth");
            sameHealth.Value = 60; // Aynı observable'ı günceller

            Debug.Log($"Manager stats - Observables: {UnityObserverManager.Instance.ObservableCount}, Total Observers: {UnityObserverManager.Instance.TotalObserverCount}");
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Oyun durumu enum'u
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// Oyuncu verisi
    /// </summary>
    public class PlayerData : IDisposable
    {
        public ReactiveProperty<int> Health { get; }
        public ReactiveProperty<int> Score { get; }
        public ReactiveProperty<int> Level { get; }

        public PlayerData()
        {
            Health = new ReactiveProperty<int>(100, "PlayerHealth");
            Score = new ReactiveProperty<int>(0, "PlayerScore");
            Level = new ReactiveProperty<int>(1, "PlayerLevel");
        }

        public void Dispose()
        {
            Health?.Dispose();
            Score?.Dispose();
            Level?.Dispose();
        }
    }

    /// <summary>
    /// UI verisi
    /// </summary>
    public class UIData : IDisposable
    {
        public ReactiveProperty<int> PlayerHealth { get; }
        public ReactiveProperty<int> PlayerScore { get; }
        public ReactiveProperty<List<string>> InventoryItems { get; }

        public UIData()
        {
            PlayerHealth = new ReactiveProperty<int>(100, "UIPlayerHealth");
            PlayerScore = new ReactiveProperty<int>(0, "UIPlayerScore");
            InventoryItems = new ReactiveProperty<List<string>>(new List<string>(), "UIInventoryItems");
        }

        public void Dispose()
        {
            PlayerHealth?.Dispose();
            PlayerScore?.Dispose();
            InventoryItems?.Dispose();
        }
    }

    /// <summary>
    /// Sağlık çubuğu UI simülasyonu
    /// </summary>
    public class HealthBarUI
    {
        public void UpdateHealth(int health)
        {
            Debug.Log($"[HealthBar] Health updated to: {health}%");
        }
    }

    /// <summary>
    /// Skor metni UI simülasyonu
    /// </summary>
    public class ScoreTextUI
    {
        public void UpdateScore(int score)
        {
            Debug.Log($"[ScoreText] Score updated to: {score:N0}");
        }
    }

    /// <summary>
    /// Envanter UI simülasyonu
    /// </summary>
    public class InventoryUI
    {
        public void UpdateItems(List<string> items)
        {
            Debug.Log($"[Inventory] Items updated: [{string.Join(", ", items)}]");
        }
    }

    #endregion
}