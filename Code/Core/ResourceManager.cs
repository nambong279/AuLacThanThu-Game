using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Core;

namespace AuLacThanThu.Core
{
    /// <summary>
    /// Quản lý tài nguyên trong game (vàng, kim cương, năng lượng, vật phẩm...)
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        #region Singleton
        private static ResourceManager _instance;
        public static ResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ResourceManager>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("ResourceManager");
                        _instance = obj.AddComponent<ResourceManager>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        [Header("Currencies")]
        [SerializeField] private int gold = 0;
        [SerializeField] private int diamond = 0;
        [SerializeField] private int energy = 100;
        [SerializeField] private int maxEnergy = 100;
        
        [Header("Crafting Materials")]
        [SerializeField] private int iron = 0;            // Sắt - nâng cấp vũ khí, nhẫn, dây chuyền
        [SerializeField] private int fabric = 0;          // Vải - nâng cấp áo, giày, đai lưng
        [SerializeField] private int divineStone = 0;     // Đá thần - tiến bậc trang bị/tiến hóa
        
        [Header("Special Items")]
        [SerializeField] private int skillBook = 0;           // Sách kỹ năng - nâng cấp kỹ năng thành trì
        [SerializeField] private int silverLacHongMark = 0;   // Dấu ấn Lạc Hồng bạc - gacha anh hùng
        [SerializeField] private int goldenLacHongMark = 0;   // Dấu ấn Lạc Hồng vàng - gacha anh hùng cao cấp
        [SerializeField] private Dictionary<string, int> heroFragments = new Dictionary<string, int>();  // Mảnh anh hùng
        
        [Header("Energy Settings")]
        [SerializeField] private float energyRefillTime = 360f;  // Seconds per energy point (360s = 6 min)
        private float energyRefillTimer = 0f;
        
        [Header("AFK Rewards")]
        [SerializeField] private float goldPerMinute = 10f;
        [SerializeField] private float expPerMinute = 5f;
        [SerializeField] private float ironPerMinute = 1f;
        [SerializeField] private float fabricPerMinute = 1f;
        [SerializeField] private float maxOfflineTime = 43200f;  // 12 hours in seconds
        private System.DateTime lastLogoutTime;
        
        // Cached components
        private SaveDataManager saveDataManager;
        #endregion
        
        #region Events
        public delegate void ResourceChangedHandler(ResourceType type, int amount, int delta);
        public event ResourceChangedHandler OnResourceChanged;
        
        public delegate void EnergyRefillHandler(int currentEnergy, int maxEnergy);
        public event EnergyRefillHandler OnEnergyRefilled;
        
        public delegate void AFKRewardHandler(AFKRewards rewards);
        public event AFKRewardHandler OnAFKRewardCollected;
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
            saveDataManager = SaveDataManager.Instance;
            
            // Load resources
            LoadResources();
            
            // Check for AFK rewards
            CheckAFKRewards();
        }
        
        private void Update()
        {
            // Handle energy refill
            if (energy < maxEnergy)
            {
                energyRefillTimer += Time.deltaTime;
                
                if (energyRefillTimer >= energyRefillTime)
                {
                    energyRefillTimer = 0f;
                    RefillEnergy(1);
                }
            }
        }
        
        private void OnApplicationQuit()
        {
            // Save logout time
            lastLogoutTime = System.DateTime.Now;
            PlayerPrefs.SetString("LastLogoutTime", lastLogoutTime.ToString());
            PlayerPrefs.Save();
            
            // Save resources
            SaveResources();
        }
        #endregion
        
        #region Resource Management
        private void LoadResources()
        {
            // This would typically load from SaveDataManager
            // For now, use default values or load from PlayerPrefs
            
            gold = PlayerPrefs.GetInt("Gold", 1000);
            diamond = PlayerPrefs.GetInt("Diamond", 100);
            energy = PlayerPrefs.GetInt("Energy", 100);
            
            iron = PlayerPrefs.GetInt("Iron", 50);
            fabric = PlayerPrefs.GetInt("Fabric", 50);
            divineStone = PlayerPrefs.GetInt("DivineStone", 10);
            
            skillBook = PlayerPrefs.GetInt("SkillBook", 1);
            silverLacHongMark = PlayerPrefs.GetInt("SilverLacHongMark", 10);
            goldenLacHongMark = PlayerPrefs.GetInt("GoldenLacHongMark", 1);
            
            // Load last logout time
            string lastLogoutTimeStr = PlayerPrefs.GetString("LastLogoutTime", System.DateTime.Now.ToString());
            if (System.DateTime.TryParse(lastLogoutTimeStr, out lastLogoutTime))
            {
                // Successfully parsed
            }
            else
            {
                // If parsing fails, use current time
                lastLogoutTime = System.DateTime.Now;
            }
        }
        
        private void SaveResources()
        {
            // Save to PlayerPrefs for persistence
            PlayerPrefs.SetInt("Gold", gold);
            PlayerPrefs.SetInt("Diamond", diamond);
            PlayerPrefs.SetInt("Energy", energy);
            
            PlayerPrefs.SetInt("Iron", iron);
            PlayerPrefs.SetInt("Fabric", fabric);
            PlayerPrefs.SetInt("DivineStone", divineStone);
            
            PlayerPrefs.SetInt("SkillBook", skillBook);
            PlayerPrefs.SetInt("SilverLacHongMark", silverLacHongMark);
            PlayerPrefs.SetInt("GoldenLacHongMark", goldenLacHongMark);
            
            // Save hero fragments and other complex data through SaveDataManager
            if (saveDataManager != null)
            {
                // TODO: Implement saving via SaveDataManager
            }
            
            PlayerPrefs.Save();
        }
        
        // Generic method to add resources
        public bool AddResource(ResourceType resourceType, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"Invalid amount to add: {amount}");
                return false;
            }
            
            int previousAmount = GetResourceAmount(resourceType);
            
            switch (resourceType)
            {
                case ResourceType.Gold:
                    gold += amount;
                    break;
                    
                case ResourceType.Diamond:
                    diamond += amount;
                    break;
                    
                case ResourceType.Energy:
                    energy = Mathf.Min(energy + amount, maxEnergy);
                    break;
                    
                case ResourceType.Iron:
                    iron += amount;
                    break;
                    
                case ResourceType.Fabric:
                    fabric += amount;
                    break;
                    
                case ResourceType.DivineStone:
                    divineStone += amount;
                    break;
                    
                case ResourceType.SkillBook:
                    skillBook += amount;
                    break;
                    
                case ResourceType.SilverLacHongMark:
                    silverLacHongMark += amount;
                    break;
                    
                case ResourceType.GoldenLacHongMark:
                    goldenLacHongMark += amount;
                    break;
                    
                default:
                    Debug.LogWarning($"Resource type not handled: {resourceType}");
                    return false;
            }
            
            // Trigger event
            OnResourceChanged?.Invoke(resourceType, GetResourceAmount(resourceType), amount);
            
            return true;
        }
        
        // Generic method to spend resources
        public bool SpendResource(ResourceType resourceType, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"Invalid amount to spend: {amount}");
                return false;
            }
            
            // Check if we have enough
            if (GetResourceAmount(resourceType) < amount)
            {
                Debug.LogWarning($"Not enough {resourceType}. Have {GetResourceAmount(resourceType)}, need {amount}");
                return false;
            }
            
            int previousAmount = GetResourceAmount(resourceType);
            
            switch (resourceType)
            {
                case ResourceType.Gold:
                    gold -= amount;
                    break;
                    
                case ResourceType.Diamond:
                    diamond -= amount;
                    break;
                    
                case ResourceType.Energy:
                    energy -= amount;
                    break;
                    
                case ResourceType.Iron:
                    iron -= amount;
                    break;
                    
                case ResourceType.Fabric:
                    fabric -= amount;
                    break;
                    
                case ResourceType.DivineStone:
                    divineStone -= amount;
                    break;
                    
                case ResourceType.SkillBook:
                    skillBook -= amount;
                    break;
                    
                case ResourceType.SilverLacHongMark:
                    silverLacHongMark -= amount;
                    break;
                    
                case ResourceType.GoldenLacHongMark:
                    goldenLacHongMark -= amount;
                    break;
                    
                default:
                    Debug.LogWarning($"Resource type not handled: {resourceType}");
                    return false;
            }
            
            // Trigger event
            OnResourceChanged?.Invoke(resourceType, GetResourceAmount(resourceType), -amount);
            
            return true;
        }
        
        public int GetResourceAmount(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Gold:
                    return gold;
                    
                case ResourceType.Diamond:
                    return diamond;
                    
                case ResourceType.Energy:
                    return energy;
                    
                case ResourceType.Iron:
                    return iron;
                    
                case ResourceType.Fabric:
                    return fabric;
                    
                case ResourceType.DivineStone:
                    return divineStone;
                    
                case ResourceType.SkillBook:
                    return skillBook;
                    
                case ResourceType.SilverLacHongMark:
                    return silverLacHongMark;
                    
                case ResourceType.GoldenLacHongMark:
                    return goldenLacHongMark;
                    
                default:
                    Debug.LogWarning($"Resource type not handled: {resourceType}");
                    return 0;
            }
        }
        
        public int GetHeroFragments(string heroId)
        {
            if (heroFragments.ContainsKey(heroId))
            {
                return heroFragments[heroId];
            }
            return 0;
        }
        
        public bool AddHeroFragments(string heroId, int amount)
        {
            if (amount <= 0) return false;
            
            if (!heroFragments.ContainsKey(heroId))
            {
                heroFragments[heroId] = 0;
            }
            
            heroFragments[heroId] += amount;
            
            // Trigger event if needed
            // OnHeroFragmentChanged?.Invoke(heroId, heroFragments[heroId], amount);
            
            return true;
        }
        
        public bool SpendHeroFragments(string heroId, int amount)
        {
            if (amount <= 0) return false;
            
            if (!heroFragments.ContainsKey(heroId) || heroFragments[heroId] < amount)
            {
                Debug.LogWarning($"Not enough fragments for hero {heroId}");
                return false;
            }
            
            heroFragments[heroId] -= amount;
            
            // Trigger event if needed
            // OnHeroFragmentChanged?.Invoke(heroId, heroFragments[heroId], -amount);
            
            return true;
        }
        
        private void RefillEnergy(int amount)
        {
            if (amount <= 0) return;
            
            int previousEnergy = energy;
            energy = Mathf.Min(energy + amount, maxEnergy);
            
            if (energy > previousEnergy)
            {
                energyRefillTimer = 0f;
                OnEnergyRefilled?.Invoke(energy, maxEnergy);
            }
        }
        #endregion
        
        #region AFK Rewards
        private void CheckAFKRewards()
        {
            System.DateTime now = System.DateTime.Now;
            System.TimeSpan offlineTime = now - lastLogoutTime;
            
            // Cap offline time
            float offlineSeconds = Mathf.Min((float)offlineTime.TotalSeconds, maxOfflineTime);
            
            if (offlineSeconds > 60f) // Only process if offline for more than 1 minute
            {
                // Calculate rewards
                AFKRewards rewards = CalculateAFKRewards(offlineSeconds);
                
                // Show reward dialog or auto-collect
                Debug.Log($"AFK rewards calculated for {offlineSeconds / 60f} minutes");
                
                // For now, automatically collect rewards
                CollectAFKRewards(rewards);
            }
        }
        
        private AFKRewards CalculateAFKRewards(float offlineSeconds)
        {
            float offlineMinutes = offlineSeconds / 60f;
            
            AFKRewards rewards = new AFKRewards
            {
                gold = Mathf.FloorToInt(goldPerMinute * offlineMinutes),
                experience = Mathf.FloorToInt(expPerMinute * offlineMinutes),
                iron = Mathf.FloorToInt(ironPerMinute * offlineMinutes),
                fabric = Mathf.FloorToInt(fabricPerMinute * offlineMinutes),
                offlineTime = offlineSeconds
            };
            
            return rewards;
        }
        
        public void CollectAFKRewards(AFKRewards rewards, bool doubled = false)
        {
            // Apply multiplier if doubled (e.g., from watching an ad)
            int multiplier = doubled ? 2 : 1;
            
            // Add resources
            AddResource(ResourceType.Gold, rewards.gold * multiplier);
            // Experience should be applied to player level or heroes
            AddResource(ResourceType.Iron, rewards.iron * multiplier);
            AddResource(ResourceType.Fabric, rewards.fabric * multiplier);
            
            // Trigger event
            OnAFKRewardCollected?.Invoke(rewards);
            
            // Reset last logout time
            lastLogoutTime = System.DateTime.Now;
        }
        #endregion
    }
    
    public enum ResourceType
    {
        Gold,
        Diamond,
        Energy,
        Iron,
        Fabric,
        DivineStone,
        SkillBook,
        SilverLacHongMark,
        GoldenLacHongMark,
        HeroFragment
    }
    
    public class AFKRewards
    {
        public int gold;
        public int experience;
        public int iron;
        public int fabric;
        public float offlineTime;  // seconds
    }
}