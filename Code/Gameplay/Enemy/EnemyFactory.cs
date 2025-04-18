using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Utils;
using AuLacThanThu.Core;

namespace AuLacThanThu.Gameplay.Enemy
{
    /// <summary>
    /// Factory class cho việc tạo và quản lý các loại kẻ địch trong game
    /// </summary>
    public class EnemyFactory : MonoBehaviour
    {
        #region Singleton
        private static EnemyFactory _instance;
        public static EnemyFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EnemyFactory>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("EnemyFactory");
                        _instance = obj.AddComponent<EnemyFactory>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        [Header("Enemy Prefabs By Type")]
        [SerializeField] private List<EnemyPrefabData> regularEnemies = new List<EnemyPrefabData>();
        [SerializeField] private List<EnemyPrefabData> eliteEnemies = new List<EnemyPrefabData>();
        [SerializeField] private List<EnemyPrefabData> bossEnemies = new List<EnemyPrefabData>();
        
        [Header("Enemy Prefabs By Element")]
        [SerializeField] private List<EnemyPrefabData> fireEnemies = new List<EnemyPrefabData>();
        [SerializeField] private List<EnemyPrefabData> waterEnemies = new List<EnemyPrefabData>();
        [SerializeField] private List<EnemyPrefabData> earthEnemies = new List<EnemyPrefabData>();
        [SerializeField] private List<EnemyPrefabData> lightningEnemies = new List<EnemyPrefabData>();
        
        [Header("Enemy Options")]
        [SerializeField] private float commonEnemyChance = 0.7f;
        [SerializeField] private float eliteEnemyChance = 0.25f;
        [SerializeField] private float bossEnemyChance = 0.05f;
        
        // Cached data
        private Dictionary<string, GameObject> enemyPrefabDict = new Dictionary<string, GameObject>();
        private Dictionary<EnemyType, List<EnemyPrefabData>> enemyTypeDict = new Dictionary<EnemyType, List<EnemyPrefabData>>();
        private Dictionary<ElementType, List<EnemyPrefabData>> enemyElementDict = new Dictionary<ElementType, List<EnemyPrefabData>>();
        
        // References
        private ObjectPoolManager objectPoolManager;
        private Transform enemyContainer;
        #endregion
        
        #region Unity Lifecycle
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
            
            // Initialize dictionaries
            InitializeDictionaries();
        }
        
        private void Start()
        {
            // Get references
            objectPoolManager = ObjectPoolManager.Instance;
            
            // Create enemy container if it doesn't exist
            if (enemyContainer == null)
            {
                GameObject container = new GameObject("EnemyContainer");
                enemyContainer = container.transform;
            }
            
            // Create object pools for enemies
            CreateEnemyPools();
        }
        #endregion
        
        #region Initialization
        private void InitializeDictionaries()
        {
            // Clear dictionaries
            enemyPrefabDict.Clear();
            enemyTypeDict.Clear();
            enemyElementDict.Clear();
            
            // Initialize type dictionary
            enemyTypeDict[EnemyType.Regular] = regularEnemies;
            enemyTypeDict[EnemyType.Elite] = eliteEnemies;
            enemyTypeDict[EnemyType.Boss] = bossEnemies;
            
            // Initialize element dictionary
            enemyElementDict[ElementType.Fire] = fireEnemies;
            enemyElementDict[ElementType.Water] = waterEnemies;
            enemyElementDict[ElementType.Earth] = earthEnemies;
            enemyElementDict[ElementType.Lightning] = lightningEnemies;
            
            // Add all enemies to prefab dictionary
            AddEnemyListToPrefabDict(regularEnemies);
            AddEnemyListToPrefabDict(eliteEnemies);
            AddEnemyListToPrefabDict(bossEnemies);
        }
        
        private void AddEnemyListToPrefabDict(List<EnemyPrefabData> enemyList)
        {
            foreach (EnemyPrefabData data in enemyList)
            {
                if (data.enemyPrefab != null && !enemyPrefabDict.ContainsKey(data.enemyId))
                {
                    enemyPrefabDict[data.enemyId] = data.enemyPrefab;
                }
            }
        }
        
        private void CreateEnemyPools()
        {
            if (objectPoolManager == null) return;
            
            // Create pools for all enemies
            foreach (var kvp in enemyPrefabDict)
            {
                string poolName = "Enemy_" + kvp.Key;
                GameObject prefab = kvp.Value;
                
                if (!objectPoolManager.HasPool(poolName))
                {
                    objectPoolManager.CreatePool(poolName, prefab, 5, true, enemyContainer);
                }
            }
        }
        #endregion
        
        #region Enemy Creation
        public GameObject CreateEnemy(string enemyId, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (string.IsNullOrEmpty(enemyId))
            {
                Debug.LogError("Enemy ID is null or empty!");
                return null;
            }
            
            // Get prefab
            GameObject enemyPrefab = GetEnemyPrefab(enemyId);
            if (enemyPrefab == null)
            {
                Debug.LogError($"Enemy prefab with ID {enemyId} not found!");
                return null;
            }
            
            // Try to get from object pool
            GameObject enemy = null;
            string poolName = "Enemy_" + enemyId;
            
            if (objectPoolManager != null && objectPoolManager.HasPool(poolName))
            {
                enemy = objectPoolManager.GetPooledObject(poolName);
                
                if (enemy != null)
                {
                    // Set position and rotation
                    enemy.transform.position = position;
                    enemy.transform.rotation = rotation;
                    
                    // Set parent if provided
                    if (parent != null)
                    {
                        enemy.transform.SetParent(parent);
                    }
                    else if (enemyContainer != null)
                    {
                        enemy.transform.SetParent(enemyContainer);
                    }
                }
            }
            
            // If not available in pool, instantiate directly
            if (enemy == null)
            {
                Transform parentTransform = parent != null ? parent : enemyContainer;
                enemy = Instantiate(enemyPrefab, position, rotation, parentTransform);
            }
            
            // Initialize enemy
            EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                // Any additional initialization can be done here
            }
            
            return enemy;
        }
        
        public GameObject CreateRandomEnemy(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // Determine enemy type based on chance
            float typeRoll = Random.value;
            EnemyType selectedType;
            
            if (typeRoll < commonEnemyChance)
            {
                selectedType = EnemyType.Regular;
            }
            else if (typeRoll < commonEnemyChance + eliteEnemyChance)
            {
                selectedType = EnemyType.Elite;
            }
            else
            {
                selectedType = EnemyType.Boss;
            }
            
            // Get random enemy of selected type
            string enemyId = GetRandomEnemyId(selectedType);
            
            if (string.IsNullOrEmpty(enemyId))
            {
                Debug.LogWarning($"No enemy found for type {selectedType}!");
                return null;
            }
            
            return CreateEnemy(enemyId, position, rotation, parent);
        }
        
        public GameObject CreateRandomEnemyByElement(ElementType elementType, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // Get random enemy of selected element
            string enemyId = GetRandomEnemyIdByElement(elementType);
            
            if (string.IsNullOrEmpty(enemyId))
            {
                Debug.LogWarning($"No enemy found for element {elementType}!");
                return null;
            }
            
            return CreateEnemy(enemyId, position, rotation, parent);
        }
        
        public GameObject CreateRandomEnemyByTypeAndElement(EnemyType enemyType, ElementType elementType, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // Get list of enemies matching both type and element
            List<EnemyPrefabData> matchingEnemies = new List<EnemyPrefabData>();
            
            if (enemyTypeDict.ContainsKey(enemyType) && enemyElementDict.ContainsKey(elementType))
            {
                foreach (EnemyPrefabData enemy in enemyTypeDict[enemyType])
                {
                    if (enemy.primaryElement == elementType || enemy.secondaryElement == elementType)
                    {
                        matchingEnemies.Add(enemy);
                    }
                }
            }
            
            if (matchingEnemies.Count == 0)
            {
                Debug.LogWarning($"No enemy found for type {enemyType} and element {elementType}!");
                return null;
            }
            
            // Select random enemy from matching list
            int randomIndex = Random.Range(0, matchingEnemies.Count);
            string enemyId = matchingEnemies[randomIndex].enemyId;
            
            return CreateEnemy(enemyId, position, rotation, parent);
        }
        #endregion
        
        #region Helper Methods
        private GameObject GetEnemyPrefab(string enemyId)
        {
            if (enemyPrefabDict.ContainsKey(enemyId))
            {
                return enemyPrefabDict[enemyId];
            }
            return null;
        }
        
        private string GetRandomEnemyId(EnemyType enemyType)
        {
            if (!enemyTypeDict.ContainsKey(enemyType) || enemyTypeDict[enemyType].Count == 0)
            {
                return null;
            }
            
            List<EnemyPrefabData> enemies = enemyTypeDict[enemyType];
            int randomIndex = Random.Range(0, enemies.Count);
            
            return enemies[randomIndex].enemyId;
        }
        
        private string GetRandomEnemyIdByElement(ElementType elementType)
        {
            if (!enemyElementDict.ContainsKey(elementType) || enemyElementDict[elementType].Count == 0)
            {
                return null;
            }
            
            List<EnemyPrefabData> enemies = enemyElementDict[elementType];
            int randomIndex = Random.Range(0, enemies.Count);
            
            return enemies[randomIndex].enemyId;
        }
        
        public List<string> GetAllEnemyIds()
        {
            return new List<string>(enemyPrefabDict.Keys);
        }
        
        public List<string> GetEnemyIdsByType(EnemyType enemyType)
        {
            List<string> enemyIds = new List<string>();
            
            if (enemyTypeDict.ContainsKey(enemyType))
            {
                foreach (EnemyPrefabData enemy in enemyTypeDict[enemyType])
                {
                    enemyIds.Add(enemy.enemyId);
                }
            }
            
            return enemyIds;
        }
        
        public List<string> GetEnemyIdsByElement(ElementType elementType)
        {
            List<string> enemyIds = new List<string>();
            
            if (enemyElementDict.ContainsKey(elementType))
            {
                foreach (EnemyPrefabData enemy in enemyElementDict[elementType])
                {
                    enemyIds.Add(enemy.enemyId);
                }
            }
            
            return enemyIds;
        }
        #endregion
    }
    
    [System.Serializable]
    public class EnemyPrefabData
    {
        public string enemyId;
        public string enemyName;
        public GameObject enemyPrefab;
        public EnemyType enemyType = EnemyType.Regular;
        public ElementType primaryElement = ElementType.None;
        public ElementType secondaryElement = ElementType.None;
    }
    
    public enum EnemyType
    {
        Regular,
        Elite,
        Boss
    }
}