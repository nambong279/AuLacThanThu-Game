using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AuLacThanThu.Core
{
    /// <summary>
    /// Quản lý Object Pool để tái sử dụng game objects, giảm thiểu việc Instantiate và Destroy liên tục
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        #region Singleton
        private static ObjectPoolManager _instance;
        public static ObjectPoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ObjectPoolManager>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("ObjectPoolManager");
                        _instance = obj.AddComponent<ObjectPoolManager>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        [System.Serializable]
        public class Pool
        {
            public string poolName;
            public GameObject prefab;
            public int initialSize = 10;
            public bool expandable = true;
            public Transform customParent;
            [HideInInspector] public List<GameObject> pooledObjects = new List<GameObject>();
        }
        
        [Header("Pool Definitions")]
        [SerializeField] private List<Pool> pools = new List<Pool>();
        [SerializeField] private Transform poolParent;
        
        // Dictionary for quick lookup of pools by name
        private Dictionary<string, Pool> poolDictionary = new Dictionary<string, Pool>();
        
        // Dictionary for quick lookup of prefab to pool name
        private Dictionary<GameObject, string> prefabToPoolName = new Dictionary<GameObject, string>();
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
            
            // Create parent for pooled objects if not set
            if (poolParent == null)
            {
                GameObject parent = new GameObject("PooledObjects");
                parent.transform.SetParent(transform);
                poolParent = parent.transform;
            }
            
            // Initialize pools
            InitializePools();
        }
        
        private void InitializePools()
        {
            // Clear dictionaries
            poolDictionary.Clear();
            prefabToPoolName.Clear();
            
            // Initialize each pool
            foreach (Pool pool in pools)
            {
                // Create a parent for this pool
                Transform poolGroupParent = pool.customParent;
                if (poolGroupParent == null)
                {
                    GameObject poolGroup = new GameObject(pool.poolName + "Pool");
                    poolGroup.transform.SetParent(poolParent);
                    poolGroupParent = poolGroup.transform;
                }
                
                // Create initial objects
                for (int i = 0; i < pool.initialSize; i++)
                {
                    GameObject obj = CreateNewPooledObject(pool, poolGroupParent);
                    obj.SetActive(false);
                    pool.pooledObjects.Add(obj);
                }
                
                // Add to dictionaries
                poolDictionary[pool.poolName] = pool;
                if (pool.prefab != null)
                {
                    prefabToPoolName[pool.prefab] = pool.poolName;
                }
            }
        }
        #endregion
        
        #region Object Pooling Methods
        public GameObject GetPooledObject(string poolName)
        {
            // Check if pool exists
            if (!poolDictionary.ContainsKey(poolName))
            {
                Debug.LogWarning($"Pool with name {poolName} doesn't exist!");
                return null;
            }
            
            Pool pool = poolDictionary[poolName];
            
            // Find an inactive object in the pool
            foreach (GameObject obj in pool.pooledObjects)
            {
                if (obj != null && !obj.activeInHierarchy)
                {
                    obj.SetActive(true);
                    return obj;
                }
            }
            
            // If no inactive object found and pool is expandable, create a new one
            if (pool.expandable)
            {
                Transform poolGroupParent = pool.customParent;
                if (poolGroupParent == null)
                {
                    poolGroupParent = poolParent.Find(pool.poolName + "Pool");
                }
                
                GameObject newObj = CreateNewPooledObject(pool, poolGroupParent);
                newObj.SetActive(true);
                pool.pooledObjects.Add(newObj);
                return newObj;
            }
            
            // If no object available and pool is not expandable, return null
            Debug.LogWarning($"No object available in pool {poolName} and pool is not expandable!");
            return null;
        }
        
        public GameObject GetPooledObject(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Trying to get pooled object with null prefab!");
                return null;
            }
            
            // Check if prefab has a registered pool
            if (prefabToPoolName.ContainsKey(prefab))
            {
                return GetPooledObject(prefabToPoolName[prefab]);
            }
            
            // If not, create a new dynamic pool for this prefab
            string poolName = "DynamicPool_" + prefab.name;
            CreatePool(poolName, prefab, 5, true);
            return GetPooledObject(poolName);
        }
        
        public void ReturnToPool(GameObject obj)
        {
            if (obj == null) return;
            
            // Deactivate the object
            obj.SetActive(false);
        }
        
        public bool CreatePool(string poolName, GameObject prefab, int initialSize, bool expandable = true, Transform customParent = null)
        {
            // Check if pool already exists
            if (poolDictionary.ContainsKey(poolName))
            {
                Debug.LogWarning($"Pool with name {poolName} already exists!");
                return false;
            }
            
            // Create new pool
            Pool newPool = new Pool
            {
                poolName = poolName,
                prefab = prefab,
                initialSize = initialSize,
                expandable = expandable,
                customParent = customParent,
                pooledObjects = new List<GameObject>()
            };
            
            // Create parent for this pool
            Transform poolGroupParent = customParent;
            if (poolGroupParent == null)
            {
                GameObject poolGroup = new GameObject(poolName + "Pool");
                poolGroup.transform.SetParent(poolParent);
                poolGroupParent = poolGroup.transform;
            }
            
            // Create initial objects
            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = CreateNewPooledObject(newPool, poolGroupParent);
                obj.SetActive(false);
                newPool.pooledObjects.Add(obj);
            }
            
            // Add to dictionaries
            poolDictionary[poolName] = newPool;
            if (prefab != null)
            {
                prefabToPoolName[prefab] = poolName;
            }
            
            // Add to pools list
            pools.Add(newPool);
            
            return true;
        }
        
        private GameObject CreateNewPooledObject(Pool pool, Transform parent)
        {
            GameObject obj = Instantiate(pool.prefab, parent);
            
            // Add PooledObject component if not already exists
            PooledObject pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                pooledObj = obj.AddComponent<PooledObject>();
            }
            
            // Set pool name
            pooledObj.PoolName = pool.poolName;
            
            return obj;
        }
        
        public void ClearPool(string poolName)
        {
            if (!poolDictionary.ContainsKey(poolName))
            {
                Debug.LogWarning($"Pool with name {poolName} doesn't exist!");
                return;
            }
            
            Pool pool = poolDictionary[poolName];
            
            // Destroy all objects in the pool
            foreach (GameObject obj in pool.pooledObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            
            // Clear the list
            pool.pooledObjects.Clear();
        }
        
        public void ClearAllPools()
        {
            foreach (var pool in pools)
            {
                ClearPool(pool.poolName);
            }
        }
        #endregion
        
        #region Utility Methods
        public void PrewarmPool(string poolName, int count)
        {
            if (!poolDictionary.ContainsKey(poolName))
            {
                Debug.LogWarning($"Pool with name {poolName} doesn't exist!");
                return;
            }
            
            Pool pool = poolDictionary[poolName];
            
            // Calculate how many more objects we need
            int currentSize = pool.pooledObjects.Count;
            int neededSize = count - currentSize;
            
            if (neededSize <= 0) return;
            
            // Find parent
            Transform poolGroupParent = pool.customParent;
            if (poolGroupParent == null)
            {
                poolGroupParent = poolParent.Find(pool.poolName + "Pool");
                
                if (poolGroupParent == null)
                {
                    GameObject poolGroup = new GameObject(pool.poolName + "Pool");
                    poolGroup.transform.SetParent(poolParent);
                    poolGroupParent = poolGroup.transform;
                }
            }
            
            // Create new objects
            for (int i = 0; i < neededSize; i++)
            {
                GameObject obj = CreateNewPooledObject(pool, poolGroupParent);
                obj.SetActive(false);
                pool.pooledObjects.Add(obj);
            }
        }
        
        public bool HasPool(string poolName)
        {
            return poolDictionary.ContainsKey(poolName);
        }
        
        public bool HasPrefabPool(GameObject prefab)
        {
            return prefabToPoolName.ContainsKey(prefab);
        }
        
        public int GetPoolSize(string poolName)
        {
            if (!poolDictionary.ContainsKey(poolName))
            {
                Debug.LogWarning($"Pool with name {poolName} doesn't exist!");
                return -1;
            }
            
            return poolDictionary[poolName].pooledObjects.Count;
        }
        
        public int GetActiveCount(string poolName)
        {
            if (!poolDictionary.ContainsKey(poolName))
            {
                Debug.LogWarning($"Pool with name {poolName} doesn't exist!");
                return -1;
            }
            
            int count = 0;
            foreach (GameObject obj in poolDictionary[poolName].pooledObjects)
            {
                if (obj != null && obj.activeInHierarchy)
                {
                    count++;
                }
            }
            
            return count;
        }
        #endregion
    }
    
    /// <summary>
    /// Component attached to pooled objects to track which pool they belong to
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public string PoolName { get; set; }
        
        private void OnDisable()
        {
            // Reset any necessary properties when object is returned to pool
            // For example, reset velocity for rigidbodies
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            Rigidbody2D rb2D = GetComponent<Rigidbody2D>();
            if (rb2D != null)
            {
                rb2D.velocity = Vector2.zero;
                rb2D.angularVelocity = 0f;
            }
        }
    }
}