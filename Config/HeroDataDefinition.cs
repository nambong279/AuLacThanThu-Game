using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Utils;
using AuLacThanThu.Gameplay.Hero;

namespace AuLacThanThu.Config
{
    /// <summary>
    /// ScriptableObject chứa dữ liệu định nghĩa cho anh hùng trong game
    /// </summary>
    [CreateAssetMenu(fileName = "NewHeroData", menuName = "Âu Lạc Thần Thủ/Hero Data", order = 1)]
    public class HeroDataDefinition : ScriptableObject
    {
        [Header("Basic Information")]
        [Tooltip("ID độc nhất cho anh hùng")]
        public string heroId;
        
        [Tooltip("Tên anh hùng")]
        public string heroName;
        
        [Tooltip("Mô tả anh hùng")]
        [TextArea(3, 6)]
        public string description;
        
        [Tooltip("Hình ảnh chân dung anh hùng")]
        public Sprite portraitImage;
        
        [Tooltip("Hình ảnh biểu tượng anh hùng")]
        public Sprite iconImage;
        
        [Tooltip("Prefab của anh hùng")]
        public GameObject heroPrefab;
        
        [Header("Hero Classification")]
        [Tooltip("Độ hiếm của anh hùng")]
        public HeroRarity rarity = HeroRarity.Common;
        
        [Tooltip("Hệ nguyên tố của anh hùng")]
        public ElementType elementType = ElementType.None;
        
        [Tooltip("Vai trò chính của anh hùng")]
        public HeroRole role = HeroRole.DPS;
        
        [Header("Unlock Requirements")]
        [Tooltip("Số mảnh cần thiết để mở khóa anh hùng")]
        public int fragmentsToUnlock = 10;
        
        [Tooltip("Số mảnh nhận được khi triệu hồi trùng")]
        public int fragmentsWhenDuplicate = 5;
        
        [Header("Base Stats (Level 1, 1 Star)")]
        [Tooltip("Máu cơ bản")]
        public float baseHealth = 100f;
        
        [Tooltip("Tấn công cơ bản")]
        public float baseAttack = 10f;
        
        [Tooltip("Phòng thủ cơ bản")]
        public float baseDefense = 5f;
        
        [Tooltip("Tốc độ tấn công cơ bản (số đòn mỗi giây)")]
        public float baseAttackSpeed = 1f;
        
        [Tooltip("Tỉ lệ chí mạng cơ bản")]
        [Range(0f, 1f)]
        public float baseCritChance = 0.05f;
        
        [Tooltip("Sát thương chí mạng cơ bản")]
        public float baseCritDamage = 1.5f;
        
        [Header("Stat Growth (Per Level)")]
        [Tooltip("Tăng máu mỗi cấp")]
        public float healthPerLevel = 10f;
        
        [Tooltip("Tăng tấn công mỗi cấp")]
        public float attackPerLevel = 1f;
        
        [Tooltip("Tăng phòng thủ mỗi cấp")]
        public float defensePerLevel = 0.5f;
        
        [Header("Star Upgrade Bonuses")]
        [Tooltip("Tăng máu mỗi sao (%)")]
        [Range(0f, 1f)]
        public float healthPerStarPercent = 0.2f;
        
        [Tooltip("Tăng tấn công mỗi sao (%)")]
        [Range(0f, 1f)]
        public float attackPerStarPercent = 0.2f;
        
        [Tooltip("Tăng phòng thủ mỗi sao (%)")]
        [Range(0f, 1f)]
        public float defensePerStarPercent = 0.15f;
        
        [Header("Skills")]
        [Tooltip("Kỹ năng chủ động")]
        public SkillDefinition activeSkill;
        
        [Tooltip("Kỹ năng bị động 1 (mở khóa ở 3 sao)")]
        public SkillDefinition passiveSkill1;
        
        [Tooltip("Kỹ năng bị động 2 (mở khóa ở 5 sao)")]
        public SkillDefinition passiveSkill2;
        
        [Header("Hero Evolution")]
        [Tooltip("Giai đoạn tiến hóa của anh hùng")]
        public List<EvolutionStage> evolutionStages = new List<EvolutionStage>();
        
        [Header("Lore & Background")]
        [Tooltip("Thời kỳ lịch sử của anh hùng")]
        public string historicalPeriod;
        
        [Tooltip("Cốt truyện của anh hùng")]
        [TextArea(5, 10)]
        public string lore;
        
        [Tooltip("Câu nói nổi tiếng")]
        [TextArea(1, 3)]
        public string famousQuote;
        
        [Header("Audio")]
        [Tooltip("Âm thanh triệu hồi")]
        public AudioClip summonVoice;
        
        [Tooltip("Âm thanh sử dụng kỹ năng")]
        public AudioClip skillVoice;
        
        [Tooltip("Âm thanh thắng trận")]
        public AudioClip victoryVoice;
        
        #region Helper Methods
        /// <summary>
        /// Tính máu của anh hùng dựa vào cấp độ và số sao
        /// </summary>
        public float CalculateHealth(int level, int stars)
        {
            float baseValue = baseHealth + (healthPerLevel * (level - 1));
            float starMultiplier = 1f + ((stars - 1) * healthPerStarPercent);
            
            return baseValue * starMultiplier;
        }
        
        /// <summary>
        /// Tính tấn công của anh hùng dựa vào cấp độ và số sao
        /// </summary>
        public float CalculateAttack(int level, int stars)
        {
            float baseValue = baseAttack + (attackPerLevel * (level - 1));
            float starMultiplier = 1f + ((stars - 1) * attackPerStarPercent);
            
            return baseValue * starMultiplier;
        }
        
        /// <summary>
        /// Tính phòng thủ của anh hùng dựa vào cấp độ và số sao
        /// </summary>
        public float CalculateDefense(int level, int stars)
        {
            float baseValue = baseDefense + (defensePerLevel * (level - 1));
            float starMultiplier = 1f + ((stars - 1) * defensePerStarPercent);
            
            return baseValue * starMultiplier;
        }
        
        /// <summary>
        /// Lấy thông tin giai đoạn tiến hóa dựa vào số sao
        /// </summary>
        public EvolutionStage GetEvolutionStage(int stars)
        {
            if (evolutionStages == null || evolutionStages.Count == 0)
                return null;
                
            // Tìm giai đoạn tiến hóa phù hợp với số sao
            EvolutionStage matchingStage = null;
            
            foreach (var stage in evolutionStages)
            {
                if (stars >= stage.requiredStars)
                {
                    if (matchingStage == null || stage.requiredStars > matchingStage.requiredStars)
                    {
                        matchingStage = stage;
                    }
                }
            }
            
            return matchingStage;
        }
        
        /// <summary>
        /// Lấy prefab của anh hùng dựa vào cấp độ và số sao (sử dụng giai đoạn tiến hóa phù hợp)
        /// </summary>
        public GameObject GetHeroPrefab(int stars)
        {
            EvolutionStage stage = GetEvolutionStage(stars);
            
            if (stage != null && stage.heroPrefab != null)
            {
                return stage.heroPrefab;
            }
            
            return heroPrefab;
        }
        #endregion
        
        #region Validation
        private void OnValidate()
        {
            // Validation checks
            if (string.IsNullOrEmpty(heroId))
            {
                Debug.LogWarning($"Hero {name} is missing an ID");
            }
            
            if (string.IsNullOrEmpty(heroName))
            {
                Debug.LogWarning($"Hero {name} is missing a name");
            }
            
            if (heroPrefab == null)
            {
                Debug.LogWarning($"Hero {name} is missing a prefab");
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Định nghĩa kỹ năng của anh hùng
    /// </summary>
    [System.Serializable]
    public class SkillDefinition
    {
        [Tooltip("ID độc nhất cho kỹ năng")]
        public string skillId;
        
        [Tooltip("Tên kỹ năng")]
        public string skillName;
        
        [Tooltip("Mô tả kỹ năng")]
        [TextArea(2, 5)]
        public string description;
        
        [Tooltip("Biểu tượng kỹ năng")]
        public Sprite skillIcon;
        
        [Tooltip("Hệ nguyên tố của kỹ năng")]
        public ElementType elementType = ElementType.None;
        
        [Tooltip("Loại kỹ năng")]
        public SkillType skillType = SkillType.Active;
        
        [Tooltip("Thời gian hồi (giây)")]
        public float cooldown = 10f;
        
        [Tooltip("Sát thương cơ bản")]
        public float baseDamage = 0f;
        
        [Tooltip("Hồi máu cơ bản")]
        public float baseHealing = 0f;
        
        [Tooltip("Thời gian kéo dài hiệu ứng (giây)")]
        public float effectDuration = 0f;
        
        [Tooltip("Tỉ lệ tác động hiệu ứng")]
        [Range(0f, 1f)]
        public float effectChance = 1f;
        
        [Tooltip("Loại mục tiêu")]
        public TargetType targetType = TargetType.Enemy;
        
        [Tooltip("Số lượng mục tiêu")]
        public int targetCount = 1;
        
        [Tooltip("Phạm vi ảnh hưởng")]
        public float range = 0f;
        
        [Tooltip("Prefab hiệu ứng kỹ năng")]
        public GameObject effectPrefab;
        
        [Tooltip("Mô tả nâng cấp kỹ năng theo cấp")]
        [TextArea(1, 3)]
        public string upgradeDescription;
    }
    
    /// <summary>
    /// Định nghĩa giai đoạn tiến hóa của anh hùng
    /// </summary>
    [System.Serializable]
    public class EvolutionStage
    {
        [Tooltip("Tên giai đoạn tiến hóa")]
        public string stageName;
        
        [Tooltip("Số sao yêu cầu để tiến hóa")]
        [Range(1, 10)]
        public int requiredStars = 3;
        
        [Tooltip("Prefab của anh hùng ở giai đoạn này")]
        public GameObject heroPrefab;
        
        [Tooltip("Hình ảnh chân dung ở giai đoạn này")]
        public Sprite portraitImage;
        
        [Tooltip("Hiệu ứng thêm vào cho kỹ năng")]
        [TextArea(1, 3)]
        public string skillEnhancements;
    }
    
    /// <summary>
    /// Vai trò của anh hùng
    /// </summary>
    public enum HeroRole
    {
        DPS,        // Tấn công
        Tank,       // Phòng thủ
        Support,    // Hỗ trợ
        Control     // Kiểm soát
    }
}