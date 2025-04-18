using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AuLacThanThu.Core
{
    /// <summary>
    /// Hệ thống quản lý sự kiện toàn cục, sử dụng mô hình publisher-subscriber
    /// </summary>
    public class EventSystem : MonoBehaviour
    {
        #region Singleton
        private static EventSystem _instance;
        public static EventSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EventSystem>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("EventSystem");
                        _instance = obj.AddComponent<EventSystem>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        // Dictionary mapping event types to listeners
        private Dictionary<GameEventType, List<Action<EventArgs>>> eventListeners = new Dictionary<GameEventType, List<Action<EventArgs>>>();
        
        private void Awake()
        {
            // Ensure singleton
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize event system
            InitEventSystem();
        }
        
        private void InitEventSystem()
        {
            // Initialize event dictionary with empty lists for all event types
            foreach (GameEventType eventType in Enum.GetValues(typeof(GameEventType)))
            {
                eventListeners[eventType] = new List<Action<EventArgs>>();
            }
        }
        
        #region Subscription Methods
        /// <summary>
        /// Subscribe to an event type
        /// </summary>
        /// <param name="eventType">Type of event to subscribe to</param>
        /// <param name="listener">Callback function to be called when event is triggered</param>
        public void Subscribe(GameEventType eventType, Action<EventArgs> listener)
        {
            if (!eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType] = new List<Action<EventArgs>>();
            }
            
            eventListeners[eventType].Add(listener);
        }
        
        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        /// <param name="eventType">Type of event to unsubscribe from</param>
        /// <param name="listener">Callback function to remove</param>
        public void Unsubscribe(GameEventType eventType, Action<EventArgs> listener)
        {
            if (eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType].Remove(listener);
            }
        }
        
        /// <summary>
        /// Subscribe to an event type with generic event args
        /// </summary>
        /// <typeparam name="T">Type of event args</typeparam>
        /// <param name="eventType">Type of event to subscribe to</param>
        /// <param name="listener">Callback function to be called when event is triggered</param>
        public void Subscribe<T>(GameEventType eventType, Action<T> listener) where T : EventArgs
        {
            Subscribe(eventType, (args) =>
            {
                if (args is T typedArgs)
                {
                    listener(typedArgs);
                }
            });
        }
        
        /// <summary>
        /// Unsubscribe from all events
        /// </summary>
        public void UnsubscribeAll()
        {
            foreach (var eventType in eventListeners.Keys)
            {
                eventListeners[eventType].Clear();
            }
        }
        #endregion
        
        #region Trigger Methods
        /// <summary>
        /// Trigger an event
        /// </summary>
        /// <param name="eventType">Type of event to trigger</param>
        /// <param name="args">Optional event arguments</param>
        public void TriggerEvent(GameEventType eventType, EventArgs args = null)
        {
            if (eventListeners.ContainsKey(eventType))
            {
                // Create a copy of the list to avoid modification during iteration
                var listeners = new List<Action<EventArgs>>(eventListeners[eventType]);
                
                foreach (var listener in listeners)
                {
                    try
                    {
                        listener?.Invoke(args ?? EventArgs.Empty);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error in event listener for {eventType}: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Helper method to trigger an event with a typed event args
        /// </summary>
        /// <typeparam name="T">Type of event args</typeparam>
        /// <param name="eventType">Type of event to trigger</param>
        /// <param name="args">Event arguments</param>
        public void TriggerEvent<T>(GameEventType eventType, T args) where T : EventArgs
        {
            TriggerEvent(eventType, args as EventArgs);
        }
        #endregion
    }
    
    #region Event Types and Args
    /// <summary>
    /// Types of game events
    /// </summary>
    public enum GameEventType
    {
        // Game state events
        GameStateChanged,
        LevelStarted,
        LevelCompleted,
        LevelFailed,
        WaveStarted,
        WaveCompleted,
        GamePaused,
        GameResumed,
        
        // Player events
        PlayerLevelUp,
        ExperienceGained,
        
        // Combat events
        EnemySpawned,
        EnemyDied,
        EnemyReachedFortress,
        FortressDamaged,
        FortressDestroyed,
        ProjectileFired,
        
        // Hero events
        HeroUnlocked,
        HeroLevelUp,
        HeroStarUp,
        HeroSkillActivated,
        HeroEquipmentChanged,
        
        // Resource events
        ResourceChanged,
        ItemObtained,
        ItemUsed,
        
        // Fortress events
        FortressUpgraded,
        FortressSkillUpgraded,
        
        // UI events
        ScreenOpened,
        ScreenClosed,
        ButtonClicked,
        
        // Gacha events
        GachaOpened,
        GachaResultObtained,
        
        // Achievement events
        AchievementUnlocked,
        QuestCompleted,
        
        // Misc
        DataSaved,
        DataLoaded,
        ErrorOccurred
    }
    
    /// <summary>
    /// Game state changed event arguments
    /// </summary>
    public class GameStateChangedEventArgs : EventArgs
    {
        public GameManager.GameState PreviousState { get; private set; }
        public GameManager.GameState NewState { get; private set; }
        
        public GameStateChangedEventArgs(GameManager.GameState previousState, GameManager.GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }
    
    /// <summary>
    /// Level event arguments
    /// </summary>
    public class LevelEventArgs : EventArgs
    {
        public int ChapterId { get; private set; }
        public int LevelId { get; private set; }
        public int StarsEarned { get; private set; }
        
        public LevelEventArgs(int chapterId, int levelId, int starsEarned = 0)
        {
            ChapterId = chapterId;
            LevelId = levelId;
            StarsEarned = starsEarned;
        }
    }
    
    /// <summary>
    /// Wave event arguments
    /// </summary>
    public class WaveEventArgs : EventArgs
    {
        public int WaveNumber { get; private set; }
        public int TotalWaves { get; private set; }
        
        public WaveEventArgs(int waveNumber, int totalWaves)
        {
            WaveNumber = waveNumber;
            TotalWaves = totalWaves;
        }
    }
    
    /// <summary>
    /// Resource changed event arguments
    /// </summary>
    public class ResourceChangedEventArgs : EventArgs
    {
        public ResourceType ResourceType { get; private set; }
        public int Amount { get; private set; }
        public int Delta { get; private set; }
        
        public ResourceChangedEventArgs(ResourceType resourceType, int amount, int delta)
        {
            ResourceType = resourceType;
            Amount = amount;
            Delta = delta;
        }
    }
    
    /// <summary>
    /// Enemy event arguments
    /// </summary>
    public class EnemyEventArgs : EventArgs
    {
        public GameObject Enemy { get; private set; }
        public string EnemyId { get; private set; }
        public float Damage { get; private set; }
        
        public EnemyEventArgs(GameObject enemy, string enemyId, float damage = 0)
        {
            Enemy = enemy;
            EnemyId = enemyId;
            Damage = damage;
        }
    }
    
    /// <summary>
    /// Hero event arguments
    /// </summary>
    public class HeroEventArgs : EventArgs
    {
        public string HeroId { get; private set; }
        public int Level { get; private set; }
        public int Stars { get; private set; }
        
        public HeroEventArgs(string heroId, int level = 1, int stars = 1)
        {
            HeroId = heroId;
            Level = level;
            Stars = stars;
        }
    }
    
    /// <summary>
    /// Fortress event arguments
    /// </summary>
    public class FortressEventArgs : EventArgs
    {
        public FortressComponent Component { get; private set; }
        public int Level { get; private set; }
        
        public FortressEventArgs(FortressComponent component, int level)
        {
            Component = component;
            Level = level;
        }
    }
    
    /// <summary>
    /// UI event arguments
    /// </summary>
    public class UIEventArgs : EventArgs
    {
        public string ScreenName { get; private set; }
        public string ElementName { get; private set; }
        
        public UIEventArgs(string screenName, string elementName = "")
        {
            ScreenName = screenName;
            ElementName = elementName;
        }
    }
    
    /// <summary>
    /// Item event arguments
    /// </summary>
    public class ItemEventArgs : EventArgs
    {
        public string ItemId { get; private set; }
        public int Amount { get; private set; }
        
        public ItemEventArgs(string itemId, int amount = 1)
        {
            ItemId = itemId;
            Amount = amount;
        }
    }
    
    /// <summary>
    /// Error event arguments
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; private set; }
        public System.Exception Exception { get; private set; }
        
        public ErrorEventArgs(string errorMessage, System.Exception exception = null)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }
    #endregion
}