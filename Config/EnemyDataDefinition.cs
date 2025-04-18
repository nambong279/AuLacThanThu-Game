using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Utils;
using AuLacThanThu.Gameplay.Enemy;

namespace AuLacThanThu.Config
{
    /// <summary>
    /// ScriptableObject chứa dữ liệu định nghĩa cho kẻ địch trong game
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Âu Lạc Thần Thủ/Enemy Data", order = 2)]
    public class EnemyDataDefinition : ScriptableObject
    {
        [Header("Basic Information")]
        [Tooltip("ID độc nhất cho kẻ địch")]
        public string enemyId;
        
        [Tooltip("Tên kẻ địch")]
        public string enemyName;
        
        [Tooltip("Mô tả kẻ địch")]
        [TextArea(2, 5)]
        public string description;
        
        [Tooltip("Prefab của kẻ địch")]
        public GameObject enemyPrefab;
        
        [Tooltip("Hình ảnh biểu tượng của kẻ địch")]
        public Sprite iconImage;
        
        [Header("Enemy Classification")]
        [Tooltip("Loại kẻ địch")]
        public EnemyType enemyType = EnemyType.Regular;
        
        [Tooltip("Hệ nguyên tố chính")]
        public ElementType primaryElement = ElementType.None;
        
        [Tooltip("Hệ nguyên tố phụ (nếu có)")]
        public ElementType secondaryElement = ElementType.None;
        
        [Tooltip("Thời kỳ xuất hiện của kẻ địch")]
        public string historicalPeriod;
        
        [Header("Base Stats")]
        [Tooltip("Máu cơ bản")]
        public float baseHealth = 100f;
        
        [Tooltip("Tấn công cơ bản")]
        public float baseAttack = 10f;
        
        [Tooltip("Phòng thủ cơ bản")]
        public float baseDefense = 5f;
        
        [Tooltip("Tốc độ di chuyển")]
        public float moveSpeed = 2f;
        
        [Tooltip("Tốc độ tấn công (số đòn mỗi giây)")]
        public float attackSpeed = 1f;
        
        [Tooltip("Tầm tấn công")]
        public float attackRange = 1f;
        
        [Tooltip("Tỷ lệ chí mạng")]
        [Range(0f, 1f)]
        public float critChance = 0.05f;
        
        [Tooltip("Sát thương chí mạng")]
        public float critDamage = 1.5f;
        
        [Tooltip("Tỷ lệ né tránh")]
        [Range(0f, 1f)]
        public float dodgeChance = 0.05f;
        
        [Header("Advanced Stats")]
        [Tooltip("Hộ giáp (giảm sát thương theo công thức)")]
        public float armor = 0f;
        
        [Tooltip("Kháng hệ nguyên tố (0-1 scale)")]
        [Range(0f, 1f)]
        public float elementResistance = 0f;
        
        [Tooltip("Kháng hiệu ứng trạng thái (0-1 scale)")]
        [Range(0f, 1f)]
        public float statusResistance = 0f;
        
        [Header("Scaling (Per Level)")]
        [Tooltip("Hệ số tăng máu theo cấp")]
        public float healthScaling = 1.1f;
        
        [Tooltip("Hệ số tăng tấn công theo cấp")]
        public float attackScaling = 1.1f;
        
        [Tooltip("Hệ số tăng phòng thủ theo cấp")]
        public float defenseScaling = 1.05f;
        
        [Header("Abilities")]
        [Tooltip("Kỹ năng của kẻ địch")]
        public List<EnemyAbility> abilities = new List<EnemyAbility>();
        
        [Header("Behavior")]
        [Tooltip("Loại hành vi AI của kẻ địch")]
        public EnemyBehaviorType behaviorType = EnemyBehaviorType.Basic;
        
        [Tooltip("Khoảng cách duy trì với mục tiêu (chỉ dành cho tấn công từ xa)")]
        public float preferredDistance = 0f;
        
        [Tooltip("Khoảng cách phát hiện mục tiêu")]
        public float detectionRange = 10f;
        
        [Tooltip("Mục tiêu ưu tiên")]
        public TargetPriority targetPriority = TargetPriority.NearestHero;
        
        [Header("Death & Rewards")]
        [Tooltip("Hiệu ứng khi chết")]
        public GameObject deathEffectPrefab;
        
        [Tooltip("Âm thanh khi chết")]
        public AudioClip deathSound;
        
        [Tooltip("Kinh nghiệm nhận được khi tiêu diệt")]
        public int expReward = 5;
        
        [Tooltip("Vàng nhận được khi tiêu diệt")]
        public int goldReward = 10;
        
        [Tooltip("Tỷ lệ rơi vật phẩm")]
        [Range(0f, 1f)]
        public float dropChance = 0.1f;
        
        [Tooltip("Vật phẩm có thể rơi")]
        public List<EnemyDrop> possibleDrops = new List<EnemyDrop>();
        
        [Header("Special Properties")]
        [Tooltip("Có phải là thủ lĩnh đợt tấn công không")]
        public bool isWaveLeader = false;
        
        [Tooltip("Có tăng sức mạnh cho đồng đội xung quanh không")]
        public bool hasBuff = false;
        
        [Tooltip("Bán kính ảnh hưởng buff")]
        public float buffRadius = 3f;
        
        [Tooltip("Có khả năng hồi sinh không")]
        public bool canRevive = false;
        
        [Tooltip("Số lần có thể hồi sinh")]
        public int reviveCount = 0;
        
        [Tooltip("Phần trăm máu khi hồi sinh")]
        [Range(0f, 1f)]
        public float reviveHealthPercent = 0.5f;
        
        [Header("Boss Specific")]
        [Tooltip("Số giai đoạn của boss")]
        [Range(1, 3)]
        public int bossPhases = 1;
        
        [Tooltip("Phần trăm máu để chuyển giai đoạn")]
        [Range(0f, 1f)]
        public float phaseTransitionThreshold = 0.5f;
        
        [Tooltip("Kỹ năng đặc biệt khi chuyển giai đoạn")]
        public EnemyAbility phaseTransitionAbility;
        
        [Tooltip("Trạng thái miễn nhiễm trong khi chuyển giai đoạn")]
        public bool immuneDuringTransition = true;
        
        #region Helper Methods
        /// <summary>
        /// Tính máu của kẻ địch dựa vào cấp độ
        /// </summary>
        public float CalculateHealth(int level)
        {
            return baseHealth * Mathf.Pow(healthScaling, level - 1);
        }
        
        /// <summary>
        /// Tính tấn công của kẻ địch dựa vào cấp độ
        /// </summary>
        public float CalculateAttack(int level)
        {
            return baseAttack * Mathf.Pow(attackScaling, level - 1);
        }
        
        /// <summary>
        /// Tính phòng thủ của kẻ địch dựa vào cấp độ
        /// </summary>
        public float CalculateDefense(int level)
        {
            return baseDefense * Mathf.Pow(defenseScaling, level - 1);
        }
        
        /// <summary>
        /// Kiểm tra xem kẻ địch có kháng hiệu ứng cụ thể không
        /// </summary>
        public bool IsResistantToStatus(StatusEffectType statusType)
        {
            float resistChance = statusResistance;
            
            // Tăng/giảm kháng theo hệ tương khắc
            switch (statusType)
            {
                case StatusEffectType.Burn:
                    if (primaryElement == ElementType.Fire || secondaryElement == ElementType.Fire)
                        resistChance += 0.5f;
                    else if (primaryElement == ElementType.Water || secondaryElement == ElementType.Water)
                        resistChance -= 0.2f;
                    break;
                    
                case StatusEffectType.Freeze:
                    if (primaryElement == ElementType.Water || secondaryElement == ElementType.Water)
                        resistChance += 0.5f;
                    else if (primaryElement == ElementType.Fire || secondaryElement == ElementType.Fire)
                        resistChance -= 0.2f;
                    break;
                    
                case StatusEffectType.Poison:
                    if (primaryElement == ElementType.Earth || secondaryElement == ElementType.Earth)
                        resistChance += 0.5f;
                    else if (primaryElement == ElementType.Lightning || secondaryElement == ElementType.Lightning)
                        resistChance -= 0.2f;
                    break;
                    
                case StatusEffectType.Stun:
                    if (primaryElement == ElementType.Lightning || secondaryElement == ElementType.Lightning)
                        resistChance += 0.5f;
                    else if (primaryElement == ElementType.Earth || secondaryElement == ElementType.Earth)
                        resistChance -= 0.2f;
                    break;
            }
            
            return Random.value < resistChance;
        }
        
        /// <summary>
        /// Lấy kỹ năng ngẫu nhiên dựa vào tỷ lệ sử dụng
        /// </summary>
        public EnemyAbility GetRandomAbility()
        {
            if (abilities == null || abilities.Count == 0)
                return null;
                
            // Tính tổng tỷ lệ
            float totalWeight = 0f;
            foreach (var ability in abilities)
            {
                totalWeight += ability.useWeight;
            }
            
            // Chọn ngẫu nhiên dựa vào tỷ lệ
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var ability in abilities)
            {
                currentWeight += ability.useWeight;
                if (randomValue <= currentWeight)
                {
                    return ability;
                }
            }
            
            // Mặc định trả về kỹ năng đầu tiên
            return abilities.Count > 0 ? abilities[0] : null;
        }
        #endregion
        
        #region Validation
        private void OnValidate()
        {
            // Validation checks
            if (string.IsNullOrEmpty(enemyId))
            {
                Debug.LogWarning($"Enemy {name} is missing an ID");
            }
            
            if (string.IsNullOrEmpty(enemyName))
            {
                Debug.LogWarning($"Enemy {name} is missing a name");
            }
            
            if (enemyPrefab == null)
            {
                Debug.LogWarning($"Enemy {name} is missing a prefab");
            }
            
            // Ensure boss has at least one ability
            if (enemyType == EnemyType.Boss && (abilities == null || abilities.Count == 0))
            {
                Debug.LogWarning($"Boss enemy {name} should have at least one ability");
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Định nghĩa kỹ năng của kẻ địch
    /// </summary>
    [System.Serializable]
    public class EnemyAbility
    {
        [Tooltip("Tên kỹ năng")]
        public string abilityName;
        
        [Tooltip("Mô tả kỹ năng")]
        [TextArea(1, 3)]
        public string description;
        
        [Tooltip("Loại kỹ năng")]
        public EnemyAbilityType abilityType = EnemyAbilityType.Attack;
        
        [Tooltip("Hệ nguyên tố của kỹ năng")]
        public ElementType elementType = ElementType.None;
        
        [Tooltip("Thời gian hồi chiêu (giây)")]
        public float cooldown = 10f;
        
        [Tooltip("Phần trăm sát thương (so với sát thương cơ bản)")]
        [Range(0f, 5f)]
        public float damagePercent = 1f;
        
        [Tooltip("Bán kính ảnh hưởng")]
        public float radius = 0f;
        
        [Tooltip("Thời gian hiệu ứng kéo dài (giây)")]
        public float effectDuration = 3f;
        
        [Tooltip("Tỷ lệ kích hoạt hiệu ứng")]
        [Range(0f, 1f)]
        public float effectChance = 0.5f;
        
        [Tooltip("Prefab hiệu ứng của kỹ năng")]
        public GameObject effectPrefab;
        
        [Tooltip("Tỷ lệ sử dụng kỹ năng (dùng cho lựa chọn ngẫu nhiên)")]
        [Range(0f, 10f)]
        public float useWeight = 1f;
        
        [Tooltip("Tỷ lệ máu ít hơn để có thể dùng kỹ năng (0 = luôn dùng được)")]
        [Range(0f, 1f)]
        public float healthThreshold = 0f;
        
        [Tooltip("Animation trigger khi sử dụng kỹ năng")]
        public string animationTrigger = "Attack";
    }
    
    /// <summary>
    /// Định nghĩa vật phẩm rơi từ kẻ địch
    /// </summary>
    [System.Serializable]
    public class EnemyDrop
    {
        [Tooltip("Loại vật phẩm")]
        public DropType dropType = DropType.Resource;
        
        [Tooltip("ID vật phẩm/tài nguyên")]
        public string itemId;
        
        [Tooltip("Số lượng")]
        public int minAmount = 1;
        
        [Tooltip("Số lượng tối đa")]
        public int maxAmount = 1;
        
        [Tooltip("Tỷ lệ rơi cụ thể cho vật phẩm này")]
        [Range(0f, 1f)]
        public float dropChance = 0.1f;
    }
    
    /// <summary>
    /// Loại hành vi AI của kẻ địch
    /// </summary>
    public enum EnemyBehaviorType
    {
        Basic,          // Di chuyển thẳng về phía thành
        Ranged,         // Tấn công từ xa và giữ khoảng cách
        Flanker,        // Cố gắng di chuyển vòng qua
        Tank,           // Chậm nhưng trâu bò
        Assassin,       // Nhắm vào anh hùng yếu nhất
        Support,        // Buff đồng đội
        Summoner,       // Gọi quái vật phụ
        Boss            // Hành vi phức tạp với các giai đoạn
    }
    
    /// <summary>
    /// Loại kỹ năng của kẻ địch
    /// </summary>
    public enum EnemyAbilityType
    {
        Attack,         // Tấn công thường
        AreaAttack,     // Tấn công diện rộng
        StatusEffect,   // Gây hiệu ứng trạng thái
        Heal,           // Hồi máu cho bản thân hoặc đồng đội
        Buff,           // Tăng sức mạnh cho đồng đội
        Summon,         // Triệu hồi quái vật phụ
        Movement,       // Di chuyển đặc biệt (nhảy, teleport)
        Special         // Kỹ năng đặc biệt (thường dành cho boss)
    }
    
    /// <summary>
    /// Ưu tiên mục tiêu của kẻ địch
    /// </summary>
    public enum TargetPriority
    {
        NearestHero,    // Hero gần nhất
        WeakestHero,    // Hero yếu nhất (ít máu nhất)
        StrongestHero,  // Hero mạnh nhất (nhiều máu nhất)
        Fortress,       // Luôn nhắm vào thành
        Random          // Ngẫu nhiên
    }
    
    /// <summary>
    /// Loại vật phẩm rơi
    /// </summary>
    public enum DropType
    {
        Resource,       // Tài nguyên (vàng, kim cương)
        Material,       // Nguyên liệu (sắt, vải)
        HeroFragment,   // Mảnh anh hùng
        Equipment,      // Trang bị
        Special         // Vật phẩm đặc biệt
    }
}