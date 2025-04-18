using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Core;
using AuLacThanThu.Gameplay.Tower;

namespace AuLacThanThu.Gameplay.Tower
{
    /// <summary>
    /// Quản lý việc nâng cấp cho thành trì
    /// </summary>
    public class FortressUpgradeSystem : MonoBehaviour
    {
        #region Singleton
        private static FortressUpgradeSystem _instance;
        public static FortressUpgradeSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<FortressUpgradeSystem>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("FortressUpgradeSystem");
                        _instance = obj.AddComponent<FortressUpgradeSystem>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        [Header("Component References")]
        [SerializeField] private FortressController fortressController;
        [SerializeField] private CrossbowController crossbowController;
        
        [Header("Upgrade Settings")]
        [SerializeField] private int maxWallLevel = 500;
        [SerializeField] private int maxGateLevel = 500;
        [SerializeField] private int maxCrossbowLevel = 500;
        
        [Header("Skill Settings")]
        [SerializeField] private int skillUnlockLevel = 10;
        [SerializeField] private List<FortressSkill> fortressSkills = new List<FortressSkill>();
        
        // Upgrade costs
        [SerializeField] private int baseWallUpgradeCost = 100;
        [SerializeField] private int baseGateUpgradeCost = 100;
        [SerializeField] private int baseCrossbowUpgradeCost = 100;
        [SerializeField] private float costIncreaseRate = 1.1f;
        
        // Skill upgrade costs
        [SerializeField] private int baseSkillUpgradeCost = 1;
        
        // Current levels
        private int wallLevel = 1;
        private int gateLevel = 1;
        private int crossbowLevel = 1;
        
        // Cached components
        private ResourceManager resourceManager;
        #endregion
        
        #region Events
        public delegate void FortressUpgradedHandler(FortressComponent component, int level);
        public event FortressUpgradedHandler OnFortressUpgraded;
        
        public delegate void SkillUpgradedHandler(FortressSkill skill, int level);
        public event SkillUpgradedHandler OnSkillUpgraded;
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
            
            // Find fortress components if not set
            if (fortressController == null)
            {
                fortressController = FindObjectOfType<FortressController>();
            }
            
            if (crossbowController == null)
            {
                crossbowController = FindObjectOfType<CrossbowController>();
            }
            
            // Initialize skills
            InitializeSkills();
        }
        #endregion
        
        #region Initialization
        private void InitializeSkills()
        {
            // Initialize default skills if empty
            if (fortressSkills.Count == 0)
            {
                // Skill 1: Increase EXP gained (max level 10)
                fortressSkills.Add(new FortressSkill
                {
                    skillId = "skill_exp_boost",
                    skillName = "Kinh Nghiệm Vượt Trội",
                    description = "Tăng EXP nhận được từ kẻ địch",
                    iconSprite = null,
                    maxLevel = 10,
                    currentLevel = 0,
                    skillType = FortressSkillType.ExpGain,
                    baseValue = 0.05f,
                    valuePerLevel = 0.05f,
                    unlocked = true
                });
                
                // Skill 2: Increase attack speed (max level 10)
                fortressSkills.Add(new FortressSkill
                {
                    skillId = "skill_attack_speed",
                    skillName = "Tốc Độ Thần Công",
                    description = "Tăng tốc độ tấn công của nỏ thần",
                    iconSprite = null,
                    maxLevel = 10,
                    currentLevel = 0,
                    skillType = FortressSkillType.AttackSpeed,
                    baseValue = 0.0f,
                    valuePerLevel = 0.1f,
                    unlocked = true
                });
                
                // Skill 3: Increase crit chance (max level 10)
                fortressSkills.Add(new FortressSkill
                {
                    skillId = "skill_crit_chance",
                    skillName = "Thiên Nhãn Chí Mạng",
                    description = "Tăng tỉ lệ chí mạng cho nỏ thần",
                    iconSprite = null,
                    maxLevel = 10,
                    currentLevel = 0,
                    skillType = FortressSkillType.CritChance,
                    baseValue = 0.0f,
                    valuePerLevel = 0.05f,
                    unlocked = true
                });
                
                // Add more skills as needed...
                
                // Skill 4: Increase crit damage (max level 10)
                fortressSkills.Add(new FortressSkill
                {
                    skillId = "skill_crit_damage",
                    skillName = "Uy Lực Xuyên Phá",
                    description = "Tăng sát thương chí mạng của nỏ thần",
                    iconSprite = null,
                    maxLevel = 10,
                    currentLevel = 0,
                    skillType = FortressSkillType.CritDamage,
                    baseValue = 0.0f,
                    valuePerLevel = 0.2f,
                    unlocked = true
                });
                
                // Skill 5: Increase dodge chance (max level 10)
                fortressSkills.Add(new FortressSkill
                {
                    skillId = "skill_dodge",
                    skillName = "Thiên Địa Độn Pháp",
                    description = "Tăng khả năng né tránh cho thành",
                    iconSprite = null,
                    maxLevel = 10,
                    currentLevel = 0,
                    skillType = FortressSkillType.DodgeChance,
                    baseValue = 0.0f,
                    valuePerLevel = 0.05f,
                    unlocked = true
                });
                
                // Skill 6: Reduce crit damage (max level 10)
                fortressSkills.Add(new FortressSkill
                {
                    skillId = "skill_crit_reduction",
                    skillName = "Gỗ Lim Kiên Cố",
                    description = "Giảm sát thương chí mạng nhận vào",
                    iconSprite = null,
                    maxLevel = 10,
                    currentLevel = 0,
                    skillType = FortressSkillType.CritDamageReduction,
                    baseValue = 0.0f,
                    valuePerLevel = 0.2f,
                    unlocked = true
                });
                
                // More skills...
                
                // Skill 14-16: Increase global stats (max level 100)
                fortressSkills.Add(new FortressSkill
                {
                    skillId = "skill_attack_percent",
                    skillName = "Ưu Thế Vũ Lực",
                    description = "Tăng % tấn công cho thành và anh hùng",
                    iconSprite = null,
                    maxLevel = 100,
                    currentLevel = 0,
                    skillType = FortressSkillType.AttackPercent,
                    baseValue = 0.0f,
                    valuePerLevel = 0.05f,
                    unlocked = true
                });
                
                fortressSkills.Add(new FortressSkill
                {
                    skillId = "skill_defense_percent",
                    skillName = "Phòng Thủ Tuyệt Đối",
                    description = "Tăng % phòng thủ cho thành và anh hùng",
                    iconSprite = null,
                    maxLevel = 100,
                    currentLevel = 0,
                    skillType = FortressSkillType.DefensePercent,
                    baseValue = 0.0f,
                    valuePerLevel = 0.05f,
                    unlocked = true
                });
                
                fortressSkills.Add(new FortressSkill
                {
                    skillId = "skill_hp_percent",
                    skillName = "Sinh Lực Tràn Đầy",
                    description = "Tăng % HP cho thành và anh hùng",
                    iconSprite = null,
                    maxLevel = 100,
                    currentLevel = 0,
                    skillType = FortressSkillType.HPPercent,
                    baseValue = 0.0f,
                    valuePerLevel = 0.05f,
                    unlocked = true
                });
            }
        }
        #endregion
        
        #region Fortress Upgrades
        public bool UpgradeWall()
        {
            if (wallLevel >= maxWallLevel)
            {
                Debug.LogWarning("Wall already at max level");
                return false;
            }
            
            // Calculate cost
            int cost = CalculateUpgradeCost(baseWallUpgradeCost, wallLevel);
            
            // Check if enough resources
            if (resourceManager != null && !resourceManager.SpendResource(ResourceType.Gold, cost))
            {
                Debug.LogWarning("Not enough gold to upgrade wall");
                return false;
            }
            
            // Upgrade wall
            wallLevel++;
            
            // Apply upgrade to fortress
            if (fortressController != null)
            {
                fortressController.UpgradeWall(wallLevel);
            }
            
            // Check for skill unlock
            if (wallLevel >= skillUnlockLevel && gateLevel >= skillUnlockLevel && crossbowLevel >= skillUnlockLevel)
            {
                // Give skill book
                if (resourceManager != null)
                {
                    resourceManager.AddResource(ResourceType.SkillBook, 1);
                }
            }
            
            // Trigger event
            OnFortressUpgraded?.Invoke(FortressComponent.Wall, wallLevel);
            
            Debug.Log($"Wall upgraded to level {wallLevel}");
            return true;
        }
        
        public bool UpgradeGate()
        {
            if (gateLevel >= maxGateLevel)
            {
                Debug.LogWarning("Gate already at max level");
                return false;
            }
            
            // Calculate cost
            int cost = CalculateUpgradeCost(baseGateUpgradeCost, gateLevel);
            
            // Check if enough resources
            if (resourceManager != null && !resourceManager.SpendResource(ResourceType.Gold, cost))
            {
                Debug.LogWarning("Not enough gold to upgrade gate");
                return false;
            }
            
            // Upgrade gate
            gateLevel++;
            
            // Apply upgrade to fortress
            if (fortressController != null)
            {
                fortressController.UpgradeGate(gateLevel);
            }
            
            // Check for skill unlock
            if (wallLevel >= skillUnlockLevel && gateLevel >= skillUnlockLevel && crossbowLevel >= skillUnlockLevel)
            {
                // Give skill book
                if (resourceManager != null)
                {
                    resourceManager.AddResource(ResourceType.SkillBook, 1);
                }
            }
            
            // Trigger event
            OnFortressUpgraded?.Invoke(FortressComponent.Gate, gateLevel);
            
            Debug.Log($"Gate upgraded to level {gateLevel}");
            return true;
        }
        
        public bool UpgradeCrossbow()
        {
            if (crossbowLevel >= maxCrossbowLevel)
            {
                Debug.LogWarning("Crossbow already at max level");
                return false;
            }
            
            // Calculate cost
            int cost = CalculateUpgradeCost(baseCrossbowUpgradeCost, crossbowLevel);
            
            // Check if enough resources
            if (resourceManager != null && !resourceManager.SpendResource(ResourceType.Gold, cost))
            {
                Debug.LogWarning("Not enough gold to upgrade crossbow");
                return false;
            }
            
            // Upgrade crossbow
            crossbowLevel++;
            
            // Apply upgrade to crossbow
            if (fortressController != null)
            {
                fortressController.UpgradeCrossbow(crossbowLevel);
            }
            
            // Check for skill unlock
            if (wallLevel >= skillUnlockLevel && gateLevel >= skillUnlockLevel && crossbowLevel >= skillUnlockLevel)
            {
                // Give skill book
                if (resourceManager != null)
                {
                    resourceManager.AddResource(ResourceType.SkillBook, 1);
                }
            }
            
            // Trigger event
            OnFortressUpgraded?.Invoke(FortressComponent.Crossbow, crossbowLevel);
            
            Debug.Log($"Crossbow upgraded to level {crossbowLevel}");
            return true;
        }
        
        private int CalculateUpgradeCost(int baseCost, int currentLevel)
        {
            // Formula: BaseCost * (CostIncreaseRate ^ (CurrentLevel - 1))
            return Mathf.RoundToInt(baseCost * Mathf.Pow(costIncreaseRate, currentLevel - 1));
        }
        
        public int GetUpgradeCost(FortressComponent component)
        {
            switch (component)
            {
                case FortressComponent.Wall:
                    return CalculateUpgradeCost(baseWallUpgradeCost, wallLevel);
                    
                case FortressComponent.Gate:
                    return CalculateUpgradeCost(baseGateUpgradeCost, gateLevel);
                    
                case FortressComponent.Crossbow:
                    return CalculateUpgradeCost(baseCrossbowUpgradeCost, crossbowLevel);
                    
                default:
                    return 0;
            }
        }
        #endregion
        
        #region Skill Upgrades
        public bool UpgradeSkill(string skillId)
        {
            // Find skill
            FortressSkill skill = fortressSkills.Find(s => s.skillId == skillId);
            
            if (skill == null)
            {
                Debug.LogWarning($"Skill with ID {skillId} not found");
                return false;
            }
            
            // Check if skill is at max level
            if (skill.currentLevel >= skill.maxLevel)
            {
                Debug.LogWarning($"Skill {skill.skillName} already at max level");
                return false;
            }
            
            // Check if skill is unlocked
            if (!skill.unlocked)
            {
                Debug.LogWarning($"Skill {skill.skillName} is not unlocked yet");
                return false;
            }
            
            // Calculate cost (1 skill book per upgrade)
            int cost = baseSkillUpgradeCost;
            
            // Check if enough resources
            if (resourceManager != null && !resourceManager.SpendResource(ResourceType.SkillBook, cost))
            {
                Debug.LogWarning("Not enough skill books to upgrade skill");
                return false;
            }
            
            // Upgrade skill
            skill.currentLevel++;
            
            // Apply skill effect
            ApplySkillEffect(skill);
            
            // Trigger event
            OnSkillUpgraded?.Invoke(skill, skill.currentLevel);
            
            Debug.Log($"Skill {skill.skillName} upgraded to level {skill.currentLevel}");
            return true;
        }
        
        private void ApplySkillEffect(FortressSkill skill)
        {
            // Calculate effect value
            float effectValue = skill.baseValue + (skill.currentLevel * skill.valuePerLevel);
            
            // Apply effect based on skill type
            switch (skill.skillType)
            {
                case FortressSkillType.ExpGain:
                    // Apply to GameManager or LevelManager
                    // TODO: Implement exp gain boost
                    break;
                    
                case FortressSkillType.AttackSpeed:
                    // Increase crossbow attack speed
                    if (crossbowController != null)
                    {
                        crossbowController.UpgradeAttackSpeed(skill.valuePerLevel);
                    }
                    break;
                    
                case FortressSkillType.CritChance:
                    // Increase crossbow crit chance
                    if (crossbowController != null)
                    {
                        crossbowController.UpgradeCritChance(skill.valuePerLevel);
                    }
                    break;
                    
                case FortressSkillType.CritDamage:
                    // Increase crossbow crit damage
                    if (crossbowController != null)
                    {
                        crossbowController.UpgradeCritDamage(skill.valuePerLevel);
                    }
                    break;
                    
                case FortressSkillType.DodgeChance:
                    // Increase fortress dodge chance
                    if (fortressController != null)
                    {
                        fortressController.ApplyStatModifier(FortressStatType.DodgeChance, skill.valuePerLevel);
                    }
                    break;
                    
                case FortressSkillType.CritReductionChance:
                    // Increase fortress crit reduction chance
                    if (fortressController != null)
                    {
                        fortressController.ApplyStatModifier(FortressStatType.CritReductionChance, skill.valuePerLevel);
                    }
                    break;
                    
                case FortressSkillType.CritDamageReduction:
                    // Increase fortress crit damage reduction
                    if (fortressController != null)
                    {
                        fortressController.ApplyStatModifier(FortressStatType.CritReduction, skill.valuePerLevel);
                    }
                    break;
                    
                case FortressSkillType.DodgeIgnore:
                    // Increase crossbow dodge ignore
                    if (crossbowController != null)
                    {
                        // TODO: Implement dodge ignore upgrade
                    }
                    break;
                    
                case FortressSkillType.DamageReduction:
                    // Increase fortress damage reduction
                    if (fortressController != null)
                    {
                        fortressController.ApplyStatModifier(FortressStatType.DamageReduction, skill.valuePerLevel);
                    }
                    break;
                    
                case FortressSkillType.DamageIncrease:
                    // Increase crossbow damage
                    if (crossbowController != null)
                    {
                        crossbowController.UpgradeDamage(skill.valuePerLevel);
                    }
                    break;
                    
                case FortressSkillType.HPRegen:
                    // Increase fortress HP regen
                    if (fortressController != null)
                    {
                        fortressController.ApplyStatModifier(FortressStatType.HealthRegen, skill.valuePerLevel);
                    }
                    break;
                    
                case FortressSkillType.Armor:
                    // Increase fortress armor
                    if (fortressController != null)
                    {
                        fortressController.ApplyStatModifier(FortressStatType.Armor, 10f); // 10 armor per level
                    }
                    break;
                    
                case FortressSkillType.ArmorPenetration:
                    // Increase crossbow armor penetration
                    if (crossbowController != null)
                    {
                        // TODO: Implement armor penetration upgrade
                    }
                    break;
                    
                case FortressSkillType.AttackPercent:
                    // Increase global attack percentage
                    if (fortressController != null)
                    {
                        fortressController.ApplyStatModifier(FortressStatType.MaxHealth, skill.valuePerLevel);
                    }
                    break;
                    
                case FortressSkillType.DefensePercent:
                    // Increase global defense percentage
                    // TODO: Implement global stats for heroes
                    break;
                    
                case FortressSkillType.HPPercent:
                    // Increase global HP percentage
                    // TODO: Implement global stats for heroes
                    break;
                    
                default:
                    Debug.LogWarning($"Skill type {skill.skillType} not implemented");
                    break;
            }
        }
        
        public List<FortressSkill> GetAllSkills()
        {
            return fortressSkills;
        }
        
        public FortressSkill GetSkill(string skillId)
        {
            return fortressSkills.Find(s => s.skillId == skillId);
        }
        
        public Dictionary<string, int> GetSkillLevels()
        {
            Dictionary<string, int> skillLevels = new Dictionary<string, int>();
            
            foreach (FortressSkill skill in fortressSkills)
            {
                skillLevels[skill.skillId] = skill.currentLevel;
            }
            
            return skillLevels;
        }
        #endregion
        
        #region Save/Load Data
        public void LoadSaveData(int savedWallLevel, int savedGateLevel, int savedCrossbowLevel, Dictionary<string, int> savedSkillLevels)
        {
            // Set component levels
            wallLevel = Mathf.Clamp(savedWallLevel, 1, maxWallLevel);
            gateLevel = Mathf.Clamp(savedGateLevel, 1, maxGateLevel);
            crossbowLevel = Mathf.Clamp(savedCrossbowLevel, 1, maxCrossbowLevel);
            
            // Update fortress components
            if (fortressController != null)
            {
                fortressController.UpgradeWall(wallLevel);
                fortressController.UpgradeGate(gateLevel);
                fortressController.UpgradeCrossbow(crossbowLevel);
            }
            
            // Load skill levels
            if (savedSkillLevels != null)
            {
                foreach (var kvp in savedSkillLevels)
                {
                    FortressSkill skill = fortressSkills.Find(s => s.skillId == kvp.Key);
                    
                    if (skill != null)
                    {
                        skill.currentLevel = Mathf.Clamp(kvp.Value, 0, skill.maxLevel);
                        
                        // Apply skill effect for each level
                        for (int i = 0; i < skill.currentLevel; i++)
                        {
                            ApplySkillEffect(skill);
                        }
                    }
                }
            }
            
            Debug.Log("Fortress upgrade data loaded");
        }
        #endregion
        
        #region Getters
        public int GetWallLevel() => wallLevel;
        public int GetGateLevel() => gateLevel;
        public int GetCrossbowLevel() => crossbowLevel;
        
        public int GetMaxWallLevel() => maxWallLevel;
        public int GetMaxGateLevel() => maxGateLevel;
        public int GetMaxCrossbowLevel() => maxCrossbowLevel;
        #endregion
    }
    
    [System.Serializable]
    public class FortressSkill
    {
        public string skillId;
        public string skillName;
        public string description;
        public Sprite iconSprite;
        public int maxLevel;
        public int currentLevel;
        public FortressSkillType skillType;
        public float baseValue;
        public float valuePerLevel;
        public bool unlocked;
        
        public float GetCurrentValue()
        {
            return baseValue + (currentLevel * valuePerLevel);
        }
    }
    
    public enum FortressComponent
    {
        Wall,
        Gate,
        Crossbow
    }
    
    public enum FortressSkillType
    {
        ExpGain,
        AttackSpeed,
        CritChance,
        CritDamage,
        DodgeChance,
        CritReductionChance,
        CritDamageReduction,
        DodgeIgnore,
        DamageReduction,
        DamageIncrease,
        HPRegen,
        Armor,
        ArmorPenetration,
        AttackPercent,
        DefensePercent,
        HPPercent
    }
}