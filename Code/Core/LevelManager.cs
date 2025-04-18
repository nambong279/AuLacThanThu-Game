using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AuLacThanThu.Core
{
    /// <summary>
    /// Quản lý level, wave, điều kiện thắng/thua của game
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        #region Properties
        [Header("Level Settings")]
        [SerializeField] private int currentChapterId;
        [SerializeField] private int currentLevelId;
        [SerializeField] private int totalWaves = 20;
        [SerializeField] private float timeBetweenWaves = 5f;
        [SerializeField] private float difficultyMultiplier = 1.1f;
        
        [Header("References")]
        [SerializeField] private Transform enemySpawnParent;
        [SerializeField] private GameObject fortressPrefab;
        [SerializeField] private Transform fortressPosition;
        
        // Wave tracking
        private int currentWave = 0;
        private bool isWaveActive = false;
        private List<GameObject> activeEnemies = new List<GameObject>();
        
        // References
        private FortressController fortress;
        #endregion
        
        #region Events
        public delegate void WaveEventHandler(int waveNumber, int totalWaves);
        public event WaveEventHandler OnWaveStarted;
        public event WaveEventHandler OnWaveCompleted;
        
        public delegate void LevelEventHandler();
        public event LevelEventHandler OnLevelCompleted;
        public event LevelEventHandler OnLevelFailed;
        #endregion
        
        #region Initialization
        private void Start()
        {
            // This could also be moved to LoadLevel method
            InitializeLevel();
        }
        
        private void InitializeLevel()
        {
            // Reset wave counter
            currentWave = 0;
            isWaveActive = false;
            
            // Clear any existing enemies
            ClearAllEnemies();
            
            // Create fortress if needed
            if (fortress == null && fortressPrefab != null)
            {
                GameObject fortressObj = Instantiate(fortressPrefab, fortressPosition.position, Quaternion.identity);
                fortress = fortressObj.GetComponent<FortressController>();
                
                // Subscribe to fortress events
                if (fortress != null)
                {
                    fortress.OnFortressDestroyed += OnFortressDestroyed;
                }
            }
            
            // Start first wave after a delay
            StartCoroutine(StartNextWaveAfterDelay(2f));
        }
        #endregion
        
        #region Level Loading
        public void LoadLevel(int chapterId, int levelId)
        {
            currentChapterId = chapterId;
            currentLevelId = levelId;
            
            // Load level data from ScriptableObject or other data source
            // TODO: Implement level data loading from resources
            
            // Initialize the loaded level
            InitializeLevel();
            
            Debug.Log($"Loaded Chapter {chapterId}, Level {levelId}");
        }
        #endregion
        
        #region Wave Management
        private IEnumerator StartNextWaveAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNextWave();
        }
        
        private void StartNextWave()
        {
            if (currentWave >= totalWaves)
            {
                // All waves completed
                OnLevelCompleted?.Invoke();
                return;
            }
            
            currentWave++;
            isWaveActive = true;
            
            // Notify listeners that wave has started
            OnWaveStarted?.Invoke(currentWave, totalWaves);
            
            // Spawn enemies for this wave
            StartCoroutine(SpawnEnemiesForCurrentWave());
            
            Debug.Log($"Wave {currentWave}/{totalWaves} started");
        }
        
        private IEnumerator SpawnEnemiesForCurrentWave()
        {
            // Determine number of enemies based on wave number and difficulty
            int baseEnemyCount = 5;
            int enemyCount = Mathf.RoundToInt(baseEnemyCount * Mathf.Pow(difficultyMultiplier, currentWave - 1));
            
            // TODO: Load enemy prefabs from EnemyFactory or similar
            GameObject dummyEnemyPrefab = Resources.Load<GameObject>("Prefabs/DummyEnemy");
            
            if (dummyEnemyPrefab == null)
            {
                Debug.LogError("Enemy prefab not found!");
                yield break;
            }
            
            // Spawn enemies with delay between each
            for (int i = 0; i < enemyCount; i++)
            {
                // Random position at top of screen
                float randomX = Random.Range(-2.5f, 2.5f);
                Vector3 spawnPos = new Vector3(randomX, 6f, 0f);
                
                GameObject enemy = Instantiate(dummyEnemyPrefab, spawnPos, Quaternion.identity, enemySpawnParent);
                activeEnemies.Add(enemy);
                
                // Subscribe to enemy death event
                EnemyBase enemyComponent = enemy.GetComponent<EnemyBase>();
                if (enemyComponent != null)
                {
                    enemyComponent.OnEnemyDeath += OnEnemyDefeated;
                }
                
                // Wait before spawning next enemy
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        private void OnEnemyDefeated(GameObject enemy)
        {
            if (activeEnemies.Contains(enemy))
            {
                activeEnemies.Remove(enemy);
                
                // Check if all enemies are defeated
                if (isWaveActive && activeEnemies.Count == 0)
                {
                    CompleteCurrentWave();
                }
            }
        }
        
        private void CompleteCurrentWave()
        {
            isWaveActive = false;
            
            // Notify listeners
            OnWaveCompleted?.Invoke(currentWave, totalWaves);
            
            // Check if this was the last wave
            if (currentWave >= totalWaves)
            {
                // Level completed!
                OnLevelCompleted?.Invoke();
                
                // Notify GameManager
                GameManager.Instance.Victory();
            }
            else
            {
                // Start next wave after delay
                StartCoroutine(StartNextWaveAfterDelay(timeBetweenWaves));
            }
            
            Debug.Log($"Wave {currentWave}/{totalWaves} completed");
        }
        #endregion
        
        #region Events & Handlers
        private void OnFortressDestroyed()
        {
            // Game over logic
            OnLevelFailed?.Invoke();
            
            // Notify GameManager
            GameManager.Instance.GameOver();
            
            Debug.Log("Fortress destroyed - Level failed");
        }
        #endregion
        
        #region Helper Methods
        private void ClearAllEnemies()
        {
            // Clean up any remaining enemies
            foreach (GameObject enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            
            activeEnemies.Clear();
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (fortress != null)
            {
                fortress.OnFortressDestroyed -= OnFortressDestroyed;
            }
            
            // Clean up any remaining enemies
            ClearAllEnemies();
        }
        #endregion
    }
}