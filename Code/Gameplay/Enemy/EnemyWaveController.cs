using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Core;
using AuLacThanThu.Utils;

namespace AuLacThanThu.Gameplay.Enemy
{
    /// <summary>
    /// Điều khiển các wave của kẻ địch trong game
    /// </summary>
    public class EnemyWaveController : MonoBehaviour
    {
        #region Properties
        [Header("Wave Settings")]
        [SerializeField] private int currentChapter = 1;
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int totalWaves = 20;
        [SerializeField] private int currentWave = 0;
        [SerializeField] private float timeBetweenWaves = 5f;
        [SerializeField] private float timeBetweenEnemies = 0.5f;
        [SerializeField] private bool autoStart = false;
        
        [Header("Difficulty Settings")]
        [SerializeField] private float difficultyMultiplier = 1.1f;
        [SerializeField] private int baseEnemiesPerWave = 5;
        [SerializeField] private float eliteChanceIncrease = 0.01f;
        [SerializeField] private float bossChanceIncrease = 0.002f;
        [SerializeField] private bool spawnBossAtFinalWave = true;
        
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnRadius = 2f;
        [SerializeField] private float yOffset = 6f;      // How far above screen to spawn
        [SerializeField] private Transform enemyParent;
        
        [Header("Wave Data")]
        [SerializeField] private List<WaveData> predefinedWaves;
        [SerializeField] private List<string> bossIdsForLevel = new List<string>();
        
        // Runtime state
        private bool isWaveActive = false;
        private bool isSpawning = false;
        private float waveTimer = 0f;
        private List<GameObject> activeEnemies = new List<GameObject>();
        
        // References
        private EnemyFactory enemyFactory;
        private GameManager gameManager;
        private LevelManager levelManager;
        private Camera mainCamera;
        #endregion
        
        #region Events
        public delegate void WaveEventHandler(int waveNumber, int totalWaves);
        public event WaveEventHandler OnWaveStarted;
        public event WaveEventHandler OnWaveCompleted;
        
        public delegate void LevelEventHandler();
        public event LevelEventHandler OnAllWavesCompleted;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Get references
            enemyFactory = EnemyFactory.Instance;
            gameManager = GameManager.Instance;
            levelManager = FindObjectOfType<LevelManager>();
        }
        
        private void Start()
        {
            // Get main camera
            mainCamera = Camera.main;
            
            // Create enemy parent if needed
            if (enemyParent == null)
            {
                GameObject parent = new GameObject("Enemies");
                enemyParent = parent.transform;
            }
            
            // Generate spawn points if none are set
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                GenerateSpawnPoints();
            }
            
            // Auto start first wave if enabled
            if (autoStart)
            {
                StartNextWave();
            }
        }
        
        private void Update()
        {
            // Check if game is paused
            if (gameManager != null && gameManager.CurrentState == GameManager.GameState.Paused)
                return;
                
            // Handle wave timer
            if (!isWaveActive && currentWave < totalWaves)
            {
                waveTimer -= Time.deltaTime;
                
                if (waveTimer <= 0f)
                {
                    StartNextWave();
                }
            }
            
            // Check if current wave is complete
            if (isWaveActive && !isSpawning && activeEnemies.Count == 0)
            {
                CompleteCurrentWave();
            }
            
            // Clean up null references in active enemies list
            activeEnemies.RemoveAll(enemy => enemy == null);
        }
        #endregion
        
        #region Initialization
        private void GenerateSpawnPoints()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                
                if (mainCamera == null)
                {
                    Debug.LogError("No main camera found for generating spawn points!");
                    return;
                }
            }
            
            // Calculate screen bounds
            float height = 2f * mainCamera.orthographicSize;
            float width = height * mainCamera.aspect;
            
            // Create 5 spawn points spread across top of screen
            spawnPoints = new Transform[5];
            
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                GameObject spawnPoint = new GameObject($"SpawnPoint_{i}");
                
                // Position evenly across screen width
                float xPos = (width * (i / (float)(spawnPoints.Length - 1))) - (width / 2f);
                float yPos = mainCamera.orthographicSize + yOffset;
                
                spawnPoint.transform.position = new Vector3(xPos, yPos, 0f);
                spawnPoint.transform.parent = transform;
                
                spawnPoints[i] = spawnPoint.transform;
            }
        }
        
        public void Initialize(int chapter, int level, int waves)
        {
            currentChapter = chapter;
            currentLevel = level;
            totalWaves = waves;
            currentWave = 0;
            isWaveActive = false;
            isSpawning = false;
            
            // Clear any existing enemies
            ClearAllEnemies();
            
            // Reset wave timer
            waveTimer = timeBetweenWaves;
            
            // Load predefined waves if available
            LoadWaveData();
            
            Debug.Log($"Wave controller initialized for Chapter {chapter}, Level {level} with {waves} waves");
        }
        
        private void LoadWaveData()
        {
            // TODO: Load wave data from LevelManager or ScriptableObject
            // For now, use empty predefined waves list
            if (predefinedWaves == null)
            {
                predefinedWaves = new List<WaveData>();
            }
        }
        #endregion
        
        #region Wave Management
        public void StartNextWave()
        {
            if (currentWave >= totalWaves)
            {
                Debug.LogWarning("All waves already completed!");
                return;
            }
            
            currentWave++;
            isWaveActive = true;
            
            // Notify listeners
            OnWaveStarted?.Invoke(currentWave, totalWaves);
            
            // Trigger event
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.TriggerEvent(GameEventType.WaveStarted, 
                    new WaveEventArgs(currentWave, totalWaves));
            }
            
            // Spawn enemies
            StartCoroutine(SpawnWaveEnemies());
            
            Debug.Log($"Wave {currentWave}/{totalWaves} started");
        }
        
        private IEnumerator SpawnWaveEnemies()
        {
            isSpawning = true;
            
            // Check if we have a predefined wave
            if (predefinedWaves.Count >= currentWave)
            {
                // Spawn predefined wave
                yield return SpawnPredefinedWave(predefinedWaves[currentWave - 1]);
            }
            else
            {
                // Generate and spawn dynamic wave
                yield return SpawnDynamicWave();
            }
            
            isSpawning = false;
        }
        
        private IEnumerator SpawnPredefinedWave(WaveData waveData)
        {
            foreach (EnemySpawnData spawnData in waveData.enemies)
            {
                // Determine spawn position
                Vector3 spawnPosition = GetSpawnPosition(spawnData.spawnPointIndex);
                
                // Create enemy
                GameObject enemy = enemyFactory.CreateEnemy(
                    spawnData.enemyId, 
                    spawnPosition, 
                    Quaternion.identity, 
                    enemyParent);
                
                if (enemy != null)
                {
                    // Add to active enemies
                    activeEnemies.Add(enemy);
                    
                    // Subscribe to death event
                    EnemyBase enemyComponent = enemy.GetComponent<EnemyBase>();
                    if (enemyComponent != null)
                    {
                        enemyComponent.OnEnemyDeath += OnEnemyDefeated;
                    }
                }
                
                yield return new WaitForSeconds(spawnData.delayAfterSpawn);
            }
        }
        
        private IEnumerator SpawnDynamicWave()
        {
            // Calculate enemies count based on wave number
            int enemyCount = CalculateEnemyCount();
            
            // Check if this is the final wave
            bool isFinalWave = currentWave == totalWaves;
            
            // Calculate elite and boss chances
            float eliteChance = CalculateEliteChance();
            float bossChance = CalculateBossChance();
            
            // Spawn normal enemies
            for (int i = 0; i < enemyCount; i++)
            {
                // Special handling for final enemy in final wave
                if (isFinalWave && spawnBossAtFinalWave && i == enemyCount - 1)
                {
                    SpawnBossEnemy();
                }
                else
                {
                    // Determine enemy type
                    EnemyType enemyType = DetermineEnemyType(eliteChance, bossChance);
                    
                    // Determine element type - more varied as waves progress
                    ElementType elementType = DetermineElementType();
                    
                    // Get spawn position
                    Vector3 spawnPosition = GetRandomSpawnPosition();
                    
                    // Create enemy
                    GameObject enemy = enemyFactory.CreateRandomEnemyByTypeAndElement(
                        enemyType, 
                        elementType, 
                        spawnPosition, 
                        Quaternion.identity, 
                        enemyParent);
                    
                    if (enemy != null)
                    {
                        // Add to active enemies
                        activeEnemies.Add(enemy);
                        
                        // Subscribe to death event
                        EnemyBase enemyComponent = enemy.GetComponent<EnemyBase>();
                        if (enemyComponent != null)
                        {
                            enemyComponent.OnEnemyDeath += OnEnemyDefeated;
                        }
                    }
                }
                
                // Wait before spawning next enemy
                yield return new WaitForSeconds(timeBetweenEnemies);
            }
        }
        
        private void SpawnBossEnemy()
        {
            // Determine boss to spawn
            string bossId = "";
            
            if (bossIdsForLevel.Count > 0)
            {
                // Use predefined boss for this level
                bossId = bossIdsForLevel[Random.Range(0, bossIdsForLevel.Count)];
            }
            else
            {
                // Get random boss
                List<string> bossIds = enemyFactory.GetEnemyIdsByType(EnemyType.Boss);
                
                if (bossIds.Count > 0)
                {
                    bossId = bossIds[Random.Range(0, bossIds.Count)];
                }
            }
            
            if (string.IsNullOrEmpty(bossId))
            {
                Debug.LogWarning("No boss ID found, spawning random enemy instead");
                enemyFactory.CreateRandomEnemy(GetRandomSpawnPosition(), Quaternion.identity, enemyParent);
                return;
            }
            
            // Spawn boss in middle of screen
            Vector3 spawnPosition = new Vector3(0f, mainCamera.orthographicSize + yOffset, 0f);
            
            GameObject boss = enemyFactory.CreateEnemy(
                bossId, 
                spawnPosition, 
                Quaternion.identity, 
                enemyParent);
            
            if (boss != null)
            {
                // Add to active enemies
                activeEnemies.Add(boss);
                
                // Subscribe to death event
                EnemyBase bossComponent = boss.GetComponent<EnemyBase>();
                if (bossComponent != null)
                {
                    bossComponent.OnEnemyDeath += OnEnemyDefeated;
                }
                
                Debug.Log($"Boss {bossId} spawned in final wave");
            }
        }
        
        private void CompleteCurrentWave()
        {
            isWaveActive = false;
            
            // Notify listeners
            OnWaveCompleted?.Invoke(currentWave, totalWaves);
            
            // Trigger event
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.TriggerEvent(GameEventType.WaveCompleted, 
                    new WaveEventArgs(currentWave, totalWaves));
            }
            
            // Check if all waves completed
            if (currentWave >= totalWaves)
            {
                OnAllWavesCompleted?.Invoke();
                
                // Notify level manager
                if (levelManager != null)
                {
                    levelManager.OnLevelCompleted?.Invoke();
                }
                
                Debug.Log("All waves completed!");
            }
            else
            {
                // Start timer for next wave
                waveTimer = timeBetweenWaves;
                
                Debug.Log($"Wave {currentWave}/{totalWaves} completed, next wave in {timeBetweenWaves} seconds");
            }
        }
        
        private void OnEnemyDefeated(GameObject enemy)
        {
            if (activeEnemies.Contains(enemy))
            {
                activeEnemies.Remove(enemy);
            }
        }
        
        public void ClearAllEnemies()
        {
            // Make a copy to avoid modification during iteration
            List<GameObject> enemyList = new List<GameObject>(activeEnemies);
            
            foreach (GameObject enemy in enemyList)
            {
                if (enemy != null)
                {
                    // Unsubscribe from events
                    EnemyBase enemyComponent = enemy.GetComponent<EnemyBase>();
                    if (enemyComponent != null)
                    {
                        enemyComponent.OnEnemyDeath -= OnEnemyDefeated;
                    }
                    
                    // Return to pool or destroy
                    if (enemy.GetComponent<PooledObject>() != null)
                    {
                        ObjectPoolManager.Instance?.ReturnToPool(enemy);
                    }
                    else
                    {
                        Destroy(enemy);
                    }
                }
            }
            
            activeEnemies.Clear();
        }
        #endregion
        
        #region Helper Methods
        private int CalculateEnemyCount()
        {
            // Base formula: BaseEnemies * (DifficultyMultiplier ^ (WaveNumber - 1))
            return Mathf.RoundToInt(baseEnemiesPerWave * Mathf.Pow(difficultyMultiplier, currentWave - 1));
        }
        
        private float CalculateEliteChance()
        {
            // Elite chance increases with wave number
            return Mathf.Min(0.4f, 0.1f + (currentWave - 1) * eliteChanceIncrease);
        }
        
        private float CalculateBossChance()
        {
            // Boss chance increases with wave number, but stays low
            return Mathf.Min(0.1f, 0.01f + (currentWave - 1) * bossChanceIncrease);
        }
        
        private EnemyType DetermineEnemyType(float eliteChance, float bossChance)
        {
            float roll = Random.value;
            
            if (roll < bossChance)
            {
                return EnemyType.Boss;
            }
            else if (roll < bossChance + eliteChance)
            {
                return EnemyType.Elite;
            }
            else
            {
                return EnemyType.Regular;
            }
        }
        
        private ElementType DetermineElementType()
        {
            // Logic for determining element type
            // Could be based on level, chapter, or wave number
            
            // For now, just return random element
            ElementType[] elements = { 
                ElementType.Fire, 
                ElementType.Water, 
                ElementType.Earth, 
                ElementType.Lightning 
            };
            
            return elements[Random.Range(0, elements.Length)];
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                // Default spawn at top middle of screen
                return new Vector3(0f, mainCamera.orthographicSize + yOffset, 0f);
            }
            
            // Get random spawn point
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            
            // Add random offset within radius
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 position = spawnPoint.position + new Vector3(offset.x, offset.y, 0f);
            
            return position;
        }
        
        private Vector3 GetSpawnPosition(int spawnPointIndex)
        {
            if (spawnPoints == null || spawnPoints.Length == 0 || spawnPointIndex < 0)
            {
                return GetRandomSpawnPosition();
            }
            
            // Ensure index is within bounds
            spawnPointIndex = Mathf.Clamp(spawnPointIndex, 0, spawnPoints.Length - 1);
            
            // Get spawn point
            Transform spawnPoint = spawnPoints[spawnPointIndex];
            
            // Add small random offset
            Vector2 offset = Random.insideUnitCircle * (spawnRadius * 0.5f);
            Vector3 position = spawnPoint.position + new Vector3(offset.x, offset.y, 0f);
            
            return position;
        }
        #endregion
        
        #region Public Methods
        public bool IsWaveActive()
        {
            return isWaveActive;
        }
        
        public int GetCurrentWave()
        {
            return currentWave;
        }
        
        public int GetTotalWaves()
        {
            return totalWaves;
        }
        
        public float GetWaveTimer()
        {
            return waveTimer;
        }
        
        public int GetActiveEnemyCount()
        {
            return activeEnemies.Count;
        }
        
        // For testing and debugging
        public void StartWave()
        {
            if (!isWaveActive)
            {
                StartNextWave();
            }
        }
        
        public void SkipToNextWave()
        {
            if (isWaveActive)
            {
                ClearAllEnemies();
                CompleteCurrentWave();
            }
            else
            {
                waveTimer = 0.1f;
            }
        }
        
        public void SkipToFinalWave()
        {
            if (currentWave < totalWaves - 1)
            {
                ClearAllEnemies();
                currentWave = totalWaves - 1;
                waveTimer = 0.1f;
                isWaveActive = false;
            }
        }
        #endregion
    }
    
    #region Data Classes
    [System.Serializable]
    public class WaveData
    {
        public int waveIndex;
        public List<EnemySpawnData> enemies = new List<EnemySpawnData>();
    }
    
    [System.Serializable]
    public class EnemySpawnData
    {
        public string enemyId;
        public int spawnPointIndex = -1;   // -1 for random
        public float delayAfterSpawn = 0.5f;
    }
    #endregion
}