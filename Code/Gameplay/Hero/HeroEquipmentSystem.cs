using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Utils;
using AuLacThanThu.Gameplay.Hero;

namespace AuLacThanThu.Gameplay.Hero
{
    #region Equipment Classes
    /// <summary>
    /// Đại diện cho một vật phẩm trang bị cho anh hùng
    /// </summary>
    [System.Serializable]
    public class EquipmentItem
    {
        public string ItemId;
        public string ItemName;
        public string Description;
        public int Level = 1;
        public int Stars = 1;
        public EquipmentSlotType SlotType;
        public EquipmentRarity Rarity;
        public Sprite ItemIcon;
        public List<StatBonus> StatBonuses = new List<StatBonus>();
        public ElementType ElementalBonus = ElementType.None;
        
        // Material costs for upgrades
        public int MetalCostPerLevel = 100;
        public int FabricCostPerLevel = 100;
        public int DivineStoneCostPerStar = 10;
        
        public EquipmentItem Clone()
        {
            EquipmentItem clone = new EquipmentItem
            {
                ItemId = this.ItemId,
                ItemName = this.ItemName,
                Description = this.Description,
                Level = this.Level,
                Stars = this.Stars,
                SlotType = this.SlotType,
                Rarity = this.Rarity,
                ItemIcon = this.ItemIcon,
                ElementalBonus = this.ElementalBonus,
                MetalCostPerLevel = this.MetalCostPerLevel,
                FabricCostPerLevel = this.FabricCostPerLevel,
                DivineStoneCostPerStar = this.DivineStoneCostPerStar
            };
            
            // Clone stat bonuses
            foreach (StatBonus bonus in StatBonuses)
            {
                clone.StatBonuses.Add(new StatBonus
                {
                    StatType = bonus.StatType,
                    Value = bonus.Value,
                    IsPercentage = bonus.IsPercentage
                });
            }
            
            return clone;
        }
        
        public bool UpgradeLevel(int levels = 1)
        {
            if (levels <= 0) return false;
            
            Level += levels;
            
            // Increase stats based on level
            foreach (StatBonus bonus in StatBonuses)
            {
                // Each level increases stats by 10%
                bonus.Value *= (1 + 0.1f * levels);
            }
            
            return true;
        }
        
        public bool UpgradeStar()
        {
            if (Stars >= 10) return false;
            
            Stars++;
            
            // Increase stats based on star
            foreach (StatBonus bonus in StatBonuses)
            {
                // Each star increases stats by 20%
                bonus.Value *= 1.2f;
            }
            
            return true;
        }
        
        public int GetUpgradeLevelMetalCost(int levels = 1)
        {
            return SlotType == EquipmentSlotType.Armor || 
                   SlotType == EquipmentSlotType.Boots || 
                   SlotType == EquipmentSlotType.Belt ? 
                   FabricCostPerLevel * levels : 
                   MetalCostPerLevel * levels;
        }
        
        public int GetUpgradeStarDivineStoneCost()
        {
            return DivineStoneCostPerStar * Stars;
        }
    }
    
    /// <summary>
    /// Định nghĩa cho mỗi chỉ số tăng thêm trên trang bị
    /// </summary>
    [System.Serializable]
    public class StatBonus
    {
        public StatType StatType;
        public float Value;
        public bool IsPercentage = false;
        
        public override string ToString()
        {
            string valueText = IsPercentage ? $"{Value * 100}%" : Value.ToString("F0");
            return $"{StatType}: +{valueText}";
        }
    }
    
    /// <summary>
    /// Quản lý một slot trang bị trên anh hùng
    /// </summary>
    [System.Serializable]
    public class EquipmentSlot
    {
        public EquipmentSlotType SlotType;
        public bool IsUnlocked = true;
        public int UnlockLevel = 1;
        
        [SerializeField] private EquipmentItem equippedItem = null;
        
        public bool CanEquip(EquipmentItem item)
        {
            if (!IsUnlocked) return false;
            if (item == null) return false;
            return item.SlotType == SlotType;
        }
        
        public bool EquipItem(EquipmentItem item)
        {
            if (!CanEquip(item)) return false;
            
            equippedItem = item;
            return true;
        }
        
        public EquipmentItem UnequipItem()
        {
            EquipmentItem item = equippedItem;
            equippedItem = null;
            return item;
        }
        
        public EquipmentItem GetEquippedItem()
        {
            return equippedItem;
        }
        
        public bool HasItem()
        {
            return equippedItem != null;
        }
    }
    #endregion
    
    public enum EquipmentRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Divine
    }
    
    /// <summary>
    /// Quản lý trang bị và nâng cấp trang bị cho anh hùng
    /// </summary>
    public class HeroEquipmentSystem : MonoBehaviour
    {
        #region Singleton
        private static HeroEquipmentSystem _instance;
        public static HeroEquipmentSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<HeroEquipmentSystem>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("HeroEquipmentSystem");
                        _instance = obj.AddComponent<HeroEquipmentSystem>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        [Header("Equipment Templates")]
        [SerializeField] private List<EquipmentItem> weaponTemplates = new List<EquipmentItem>();
        [SerializeField] private List<EquipmentItem> armorTemplates = new List<EquipmentItem>();
        [SerializeField] private List<EquipmentItem> bootsTemplates = new List<EquipmentItem>();
        [SerializeField] private List<EquipmentItem> ringTemplates = new List<EquipmentItem>();
        [SerializeField] private List<EquipmentItem> necklaceTemplates = new List<EquipmentItem>();
        [SerializeField] private List<EquipmentItem> beltTemplates = new List<EquipmentItem>();
        
        [Header("Player Inventory")]
        [SerializeField] private List<EquipmentItem> inventoryItems = new List<EquipmentItem>();
        
        [Header("Resources")]
        [SerializeField] private int metalAmount = 0;
        [SerializeField] private int fabricAmount = 0;
        [SerializeField] private int divineStoneAmount = 0;
        
        [Header("Templates")]
        [SerializeField] private GameObject equipmentSlotPrefab;
        
        // Cached references
        private ResourceManager resourceManager;
        #endregion
        
        #region Events
        public delegate void EquipmentEventHandler(EquipmentItem item);
        public event EquipmentEventHandler OnItemObtained;
        public event EquipmentEventHandler OnItemUpgraded;
        public event EquipmentEventHandler OnItemEquipped;
        public event EquipmentEventHandler OnItemUnequipped;
        #endregion
        
        #region Initialization
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
            // Get ResourceManager
            resourceManager = ResourceManager.Instance;
            
            // Load equipment data
            LoadEquipmentData();
        }
        
        private void LoadEquipmentData()
        {
            // This would typically load from save data, for now we'll use defaults
            // TODO: Load from saved data
        }
        #endregion
        
        #region Item Management
        public EquipmentItem CreateItem(string templateId, int level = 1, int stars = 1)
        {
            // Find template
            EquipmentItem template = FindItemTemplate(templateId);
            
            if (template == null)
            {
                Debug.LogError($"Item template not found: {templateId}");
                return null;
            }
            
            // Clone template
            EquipmentItem newItem = template.Clone();
            newItem.Level = level;
            newItem.Stars = stars;
            
            // Adjust stats based on level and stars
            if (level > 1 || stars > 1)
            {
                // Level adjustment
                if (level > 1)
                {
                    float levelMultiplier = 1.0f + (level - 1) * 0.1f; // 10% per level
                    
                    foreach (StatBonus bonus in newItem.StatBonuses)
                    {
                        bonus.Value *= levelMultiplier;
                    }
                }
                
                // Star adjustment
                if (stars > 1)
                {
                    float starMultiplier = 1.0f + (stars - 1) * 0.2f; // 20% per star
                    
                    foreach (StatBonus bonus in newItem.StatBonuses)
                    {
                        bonus.Value *= starMultiplier;
                    }
                }
            }
            
            return newItem;
        }
        
        public bool AddItemToInventory(EquipmentItem item)
        {
            if (item == null) return false;
            
            inventoryItems.Add(item);
            
            // Trigger event
            OnItemObtained?.Invoke(item);
            
            return true;
        }
        
        public bool RemoveItemFromInventory(EquipmentItem item)
        {
            if (item == null) return false;
            
            return inventoryItems.Remove(item);
        }
        
        public bool UpgradeItemLevel(EquipmentItem item, int levels = 1)
        {
            if (item == null || levels <= 0) return false;
            
            // Calculate cost
            int cost = item.GetUpgradeLevelMetalCost(levels);
            
            // Check if enough resources
            if (item.SlotType == EquipmentSlotType.Armor || 
                item.SlotType == EquipmentSlotType.Boots || 
                item.SlotType == EquipmentSlotType.Belt)
            {
                if (fabricAmount < cost)
                {
                    Debug.LogWarning("Not enough fabric to upgrade item");
                    return false;
                }
                
                fabricAmount -= cost;
            }
            else
            {
                if (metalAmount < cost)
                {
                    Debug.LogWarning("Not enough metal to upgrade item");
                    return false;
                }
                
                metalAmount -= cost;
            }
            
            // Upgrade item
            bool success = item.UpgradeLevel(levels);
            
            if (success)
            {
                // Trigger event
                OnItemUpgraded?.Invoke(item);
            }
            
            return success;
        }
        
        public bool UpgradeItemStar(EquipmentItem item)
        {
            if (item == null) return false;
            
            // Calculate cost
            int cost = item.GetUpgradeStarDivineStoneCost();
            
            // Check if enough resources
            if (divineStoneAmount < cost)
            {
                Debug.LogWarning("Not enough divine stones to upgrade item");
                return false;
            }
            
            // Deduct resources
            divineStoneAmount -= cost;
            
            // Upgrade item
            bool success = item.UpgradeStar();
            
            if (success)
            {
                // Trigger event
                OnItemUpgraded?.Invoke(item);
            }
            
            return success;
        }
        
        private EquipmentItem FindItemTemplate(string templateId)
        {
            // Search in all template lists
            foreach (EquipmentItem template in weaponTemplates)
            {
                if (template.ItemId == templateId) return template;
            }
            
            foreach (EquipmentItem template in armorTemplates)
            {
                if (template.ItemId == templateId) return template;
            }
            
            foreach (EquipmentItem template in bootsTemplates)
            {
                if (template.ItemId == templateId) return template;
            }
            
            foreach (EquipmentItem template in ringTemplates)
            {
                if (template.ItemId == templateId) return template;
            }
            
            foreach (EquipmentItem template in necklaceTemplates)
            {
                if (template.ItemId == templateId) return template;
            }
            
            foreach (EquipmentItem template in beltTemplates)
            {
                if (template.ItemId == templateId) return template;
            }
            
            return null;
        }
        #endregion
        
        #region Hero Equipment Management
        public bool EquipItemToHero(HeroBase hero, EquipmentItem item)
        {
            if (hero == null || item == null) return false;
            
            // Remove from inventory
            bool removed = RemoveItemFromInventory(item);
            
            if (!removed)
            {
                Debug.LogWarning("Item not found in inventory");
                return false;
            }
            
            // Equip to hero
            bool equipped = hero.EquipItem(item, item.SlotType);
            
            if (!equipped)
            {
                // If failed, add back to inventory
                AddItemToInventory(item);
                return false;
            }
            
            // Trigger event
            OnItemEquipped?.Invoke(item);
            
            return true;
        }
        
        public bool UnequipItemFromHero(HeroBase hero, EquipmentSlotType slotType)
        {
            if (hero == null) return false;
            
            // Unequip from hero
            EquipmentItem item = hero.UnequipItem(slotType);
            
            if (item == null)
            {
                return false;
            }
            
            // Add to inventory
            AddItemToInventory(item);
            
            // Trigger event
            OnItemUnequipped?.Invoke(item);
            
            return true;
        }
        #endregion
        
        #region Resource Management
        public void AddMetal(int amount)
        {
            if (amount <= 0) return;
            metalAmount += amount;
        }
        
        public void AddFabric(int amount)
        {
            if (amount <= 0) return;
            fabricAmount += amount;
        }
        
        public void AddDivineStone(int amount)
        {
            if (amount <= 0) return;
            divineStoneAmount += amount;
        }
        
        public int GetMetalAmount() => metalAmount;
        public int GetFabricAmount() => fabricAmount;
        public int GetDivineStoneAmount() => divineStoneAmount;
        #endregion
        
        #region Inventory Queries
        public List<EquipmentItem> GetAllItems()
        {
            return new List<EquipmentItem>(inventoryItems);
        }
        
        public List<EquipmentItem> GetItemsByType(EquipmentSlotType slotType)
        {
            return inventoryItems.FindAll(item => item.SlotType == slotType);
        }
        
        public List<EquipmentItem> GetItemsByRarity(EquipmentRarity rarity)
        {
            return inventoryItems.FindAll(item => item.Rarity == rarity);
        }
        
        public EquipmentItem GetItemById(string itemId)
        {
            return inventoryItems.Find(item => item.ItemId == itemId);
        }
        #endregion
    }
}