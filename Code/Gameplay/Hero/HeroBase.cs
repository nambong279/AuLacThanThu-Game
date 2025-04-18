using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Core;
using AuLacThanThu.Utils;

namespace AuLacThanThu.Gameplay.Hero
{
    /// <summary>
    /// Quản lý đội hình anh hùng, bao gồm việc mở khóa, nâng cấp, và sử dụng anh hùng
    /// </summary>
    public class HeroManager : MonoBehaviour
    {
        #region Singleton
        private static HeroManager _instance;
        public static HeroManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<HeroManager>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("HeroManager");
                        _instance = obj.AddComponent<HeroManager>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        [Header("Hero Settings")]
        [SerializeField] private int maxActiveHeroes = 4;
        [SerializeField] private int maxHeroLevel = 100;
        [SerializeField] private int maxHeroStars = 10;
        
        [Header("Hero Database")]
        [SerializeField] private List<HeroData> heroDatabase = new List<HeroData>();
        
        [Header("Owned Heroes")]
        [SerializeField] private List<OwnedHero> ownedHeroes = new List<OwnedHero>();
        
        [Header("Active Team")]
        [SerializeField] private List<string> activeHeroIds = new List<string>();
        [SerializeField] private List<HeroBase> activeHeroes = new List<HeroBase>();
        
        [Header("Hero Instances")]
        [SerializeField] private Transform heroContainer;
        [SerializeField] private GameObject heroPlaceholder;
        
        // Runtime references
        private ResourceManager resourceManager;
        private HeroEquipmentSystem equipmentSystem;
        #endregion
        
        #region Events
        public delegate void HeroEventHandler(OwnedHero hero);
        public event HeroEventHandler OnHeroObtained;
        public event HeroEventHandler OnHeroLeveledUp;
        public event HeroEventHandler OnHeroStarredUp;
        
        public delegate void TeamEventHandler(List<OwnedHero> team);
        public event TeamEventHandler OnTeamChanged;
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
        }
        
        private void Start()
        {
            // Get references
            resourceManager = ResourceManager.Instance;
            equipmentSystem = HeroEquipmentSystem.Instance;
            
            // Create hero container if needed
            if (heroContainer == null)
            {
                GameObject container = new GameObject("HeroContainer");
                heroContainer = container.transform;
            }
            
            // Load owned heroes from save data
            LoadHeroes();
            
            // Activate initial team
            ActivateTeam();
        }
        #endregion
        
        #region Hero Management
        private void LoadHeroes()
        {
            // For now, add some default heroes for testing
            // In a real implementation, this would load from SaveDataManager
            if (ownedHeroes.Count == 0 && heroDatabase.Count > 0)
            {
                // Add first hero from database (starter hero)
                if (heroDatabase.Count > 0)
                {
                    OwnedHero starter = new OwnedHero(heroDatabase[0]);
                    starter.isUnlocked = true;
                    ownedHeroes.Add(starter);
                    
                    // Add to active team if empty
                    if (activeHeroIds.Count == 0)
                    {
                        activeHeroIds.Add(starter.heroId);
                    }
                }
            }
        }
        
        public OwnedHero GetOwnedHero(string heroId)
        {
            return ownedHeroes.Find(h => h.heroId == heroId);
        }
        
        public bool IsHeroOwned(string heroId)
        {
            OwnedHero hero = GetOwnedHero(heroId);
            return hero != null && hero.isUnlocked;
        }
        
        public HeroData GetHeroData(string heroId)
        {
            return heroDatabase.Find(h => h.heroId == heroId);
        }
        
        public bool ObtainHero(string heroId, bool autoUnlock = true)
        {
            // Get hero data
            HeroData heroData = GetHeroData(heroId);
            
            if (heroData == null)
            {
                Debug.LogError($"Hero data not found for ID: {heroId}");
                return false;
            }
            
            // Check if already owned
            OwnedHero existingHero = GetOwnedHero(heroId);
            
            if (existingHero != null)
            {
                // Already owned, add fragments
                existingHero.fragments += heroData.fragmentsPerSummon;
                
                // Auto unlock if enough fragments
                if (autoUnlock && !existingHero.isUnlocked && existingHero.fragments >= heroData.fragmentsToUnlock)
                {
                    existingHero.isUnlocked = true;
                }
                
                // Trigger event
                OnHeroObtained?.Invoke(existingHero);
                
                return true;
            }
            else
            {
                // New hero
                OwnedHero newHero = new OwnedHero(heroData);
                newHero.fragments = heroData.fragmentsPerSummon;
                
                // Auto unlock if enough fragments or flagged
                if (autoUnlock || newHero.fragments >= heroData.fragmentsToUnlock)
                {
                    newHero.isUnlocked = true;
                }
                
                // Add to owned heroes
                ownedHeroes.Add(newHero);
                
                // Trigger event
                OnHeroObtained?.Invoke(newHero);
                
                return true;
            }
        }
        
        public bool LevelUpHero(string heroId, int levels = 1)
        {
            if (levels <= 0) return false;
            
            // Get owned hero
            OwnedHero hero = GetOwnedHero(heroId);
            
            if (hero == null || !hero.isUnlocked)
            {
                Debug.LogWarning($"Hero {heroId} not owned or not unlocked");
                return false;
            }
            
            // Check max level
            if (hero.level >= maxHeroLevel)
            {
                Debug.LogWarning($"Hero {heroId} already at max level");
                return false;
            }
            
            // Calculate actual levels to add (don't exceed max)
            int actualLevels = Mathf.Min(levels, maxHeroLevel - hero.level);
            
            // Calculate cost (would typically use a formula based on current level)
            int goldCost = CalculateLevelUpCost(hero.level, actualLevels);
            
            // Check if enough resources
            if (resourceManager != null && !resourceManager.SpendResource(ResourceType.Gold, goldCost))
            {
                Debug.LogWarning("Not enough gold to level up hero");
                return false;
            }
            
            // Update hero level
            hero.level += actualLevels;
            
            // Update active hero if in team
            UpdateActiveHero(hero.heroId);
            
            // Trigger event
            OnHeroLeveledUp?.Invoke(hero);
            
            Debug.Log($"Hero {hero.heroName} leveled up to {hero.level}");
            return true;
        }
        
        public bool StarUpHero(string heroId)
        {
            // Get owned hero
            OwnedHero hero = GetOwnedHero(heroId);
            
            if (hero == null || !hero.isUnlocked)
            {
                Debug.LogWarning($"Hero {heroId} not owned or not unlocked");
                return false;
            }
            
            // Check max stars
            if (hero.stars >= maxHeroStars)
            {
                Debug.LogWarning($"Hero {heroId} already at max stars");
                return false;
            }
            
            // Get hero data
            HeroData heroData = GetHeroData(heroId);
            
            if (heroData == null)
            {
                Debug.LogError($"Hero data not found for ID: {heroId}");
                return false;
            }
            
            // Calculate fragments needed
            int fragmentsNeeded = CalculateFragmentsForNextStar(hero.stars);
            
            // Check if enough fragments
            if (hero.fragments < fragmentsNeeded)
            {
                Debug.LogWarning($"Not enough fragments to star up hero. Have {hero.fragments}, need {fragmentsNeeded}");
                return false;
            }
            
            // Spend fragments
            hero.fragments -= fragmentsNeeded;
            
            // Increase star level
            hero.stars++;
            
            // Update active hero if in team
            UpdateActiveHero(hero.heroId);
            
            // Trigger event
            OnHeroStarredUp?.Invoke(hero);
            
            Debug.Log($"Hero {hero.heroName} starred up to {hero.stars} stars");
            return true;
        }
        #endregion
        
        #region Team Management
        public bool AddHeroToTeam(string heroId)
        {
            // Check if hero is owned and unlocked
            if (!IsHeroOwned(heroId))
            {
                Debug.LogWarning($"Hero {heroId} not owned or not unlocked");
                return false;
            }
            
            // Check if team is full
            if (activeHeroIds.Count >= maxActiveHeroes)
            {
                Debug.LogWarning("Team is already full");
                return false;
            }
            
            // Check if hero already in team
            if (activeHeroIds.Contains(heroId))
            {
                Debug.LogWarning($"Hero {heroId} already in team");
                return false;
            }
            
            // Add to active team
            activeHeroIds.Add(heroId);
            
            // Activate hero
            ActivateHero(heroId);
            
            // Trigger event
            OnTeamChanged?.Invoke(GetActiveTeam());
            
            return true;
        }
        
        public bool RemoveHeroFromTeam(string heroId)
        {
            // Check if hero is in team
            if (!activeHeroIds.Contains(heroId))
            {
                Debug.LogWarning($"Hero {heroId} not in team");
                return false;
            }
            
            // Remove from active team
            activeHeroIds.Remove(heroId);
            
            // Deactivate hero
            DeactivateHero(heroId);
            
            // Trigger event
            OnTeamChanged?.Invoke(GetActiveTeam());
            
            return true;
        }
        
        public bool SetTeam(List<string> heroIds)
        {
            // Check if all heroes are owned and unlocked
            foreach (string heroId in heroIds)
            {
                if (!IsHeroOwned(heroId))
                {
                    Debug.LogWarning($"Hero {heroId} not owned or not unlocked");
                    return false;
                }
            }
            
            // Check team size
            if (heroIds.Count > maxActiveHeroes)
            {
                Debug.LogWarning($"Team size exceeds maximum of {maxActiveHeroes}");
                return false;
            }
            
            // Deactivate current team
            DeactivateTeam();
            
            // Set new team
            activeHeroIds = new List<string>(heroIds);
            
            // Activate new team
            ActivateTeam();
            
            // Trigger event
            OnTeamChanged?.Invoke(GetActiveTeam());
            
            return true;
        }
        
        private void ActivateTeam()
        {
            // Activate each hero in team
            foreach (string heroId in activeHeroIds)
            {
                ActivateHero(heroId);
            }
        }
        
        private void DeactivateTeam()
        {
            // Create a copy to avoid modification during iteration
            List<HeroBase> heroes = new List<HeroBase>(activeHeroes);
            
            // Deactivate each hero
            foreach (HeroBase hero in heroes)
            {
                if (hero != null)
                {
                    DeactivateHero(hero.GetHeroId());
                }
            }
            
            // Clear active heroes list
            activeHeroes.Clear();
        }
        
        private void ActivateHero(string heroId)
        {
            // Check if already active
            if (activeHeroes.Exists(h => h != null && h.GetHeroId() == heroId))
            {
                return;
            }
            
            // Get hero data
            HeroData heroData = GetHeroData(heroId);
            OwnedHero ownedHero = GetOwnedHero(heroId);
            
            if (heroData == null || ownedHero == null)
            {
                Debug.LogError($"Hero data or owned hero not found for ID: {heroId}");
                return;
            }
            
            // Instantiate hero
            GameObject heroPrefab = heroData.heroPrefab != null ? heroData.heroPrefab : heroPlaceholder;
            
            if (heroPrefab == null)
            {
                Debug.LogError("No hero prefab or placeholder available");
                return;
            }
            
            GameObject heroObj = Instantiate(heroPrefab, heroContainer);
            
            // Get HeroBase component
            HeroBase heroComponent = heroObj.GetComponent<HeroBase>();
            
            if (heroComponent == null)
            {
                Debug.LogError($"HeroBase component not found on prefab for hero {heroId}");
                Destroy(heroObj);
                return;
            }
            
            // Initialize hero
            // TODO: Implement proper initialization with owned hero data
            
            // Add to active heroes
            activeHeroes.Add(heroComponent);
            
            // Activate hero
            heroComponent.Activate();
            
            Debug.Log($"Hero {heroData.heroName} activated");
        }
        
        private void DeactivateHero(string heroId)
        {
            // Find hero in active heroes
            HeroBase hero = activeHeroes.Find(h => h != null && h.GetHeroId() == heroId);
            
            if (hero == null)
            {
                return;
            }
            
            // Deactivate hero
            hero.Deactivate();
            
            // Remove from active heroes
            activeHeroes.Remove(hero);
            
            // Destroy game object
            Destroy(hero.gameObject);
            
            Debug.Log($"Hero {heroId} deactivated");
        }
        
        private void UpdateActiveHero(string heroId)
        {
            // Find hero in active heroes
            HeroBase hero = activeHeroes.Find(h => h != null && h.GetHeroId() == heroId);
            
            if (hero == null)
            {
                return;
            }
            
            // Reactivate hero to apply updates
            DeactivateHero(heroId);
            ActivateHero(heroId);
        }
        #endregion
        
        #region Helper Methods
        private int CalculateLevelUpCost(int currentLevel, int levelsToAdd)
        {
            // Simple formula: 100 * currentLevel * levelsToAdd
            return 100 * currentLevel * levelsToAdd;
        }
        
        private int CalculateFragmentsForNextStar(int currentStars)
        {
            // Formula: 10 * (2 ^ currentStars)
            return 10 * (int)Mathf.Pow(2, currentStars);
        }
        
        public List<OwnedHero> GetAllOwnedHeroes()
        {
            return new List<OwnedHero>(ownedHeroes);
        }
        
        public List<OwnedHero> GetUnlockedHeroes()
        {
            return ownedHeroes.FindAll(h => h.isUnlocked);
        }
        
        public List<OwnedHero> GetActiveTeam()
        {
            List<OwnedHero> team = new List<OwnedHero>();
            
            foreach (string heroId in activeHeroIds)
            {
                OwnedHero hero = GetOwnedHero(heroId);
                if (hero != null)
                {
                    team.Add(hero);
                }
            }
            
            return team;
        }
        
        public List<HeroData> GetAllHeroData()
        {
            return new List<HeroData>(heroDatabase);
        }
        
        public List<HeroBase> GetActiveHeroes()
        {
            // Clean up null references
            activeHeroes.RemoveAll(h => h == null);
            
            return new List<HeroBase>(activeHeroes);
        }
        #endregion
        
        #region Save/Load
        public List<HeroSaveData> GetSaveData()
        {
            List<HeroSaveData> saveData = new List<HeroSaveData>();
            
            foreach (OwnedHero hero in ownedHeroes)
            {
                HeroSaveData heroData = new HeroSaveData
                {
                    heroId = hero.heroId,
                    level = hero.level,
                    stars = hero.stars,
                    isUnlocked = hero.isUnlocked,
                    // TODO: Add equipment and skills
                };
                
                saveData.Add(heroData);
            }
            
            return saveData;
        }
        
        public void LoadSaveData(List<HeroSaveData> saveData)
        {
            if (saveData == null) return;
            
            // Clear current owned heroes
            ownedHeroes.Clear();
            
            foreach (HeroSaveData data in saveData)
            {
                // Get hero data
                HeroData heroData = GetHeroData(data.heroId);
                
                if (heroData != null)
                {
                    // Create owned hero
                    OwnedHero hero = new OwnedHero(heroData)
                    {
                        level = data.level,
                        stars = data.stars,
                        isUnlocked = data.isUnlocked
                        // TODO: Load equipment and skills
                    };
                    
                    // Add to owned heroes
                    ownedHeroes.Add(hero);
                }
            }
            
            // Reset and activate team
            DeactivateTeam();
            activeHeroIds.Clear();
            
            // Find first hero to add to team if empty
            if (activeHeroIds.Count == 0 && ownedHeroes.Count > 0)
            {
                OwnedHero firstHero = ownedHeroes.Find(h => h.isUnlocked);
                if (firstHero != null)
                {
                    activeHeroIds.Add(firstHero.heroId);
                }
            }
            
            ActivateTeam();
        }
        #endregion
    }
    
    #region Data Classes
    [System.Serializable]
    public class HeroData
    {
        public string heroId;
        public string heroName;
        public string description;
        public HeroRarity rarity;
        public ElementType elementType;
        public Sprite portraitIcon;
        public GameObject heroPrefab;
        
        [Header("Unlock Requirements")]
        public int fragmentsToUnlock = 10;
        public int fragmentsPerSummon = 5;
        
        [Header("Base Stats")]
        public float baseHealth = 100f;
        public float baseAttack = 10f;
        public float baseDefense = 5f;
        public float baseAttackSpeed = 1f;
    }
    
    [System.Serializable]
    public class OwnedHero
    {
        public string heroId;
        public string heroName;
        public int level = 1;
        public int stars = 1;
        public int fragments = 0;
        public bool isUnlocked = false;
        public HeroRarity rarity;
        public ElementType elementType;
        
        // Constructor
        public OwnedHero(HeroData data)
        {
            heroId = data.heroId;
            heroName = data.heroName;
            rarity = data.rarity;
            elementType = data.elementType;
        }
    }
    #endregion
}