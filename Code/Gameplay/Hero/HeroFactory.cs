using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Utils;
using AuLacThanThu.Core;

namespace AuLacThanThu.Gameplay.Hero
{
    /// <summary>
    /// Factory class cho việc tạo và khởi tạo các instance của anh hùng
    /// </summary>
    public class HeroFactory : MonoBehaviour
    {
        #region Singleton
        private static HeroFactory _instance;
        public static HeroFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<HeroFactory>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("HeroFactory");
                        _instance = obj.AddComponent<HeroFactory>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        [Header("Hero Prefab Database")]
        [SerializeField] private List<HeroPrefabData> heroPrefabs = new List<HeroPrefabData>();
        
        [Header("Default Hero")]
        [SerializeField] private GameObject defaultHeroPrefab;
        
        // Cached dictionaries for quick lookup
        private Dictionary<string, GameObject> heroPrefabDict = new Dictionary<string, GameObject>();
        private Dictionary<HeroRarity, List<HeroPrefabData>> heroRarityDict = new Dictionary<HeroRarity, List<HeroPrefabData>>();
        private Dictionary<ElementType, List<HeroPrefabData>> heroElementDict = new Dictionary<ElementType, List<HeroPrefabData>>();
        
        // References
        private ObjectPoolManager objectPoolManager;
        private Transform heroContainer;
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
            
            // Create hero container if doesn't exist
            if (heroContainer == null)
            {
                GameObject container = new GameObject("HeroContainer");
                heroContainer = container.transform;
            }
            
            // Create object pools for heroes
            CreateHeroPools();
        }
        #endregion
        
        #region Initialization
        private void InitializeDictionaries()
        {
            // Clear dictionaries
            heroPrefabDict.Clear();
            heroRarityDict.Clear();
            heroElementDict.Clear();
            
            // Initialize rarity dictionary
            heroRarityDict[HeroRarity.Common] = new List<HeroPrefabData>();
            heroRarityDict[HeroRarity.Rare] = new List<HeroPrefabData>();
            heroRarityDict[HeroRarity.Epic] = new List<HeroPrefabData>();
            heroRarityDict[HeroRarity.Legendary] = new List<HeroPrefabData>();
            heroRarityDict[HeroRarity.Immortal] = new List<HeroPrefabData>();
            
            // Initialize element dictionary
            heroElementDict[ElementType.Fire] = new List<HeroPrefabData>();
            heroElementDict[ElementType.Water] = new List<HeroPrefabData>();
            heroElementDict[ElementType.Earth] = new List<HeroPrefabData>();
            heroElementDict[ElementType.Lightning] = new List<HeroPrefabData>();
            heroElementDict[ElementType.None] = new List<HeroPrefabData>();
            
            // Populate dictionaries
            foreach (HeroPrefabData data in heroPrefabs)
            {
                if (data.heroPrefab != null)
                {
                    // Add to prefab dictionary
                    heroPrefabDict[data.heroId] = data.heroPrefab;
                    
                    // Add to rarity dictionary
                    if (heroRarityDict.ContainsKey(data.rarity))
                    {
                        heroRarityDict[data.rarity].Add(data);
                    }
                    
                    // Add to element dictionary
                    if (heroElementDict.ContainsKey(data.elementType))
                    {
                        heroElementDict[data.elementType].Add(data);
                    }
                }
            }
        }
        
        private void CreateHeroPools()
        {
            if (objectPoolManager == null) return;
            
            // Create pool for each hero prefab
            foreach (var kvp in heroPrefabDict)
            {
                string poolName = "Hero_" + kvp.Key;
                GameObject prefab = kvp.Value;
                
                if (!objectPoolManager.HasPool(poolName))
                {
                    // Each hero has a small initial pool size since they're not spawned often
                    objectPoolManager.CreatePool(poolName, prefab, 1, true, heroContainer);
                }
            }
        }
        #endregion
        
        #region Hero Creation
        public GameObject CreateHero(string heroId, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (string.IsNullOrEmpty(heroId))
            {
                Debug.LogError("Hero ID is null or empty!");
                return null;
            }
            
            // Get hero prefab
            GameObject heroPrefab = GetHeroPrefab(heroId);
            if (heroPrefab == null)
            {
                Debug.LogError($"Hero prefab with ID {heroId} not found!");
                
                // Use default prefab if available
                if (defaultHeroPrefab != null)
                {
                    heroPrefab = defaultHeroPrefab;
                }
                else
                {
                    return null;
                }
            }
            
            // Try to get from object pool
            GameObject hero = null;
            string poolName = "Hero_" + heroId;
            
            if (objectPoolManager != null && objectPoolManager.HasPool(poolName))
            {
                hero = objectPoolManager.GetPooledObject(poolName);
                
                if (hero != null)
                {
                    // Set position and rotation
                    hero.transform.position = position;
                    hero.transform.rotation = rotation;
                    
                    // Set parent if provided
                    if (parent != null)
                    {
                        hero.transform.SetParent(parent);
                    }
                    else if (heroContainer != null)
                    {
                        hero.transform.SetParent(heroContainer);
                    }
                }
            }
            
            // If not available in pool, instantiate directly
            if (hero == null)
            {
                Transform parentTransform = parent != null ? parent : heroContainer;
                hero = Instantiate(heroPrefab, position, rotation, parentTransform);
            }
            
            // Initialize hero
            HeroBase heroBase = hero.GetComponent<HeroBase>();
            if (heroBase != null)
            {
                // Initialize will be handled by HeroManager
                // This just ensures the component exists
            }
            
            return hero;
        }
        
        public GameObject CreateRandomHero(HeroRarity rarity, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // Get random hero of specified rarity
            string heroId = GetRandomHeroId(rarity);
            
            if (string.IsNullOrEmpty(heroId))
            {
                Debug.LogWarning($"No hero found for rarity {rarity}");
                return null;
            }
            
            return CreateHero(heroId, position, rotation, parent);
        }
        
        public GameObject CreateRandomHeroByElement(ElementType elementType, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // Get random hero of specified element
            string heroId = GetRandomHeroIdByElement(elementType);
            
            if (string.IsNullOrEmpty(heroId))
            {
                Debug.LogWarning($"No hero found for element {elementType}");
                return null;
            }
            
            return CreateHero(heroId, position, rotation, parent);
        }
        
        public GameObject CreateRandomHeroByRarityAndElement(HeroRarity rarity, ElementType elementType, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // Find heroes matching both criteria
            List<HeroPrefabData> matchingHeroes = new List<HeroPrefabData>();
            
            if (heroRarityDict.ContainsKey(rarity) && heroElementDict.ContainsKey(elementType))
            {
                foreach (HeroPrefabData hero in heroRarityDict[rarity])
                {
                    if (hero.elementType == elementType)
                    {
                        matchingHeroes.Add(hero);
                    }
                }
            }
            
            if (matchingHeroes.Count == 0)
            {
                Debug.LogWarning($"No hero found for rarity {rarity} and element {elementType}");
                return null;
            }
            
            // Select random hero from matching list
            int randomIndex = Random.Range(0, matchingHeroes.Count);
            string heroId = matchingHeroes[randomIndex].heroId;
            
            return CreateHero(heroId, position, rotation, parent);
        }
        #endregion
        
        #region Helper Methods
        private GameObject GetHeroPrefab(string heroId)
        {
            if (heroPrefabDict.ContainsKey(heroId))
            {
                return heroPrefabDict[heroId];
            }
            return null;
        }
        
        private string GetRandomHeroId(HeroRarity rarity)
        {
            if (!heroRarityDict.ContainsKey(rarity) || heroRarityDict[rarity].Count == 0)
            {
                return null;
            }
            
            List<HeroPrefabData> heroes = heroRarityDict[rarity];
            int randomIndex = Random.Range(0, heroes.Count);
            
            return heroes[randomIndex].heroId;
        }
        
        private string GetRandomHeroIdByElement(ElementType elementType)
        {
            if (!heroElementDict.ContainsKey(elementType) || heroElementDict[elementType].Count == 0)
            {
                return null;
            }
            
            List<HeroPrefabData> heroes = heroElementDict[elementType];
            int randomIndex = Random.Range(0, heroes.Count);
            
            return heroes[randomIndex].heroId;
        }
        
        public List<string> GetAllHeroIds()
        {
            return new List<string>(heroPrefabDict.Keys);
        }
        
        public List<string> GetHeroIdsByRarity(HeroRarity rarity)
        {
            List<string> heroIds = new List<string>();
            
            if (heroRarityDict.ContainsKey(rarity))
            {
                foreach (HeroPrefabData hero in heroRarityDict[rarity])
                {
                    heroIds.Add(hero.heroId);
                }
            }
            
            return heroIds;
        }
        
        public List<string> GetHeroIdsByElement(ElementType elementType)
        {
            List<string> heroIds = new List<string>();
            
            if (heroElementDict.ContainsKey(elementType))
            {
                foreach (HeroPrefabData hero in heroElementDict[elementType])
                {
                    heroIds.Add(hero.heroId);
                }
            }
            
            return heroIds;
        }
        
        public HeroPrefabData GetHeroPrefabData(string heroId)
        {
            return heroPrefabs.Find(h => h.heroId == heroId);
        }
        
        public void AddHeroPrefab(HeroPrefabData heroData)
        {
            // Add to prefabs list
            if (!heroPrefabs.Exists(h => h.heroId == heroData.heroId))
            {
                heroPrefabs.Add(heroData);
                
                // Add to prefab dictionary
                heroPrefabDict[heroData.heroId] = heroData.heroPrefab;
                
                // Add to rarity dictionary
                if (heroRarityDict.ContainsKey(heroData.rarity))
                {
                    heroRarityDict[heroData.rarity].Add(heroData);
                }
                
                // Add to element dictionary
                if (heroElementDict.ContainsKey(heroData.elementType))
                {
                    heroElementDict[heroData.elementType].Add(heroData);
                }
                
                // Create pool for this hero
                if (objectPoolManager != null)
                {
                    string poolName = "Hero_" + heroData.heroId;
                    if (!objectPoolManager.HasPool(poolName))
                    {
                        objectPoolManager.CreatePool(poolName, heroData.heroPrefab, 1, true, heroContainer);
                    }
                }
            }
        }
        
        public void RemoveHeroPrefab(string heroId)
        {
            // Find hero data
            HeroPrefabData heroData = heroPrefabs.Find(h => h.heroId == heroId);
            
            if (heroData != null)
            {
                // Remove from prefabs list
                heroPrefabs.Remove(heroData);
                
                // Remove from prefab dictionary
                if (heroPrefabDict.ContainsKey(heroId))
                {
                    heroPrefabDict.Remove(heroId);
                }
                
                // Remove from rarity dictionary
                if (heroRarityDict.ContainsKey(heroData.rarity))
                {
                    heroRarityDict[heroData.rarity].RemoveAll(h => h.heroId == heroId);
                }
                
                // Remove from element dictionary
                if (heroElementDict.ContainsKey(heroData.elementType))
                {
                    heroElementDict[heroData.elementType].RemoveAll(h => h.heroId == heroId);
                }
                
                // Clear pool for this hero
                if (objectPoolManager != null)
                {
                    string poolName = "Hero_" + heroId;
                    if (objectPoolManager.HasPool(poolName))
                    {
                        objectPoolManager.ClearPool(poolName);
                    }
                }
            }
        }
        #endregion
    }
    
    [System.Serializable]
    public class HeroPrefabData
    {
        public string heroId;
        public string heroName;
        public GameObject heroPrefab;
        public HeroRarity rarity = HeroRarity.Common;
        public ElementType elementType = ElementType.None;
    }
}