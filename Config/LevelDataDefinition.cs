using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AuLacThanThu.Utils;
using AuLacThanThu.Gameplay.Enemy;

namespace AuLacThanThu.Config
{
    /// <summary>
    /// ScriptableObject chứa dữ liệu định nghĩa cho level trong game
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Âu Lạc Thần Thủ/Level Data", order = 3)]
    public class LevelDataDefinition : ScriptableObject
    {
        [Header("Basic Information")]
        [Tooltip("ID độc nhất cho level")]
        public string levelId;
        
        [Tooltip("Tên level")]
        public string levelName;
        
        [Tooltip("Chapter mà level thuộc về")]
        public int chapterId;
        
        [Tooltip("Vị trí trong chapter (số thứ tự)")]
        public int levelIndex;
        
        [Tooltip("Mô tả ngắn về level")]
        [TextArea(2, 5)]
        public string description;
        
        [Header("Level Settings")]
        [Tooltip("Độ khó của level")]
        public DifficultyLevel difficulty = DifficultyLevel.Normal;
        
        [Tooltip("Số lượng wave")]
        [Range(1, 50)]
        public int waveCount = 20;
        
        [Tooltip("Thời gian giữa các wave (giây)")]
        public float timeBetweenWaves = 5f;
        
        [Tooltip("Chỉ số tăng độ khó của wave")]
        [Range(1f, 2f)]
        public float waveDifficultyMultiplier = 1.1f;
        
        [Tooltip("Chi phí năng lượng để chơi level")]
        [Range(1, 20)]
        public int energyCost = 5;
        
        [Header("Enemies")]
        [Tooltip("Danh sách wave được định nghĩa sẵn (nếu có)")]
        public List<WaveDefinition> predefinedWaves = new List<WaveDefinition>();
        
        [Tooltip("Danh sách kẻ địch có thể xuất hiện trong level (dùng khi tạo wave tự động)")]
        public List<EnemyEntry> possibleEnemies = new List<EnemyEntry>();
        
        [Tooltip("Danh sách boss của level")]
        public List<string> bossList = new List<string>();
        
        [Tooltip("Wave mà boss xuất hiện (thường là wave cuối)")]
        public int bossWave = 20;
        
        [Header("Level Conditions")]
        [Tooltip("Loại điều kiện chiến thắng")]
        public WinConditionType winCondition = WinConditionType.DefeatAllEnemies;
        
        [Tooltip("Giá trị điều kiện thắng (nếu cần)")]
        public int winConditionValue = 0;
        
        [Tooltip("Thời gian giới hạn của level (0 = không giới hạn)")]
        public float timeLimit = 0f;
        
        [Header("Rewards")]
        [Tooltip("Phần thưởng vàng cơ bản")]
        public int baseGoldReward = 100;
        
        [Tooltip("Phần thưởng EXP cơ bản")]
        public int baseExpReward = 50;
        
        [Tooltip("Phần thưởng thêm cho mỗi sao")]
        public float rewardPerStar = 0.2f;
        
        [Tooltip("Danh sách phần thưởng lần đầu hoàn thành")]
        public List<LevelReward> firstTimeRewards = new List<LevelReward>();
        
        [Tooltip("Danh sách phần thưởng mỗi lần hoàn thành")]
        public List<LevelReward> repeatRewards = new List<LevelReward>();
        
        [Header("Star Requirements")]
        [Tooltip("Phần trăm máu thành trì để đạt 3 sao")]
        [Range(0f, 1f)]
        public float threeStarHealthPercent = 0.75f;
        
        [Tooltip("Phần trăm máu thành trì để đạt 2 sao")]
        [Range(0f, 1f)]
        public float twoStarHealthPercent = 0.4f;
        
        [Tooltip("Thời gian để đạt thêm sao (0 = không tính thời gian)")]
        public float timeForExtraStar = 0f;
        
        [Header("Environment")]
        [Tooltip("Tên scene của level")]
        public string sceneName = "Battle";
        
        [Tooltip("Prefab cho background và environment")]
        public GameObject environmentPrefab;
        
        [Tooltip("Nhạc nền cho level")]
        public AudioClip backgroundMusic;
        
        [Tooltip("Hiệu ứng âm thanh nền")]
        public AudioClip ambientSound;
        
        [Header("Story")]
        [Tooltip("Cốt truyện của level")]
        [TextArea(3, 8)]
        public string storyText;
        
        [Tooltip("Dialog trước khi bắt đầu level")]
        public List<DialogEntry> preGameDialog = new List<DialogEntry>();
        
        [Tooltip("Dialog sau khi hoàn thành level")]
        public List<DialogEntry> postGameDialog = new List<DialogEntry>();
        
        #region Helper Methods
        /// <summary>
        /// Lấy định nghĩa wave dựa vào số thứ tự
        /// </summary>
        public WaveDefinition GetWaveDefinition(int waveIndex)
        {
            if (predefinedWaves != null && waveIndex >= 0 && waveIndex < predefinedWaves.Count)
            {
                return predefinedWaves[waveIndex];
            }
            
            return null;
        }
        
        /// <summary>
        /// Tính toán số vàng thưởng dựa vào số sao đạt được
        /// </summary>
        public int CalculateGoldReward(int stars)
        {
            // Tính vàng cơ bản + tăng theo số sao
            return Mathf.RoundToInt(baseGoldReward * (1f + (stars - 1) * rewardPerStar));
        }
        
        /// <summary>
        /// Tính toán số EXP thưởng dựa vào số sao đạt được
        /// </summary>
        public int CalculateExpReward(int stars)
        {
            // Tính EXP cơ bản + tăng theo số sao
            return Mathf.RoundToInt(baseExpReward * (1f + (stars - 1) * rewardPerStar));
        }
        
        /// <summary>
        /// Kiểm tra điều kiện 3 sao
        /// </summary>
        public bool CheckThreeStarCondition(float fortressHealthPercent, float completionTime)
        {
            bool healthCondition = fortressHealthPercent >= threeStarHealthPercent;
            bool timeCondition = timeForExtraStar <= 0 || completionTime <= timeForExtraStar;
            
            return healthCondition && timeCondition;
        }
        
        /// <summary>
        /// Kiểm tra điều kiện 2 sao
        /// </summary>
        public bool CheckTwoStarCondition(float fortressHealthPercent)
        {
            return fortressHealthPercent >= twoStarHealthPercent;
        }
        
        /// <summary>
        /// Tạo wave tự động nếu không có predefined waves
        /// </summary>
        public WaveDefinition GenerateWave(int waveIndex, int totalWaves)
        {
            // Kiểm tra có phải wave boss không
            bool isBossWave = waveIndex == bossWave - 1 || waveIndex == totalWaves - 1;
            
            WaveDefinition waveDefinition = new WaveDefinition();
            waveDefinition.waveIndex = waveIndex;
            waveDefinition.enemies = new List<EnemySpawnDefinition>();
            
            if (isBossWave && bossList.Count > 0)
            {
                // Tạo boss wave
                string bossId = bossList[Random.Range(0, bossList.Count)];
                
                EnemySpawnDefinition bossSpawn = new EnemySpawnDefinition
                {
                    enemyId = bossId,
                    spawnDelay = 1f,
                    spawnPoint = 2, // Giữa màn hình
                    count = 1
                };
                
                waveDefinition.enemies.Add(bossSpawn);
                
                // Thêm vài quái thường đi kèm
                GenerateRegularEnemies(waveDefinition, waveIndex, totalWaves, 3);
            }
            else
            {
                // Tạo wave thường
                GenerateRegularEnemies(waveDefinition, waveIndex, totalWaves, 0);
            }
            
            return waveDefinition;
        }
        
        /// <summary>
        /// Tạo kẻ địch thường cho wave
        /// </summary>
        private void GenerateRegularEnemies(WaveDefinition wave, int waveIndex, int totalWaves, int baseCount)
        {
            if (possibleEnemies == null || possibleEnemies.Count == 0)
                return;
                
            // Tính số lượng kẻ địch dựa vào wave và độ khó
            float progress = (float)waveIndex / totalWaves;
            int difficultyFactor = (int)difficulty;
            
            int enemyCount = baseCount + Mathf.RoundToInt(5 + progress * 10 * difficultyFactor * waveDifficultyMultiplier);
            
            // Chia nhỏ thành nhiều đợt spawn
            int maxSpawnGroups = 3;
            int spawnGroups = Mathf.Min(maxSpawnGroups, enemyCount);
            int enemiesPerGroup = enemyCount / spawnGroups;
            
            for (int i = 0; i < spawnGroups; i++)
            {
                int count = (i == spawnGroups - 1) ? 
                    enemiesPerGroup + (enemyCount % spawnGroups) : enemiesPerGroup;
                    
                if (count <= 0) continue;
                
                // Chọn ngẫu nhiên loại kẻ địch từ possibleEnemies
                EnemyEntry enemyEntry = GetRandomEnemy(progress);
                
                EnemySpawnDefinition spawnDef = new EnemySpawnDefinition
                {
                    enemyId = enemyEntry.enemyId,
                    spawnDelay = i * 2f, // Mỗi nhóm cách nhau 2 giây
                    spawnPoint = -1, // Ngẫu nhiên
                    count = count
                };
                
                wave.enemies.Add(spawnDef);
            }
        }
        
        /// <summary>
        /// Chọn ngẫu nhiên kẻ địch dựa vào tiến trình màn chơi
        /// </summary>
        private EnemyEntry GetRandomEnemy(float progress)
        {
            // Lọc các kẻ địch phù hợp với tiến trình hiện tại
            List<EnemyEntry> validEnemies = new List<EnemyEntry>();
            
            foreach (var enemy in possibleEnemies)
            {
                if (progress >= enemy.minWaveProgress && progress <= enemy.maxWaveProgress)
                {
                    validEnemies.Add(enemy);
                }
            }
            
            // Nếu không có kẻ địch phù hợp, lấy tất cả
            if (validEnemies.Count == 0)
            {
                validEnemies = new List<EnemyEntry>(possibleEnemies);
            }
            
            // Chọn ngẫu nhiên
            return validEnemies[Random.Range(0, validEnemies.Count)];
        }
        #endregion
        
        #region Validation
        private void OnValidate()
        {
            // Validation checks
            if (string.IsNullOrEmpty(levelId))
            {
                Debug.LogWarning($"Level {name} is missing an ID");
            }
            
            if (string.IsNullOrEmpty(levelName))
            {
                Debug.LogWarning($"Level {name} is missing a name");
            }
            
            if (twoStarHealthPercent > threeStarHealthPercent)
            {
                Debug.LogWarning($"Level {name}: Two star health percent should be lower than three star");
                twoStarHealthPercent = threeStarHealthPercent * 0.6f;
            }
            
            // Ensure boss wave is within range
            if (bossWave > waveCount)
            {
                bossWave = waveCount;
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Định nghĩa wave kẻ địch
    /// </summary>
    [System.Serializable]
    public class WaveDefinition
    {
        [Tooltip("Số thứ tự của wave")]
        public int waveIndex;
        
        [Tooltip("Danh sách kẻ địch spawn trong wave")]
        public List<EnemySpawnDefinition> enemies = new List<EnemySpawnDefinition>();
        
        [Tooltip("Thời gian giữa các wave (ghi đè cài đặt level nếu > 0)")]
        public float timeBetweenWaves = 0f;
        
        [Tooltip("Điều kiện hoàn thành wave (ghi đè cài đặt mặc định nếu được thiết lập)")]
        public WaveCompletionCondition completionCondition = WaveCompletionCondition.Default;
    }
    
    /// <summary>
    /// Định nghĩa spawn kẻ địch
    /// </summary>
    [System.Serializable]
    public class EnemySpawnDefinition
    {
        [Tooltip("ID của kẻ địch")]
        public string enemyId;
        
        [Tooltip("Số lượng kẻ địch")]
        public int count = 1;
        
        [Tooltip("Vị trí spawn (-1 = ngẫu nhiên, 0-4 = các điểm spawn cụ thể)")]
        [Range(-1, 4)]
        public int spawnPoint = -1;
        
        [Tooltip("Thời gian chờ trước khi spawn (giây)")]
        public float spawnDelay = 0f;
        
        [Tooltip("Thời gian giữa các lần spawn (giây)")]
        public float spawnInterval = 0.5f;
        
        [Tooltip("Hướng di chuyển ghi đè (nếu cần)")]
        public Vector2 overrideDirection = Vector2.zero;
        
        [Tooltip("Cấp độ kẻ địch ghi đè (0 = mặc định theo level)")]
        public int overrideLevel = 0;
    }
    
    /// <summary>
    /// Định nghĩa loại kẻ địch có thể xuất hiện trong level
    /// </summary>
    [System.Serializable]
    public class EnemyEntry
    {
        [Tooltip("ID của kẻ địch")]
        public string enemyId;
        
        [Tooltip("Phần trăm wave tối thiểu để xuất hiện (0-1)")]
        [Range(0f, 1f)]
        public float minWaveProgress = 0f;
        
        [Tooltip("Phần trăm wave tối đa để xuất hiện (0-1)")]
        [Range(0f, 1f)]
        public float maxWaveProgress = 1f;
        
        [Tooltip("Tỷ lệ xuất hiện")]
        [Range(0f, 10f)]
        public float spawnWeight = 1f;
    }
    
    /// <summary>
    /// Phần thưởng cho level
    /// </summary>
    [System.Serializable]
    public class LevelReward
    {
        [Tooltip("Loại phần thưởng")]
        public RewardType rewardType = RewardType.Resource;
        
        [Tooltip("ID vật phẩm/tài nguyên")]
        public string itemId;
        
        [Tooltip("Số lượng")]
        public int amount = 1;
        
        [Tooltip("Tỷ lệ nhận được (1 = 100%)")]
        [Range(0f, 1f)]
        public float chance = 1f;
    }
    
    /// <summary>
    /// Entry cho dialog cốt truyện
    /// </summary>
    [System.Serializable]
    public class DialogEntry
    {
        [Tooltip("Tên nhân vật nói")]
        public string speakerName;
        
        [Tooltip("Hình ảnh nhân vật")]
        public Sprite speakerImage;
        
        [Tooltip("Nội dung lời thoại")]
        [TextArea(2, 5)]
        public string dialogText;
        
        [Tooltip("Hiệu ứng âm thanh khi hiện dialog (nếu có)")]
        public AudioClip soundEffect;
    }
    
    /// <summary>
    /// Độ khó của level
    /// </summary>
    public enum DifficultyLevel
    {
        Easy = 1,
        Normal = 2,
        Hard = 3,
        VeryHard = 4,
        Extreme = 5
    }
    
    /// <summary>
    /// Loại điều kiện chiến thắng
    /// </summary>
    public enum WinConditionType
    {
        DefeatAllEnemies,     // Tiêu diệt tất cả kẻ địch
        SurviveTime,          // Sống sót trong thời gian quy định
        DefeatBoss,           // Tiêu diệt boss
        DefeatCount,          // Tiêu diệt số lượng kẻ địch nhất định
        ProtectTarget         // Bảo vệ mục tiêu
    }
    
    /// <summary>
    /// Điều kiện hoàn thành wave
    /// </summary>
    public enum WaveCompletionCondition
    {
        Default,              // Tiêu diệt tất cả kẻ địch
        Timer,                // Hết thời gian
        KillCount,            // Tiêu diệt đủ số lượng
        KillPercentage,       // Tiêu diệt % số lượng
        Special               // Điều kiện đặc biệt
    }
    
    /// <summary>
    /// Loại phần thưởng
    /// </summary>
    public enum RewardType
    {
        Resource,             // Tài nguyên (vàng, kim cương, năng lượng)
        Material,             // Nguyên liệu (sắt, vải, đá thần)
        HeroFragment,         // Mảnh anh hùng
        Equipment,            // Trang bị
        SpecialItem           // Vật phẩm đặc biệt
    }
}