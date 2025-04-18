using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AuLacThanThu.Utils;

namespace AuLacThanThu.Core
{
    /// <summary>
    /// Quản lý việc lưu và tải dữ liệu game
    /// </summary>
    public class SaveDataManager : MonoBehaviour
    {
        #region Singleton
        private static SaveDataManager _instance;
        public static SaveDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveDataManager>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("SaveDataManager");
                        _instance = obj.AddComponent<SaveDataManager>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        [Header("Save Settings")]
        [SerializeField] private string saveFileName = "aulacthan_save.json";
        [SerializeField] private bool useEncryption = true;
        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
        
        private GameSaveData currentSaveData;
        private float lastAutoSaveTime;
        
        // Reference to PlayerData
        private PlayerData playerData;
        
        // Encryption key (would be more secure with proper key management)
        private string encryptionKey = "AuLacThanThu2024";
        #endregion
        
        #region Events
        public delegate void SaveEventHandler(bool success);
        public event SaveEventHandler OnGameSaved;
        public event SaveEventHandler OnGameLoaded;
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
            
            // Initialize player data
            playerData = new PlayerData();
            currentSaveData = new GameSaveData();
        }
        
        private void Start()
        {
            // Load game on start
            LoadGameData();
        }
        
        private void Update()
        {
            // Auto-save
            if (autoSave && Time.time - lastAutoSaveTime > autoSaveInterval)
            {
                SaveGameData();
                lastAutoSaveTime = Time.time;
                Debug.Log("Auto-saved game data");
            }
        }
        
        private void OnApplicationQuit()
        {
            // Save when quitting
            SaveGameData();
            Debug.Log("Saved game data on quit");
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            // Save when game paused (app going to background)
            if (pauseStatus)
            {
                SaveGameData();
                Debug.Log("Saved game data on pause");
            }
        }
        #endregion
        
        #region Save & Load
        public bool SaveGameData()
        {
            try
            {
                // Gather all data to save
                GatherSaveData();
                
                // Serialize to JSON
                string jsonData = JsonUtility.ToJson(currentSaveData, true);
                
                // Encrypt if enabled
                if (useEncryption)
                {
                    jsonData = EncryptData(jsonData);
                }
                
                // Get save path
                string savePath = Path.Combine(Application.persistentDataPath, saveFileName);
                
                // Write to file
                File.WriteAllText(savePath, jsonData);
                
                Debug.Log($"Game data saved to {savePath}");
                OnGameSaved?.Invoke(true);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save game data: {e.Message}");
                OnGameSaved?.Invoke(false);
                return false;
            }
        }
        
        public bool LoadGameData()
        {
            try
            {
                // Get save path
                string savePath = Path.Combine(Application.persistentDataPath, saveFileName);
                
                // Check if save file exists
                if (!File.Exists(savePath))
                {
                    Debug.Log("No save file found. Starting fresh game.");
                    
                    // Initialize with default data
                    currentSaveData = new GameSaveData();
                    
                    OnGameLoaded?.Invoke(true);
                    return true;
                }
                
                // Read file
                string jsonData = File.ReadAllText(savePath);
                
                // Decrypt if enabled
                if (useEncryption)
                {
                    jsonData = DecryptData(jsonData);
                }
                
                // Deserialize
                currentSaveData = JsonUtility.FromJson<GameSaveData>(jsonData);
                
                // Apply loaded data
                ApplySaveData();
                
                Debug.Log("Game data loaded successfully");
                OnGameLoaded?.Invoke(true);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load game data: {e.Message}");
                
                // Initialize with default data
                currentSaveData = new GameSaveData();
                
                OnGameLoaded?.Invoke(false);
                return false;
            }
        }
        
        public bool DeleteSaveData()
        {
            try
            {
                // Get save path
                string savePath = Path.Combine(Application.persistentDataPath, saveFileName);
                
                // Check if file exists
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    Debug.Log("Save data deleted");
                }
                
                // Clear player prefs
                PlayerPrefs.DeleteAll();
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete save data: {e.Message}");
                return false;
            }
        }
        #endregion
        
        #region Data Management
        private void GatherSaveData()
        {
            // Player data
            currentSaveData.playerName = playerData.playerName;
            currentSaveData.playerLevel = playerData.playerLevel;
            currentSaveData.playerExp = playerData.playerExp;
            currentSaveData.playerChapter = playerData.currentChapter;
            currentSaveData.playerLevel = playerData.currentLevel;
            
            // Resources
            ResourceManager resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                currentSaveData.gold = resourceManager.GetResourceAmount(ResourceType.Gold);
                currentSaveData.diamond = resourceManager.GetResourceAmount(ResourceType.Diamond);
                currentSaveData.energy = resourceManager.GetResourceAmount(ResourceType.Energy);
                currentSaveData.iron = resourceManager.GetResourceAmount(ResourceType.Iron);
                currentSaveData.fabric = resourceManager.GetResourceAmount(ResourceType.Fabric);
                currentSaveData.divineStone = resourceManager.GetResourceAmount(ResourceType.DivineStone);
                currentSaveData.skillBook = resourceManager.GetResourceAmount(ResourceType.SkillBook);
                currentSaveData.silverLacHongMark = resourceManager.GetResourceAmount(ResourceType.SilverLacHongMark);
                currentSaveData.goldenLacHongMark = resourceManager.GetResourceAmount(ResourceType.GoldenLacHongMark);
            }
            
            // Heroes
            HeroManager heroManager = HeroManager.Instance;
            if (heroManager != null)
            {
                currentSaveData.ownedHeroes = heroManager.GetSaveData();
                
                // Hero fragments (currently stored in ResourceManager)
                // TODO: Gather hero fragments
            }
            
            // Fortress
            FortressUpgradeSystem fortressSystem = FortressUpgradeSystem.Instance;
            if (fortressSystem != null)
            {
                currentSaveData.fortressWallLevel = fortressSystem.GetWallLevel();
                currentSaveData.fortressGateLevel = fortressSystem.GetGateLevel();
                currentSaveData.fortressCrossbowLevel = fortressSystem.GetCrossbowLevel();
                currentSaveData.fortressSkillLevels = fortressSystem.GetSkillLevels();
            }
            
            // Game progress
            currentSaveData.unlockedChapters = new List<int>();
            for (int i = 1; i <= playerData.maxUnlockedChapter; i++)
            {
                currentSaveData.unlockedChapters.Add(i);
            }
            
            currentSaveData.completedLevels = playerData.completedLevels;
            currentSaveData.levelStars = playerData.levelStars;
            
            // Game settings
            currentSaveData.musicVolume = AudioManager.Instance != null ? AudioManager.Instance.GetMusicVolume() : 1f;
            currentSaveData.sfxVolume = AudioManager.Instance != null ? AudioManager.Instance.GetSFXVolume() : 1f;
            
            // Save timestamp
            currentSaveData.lastSaveTime = System.DateTime.Now.ToString();
        }
        
        private void ApplySaveData()
        {
            // Player data
            playerData.playerName = currentSaveData.playerName;
            playerData.playerLevel = currentSaveData.playerLevel;
            playerData.playerExp = currentSaveData.playerExp;
            playerData.currentChapter = currentSaveData.playerChapter;
            playerData.currentLevel = currentSaveData.playerLevel;
            
            // Determine max unlocked chapter
            playerData.maxUnlockedChapter = 1;
            if (currentSaveData.unlockedChapters != null && currentSaveData.unlockedChapters.Count > 0)
            {
                foreach (int chapter in currentSaveData.unlockedChapters)
                {
                    if (chapter > playerData.maxUnlockedChapter)
                    {
                        playerData.maxUnlockedChapter = chapter;
                    }
                }
            }
            
            // Completed levels and stars
            playerData.completedLevels = currentSaveData.completedLevels ?? new List<int>();
            playerData.levelStars = currentSaveData.levelStars ?? new Dictionary<int, int>();
            
            // Resources
            ResourceManager resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                // Set initial values in ResourceManager
                // This could be improved by having a SetResource method in ResourceManager
                PlayerPrefs.SetInt("Gold", currentSaveData.gold);
                PlayerPrefs.SetInt("Diamond", currentSaveData.diamond);
                PlayerPrefs.SetInt("Energy", currentSaveData.energy);
                PlayerPrefs.SetInt("Iron", currentSaveData.iron);
                PlayerPrefs.SetInt("Fabric", currentSaveData.fabric);
                PlayerPrefs.SetInt("DivineStone", currentSaveData.divineStone);
                PlayerPrefs.SetInt("SkillBook", currentSaveData.skillBook);
                PlayerPrefs.SetInt("SilverLacHongMark", currentSaveData.silverLacHongMark);
                PlayerPrefs.SetInt("GoldenLacHongMark", currentSaveData.goldenLacHongMark);
                PlayerPrefs.Save();
            }
            
            // Heroes
            HeroManager heroManager = HeroManager.Instance;
            if (heroManager != null)
            {
                heroManager.LoadSaveData(currentSaveData.ownedHeroes);
            }
            
            // Fortress
            FortressUpgradeSystem fortressSystem = FortressUpgradeSystem.Instance;
            if (fortressSystem != null)
            {
                fortressSystem.LoadSaveData(
                    currentSaveData.fortressWallLevel,
                    currentSaveData.fortressGateLevel,
                    currentSaveData.fortressCrossbowLevel,
                    currentSaveData.fortressSkillLevels
                );
            }
            
            // Game settings
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(currentSaveData.musicVolume);
                AudioManager.Instance.SetSFXVolume(currentSaveData.sfxVolume);
            }
        }
        #endregion
        
        #region Encryption
        private string EncryptData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return data;
            }
            
            // Simple XOR encryption (not secure for production)
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                result.Append((char)(data[i] ^ encryptionKey[i % encryptionKey.Length]));
            }
            
            // Convert to Base64 to ensure it's text-safe
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result.ToString());
            return System.Convert.ToBase64String(bytes);
        }
        
        private string DecryptData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return data;
            }
            
            // Convert from Base64
            byte[] bytes = System.Convert.FromBase64String(data);
            string base64Decoded = System.Text.Encoding.UTF8.GetString(bytes);
            
            // XOR decryption
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            for (int i = 0; i < base64Decoded.Length; i++)
            {
                result.Append((char)(base64Decoded[i] ^ encryptionKey[i % encryptionKey.Length]));
            }
            
            return result.ToString();
        }
        #endregion
        
        #region Public Methods
        public void SetPlayerName(string name)
        {
            playerData.playerName = name;
        }
        
        public string GetPlayerName()
        {
            return playerData.playerName;
        }
        
        public int GetPlayerLevel()
        {
            return playerData.playerLevel;
        }
        
        public void AddPlayerExp(int exp)
        {
            playerData.playerExp += exp;
            
            // Check for level up
            int expForNextLevel = playerData.GetExpForNextLevel();
            while (playerData.playerExp >= expForNextLevel)
            {
                playerData.playerExp -= expForNextLevel;
                playerData.playerLevel++;
                
                Debug.Log($"Player leveled up to {playerData.playerLevel}");
                
                // Recalculate exp needed for next level
                expForNextLevel = playerData.GetExpForNextLevel();
            }
        }
        
        public void UnlockChapter(int chapter)
        {
            if (chapter > playerData.maxUnlockedChapter)
            {
                playerData.maxUnlockedChapter = chapter;
            }
        }
        
        public bool IsChapterUnlocked(int chapter)
        {
            return chapter <= playerData.maxUnlockedChapter;
        }
        
        public void SetLevelCompleted(int levelId, int stars = 0)
        {
            if (!playerData.completedLevels.Contains(levelId))
            {
                playerData.completedLevels.Add(levelId);
            }
            
            // Update stars if new star count is higher
            if (!playerData.levelStars.ContainsKey(levelId) || playerData.levelStars[levelId] < stars)
            {
                playerData.levelStars[levelId] = stars;
            }
        }
        
        public bool IsLevelCompleted(int levelId)
        {
            return playerData.completedLevels.Contains(levelId);
        }
        
        public int GetLevelStars(int levelId)
        {
            if (playerData.levelStars.ContainsKey(levelId))
            {
                return playerData.levelStars[levelId];
            }
            return 0;
        }
        #endregion
    }
    
    #region Data Classes
    [System.Serializable]
    public class GameSaveData
    {
        // Player data
        public string playerName = "Archer";
        public int playerLevel = 1;
        public int playerExp = 0;
        public int playerChapter = 1;
        public int playerLevel = 1;
        
        // Resources
        public int gold = 1000;
        public int diamond = 100;
        public int energy = 100;
        public int iron = 50;
        public int fabric = 50;
        public int divineStone = 10;
        public int skillBook = 1;
        public int silverLacHongMark = 10;
        public int goldenLacHongMark = 1;
        
        // Hero data
        public List<HeroSaveData> ownedHeroes = new List<HeroSaveData>();
        public Dictionary<string, int> heroFragments = new Dictionary<string, int>();
        
        // Fortress
        public int fortressWallLevel = 1;
        public int fortressGateLevel = 1;
        public int fortressCrossbowLevel = 1;
        public Dictionary<string, int> fortressSkillLevels = new Dictionary<string, int>();
        
        // Progress
        public List<int> unlockedChapters = new List<int>() { 1 };
        public List<int> completedLevels = new List<int>();
        public Dictionary<int, int> levelStars = new Dictionary<int, int>();
        
        // Settings
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        
        // Metadata
        public string lastSaveTime;
        public string gameVersion = Application.version;
    }
    
    [System.Serializable]
    public class HeroSaveData
    {
        public string heroId;
        public int level = 1;
        public int stars = 1;
        public bool isUnlocked = false;
        public List<EquipmentSaveData> equipment = new List<EquipmentSaveData>();
        public List<SkillSaveData> skills = new List<SkillSaveData>();
    }
    
    [System.Serializable]
    public class EquipmentSaveData
    {
        public string itemId;
        public EquipmentSlotType slotType;
        public int level = 1;
        public int stars = 1;
    }
    
    [System.Serializable]
    public class SkillSaveData
    {
        public string skillId;
        public int level = 1;
        public bool isUnlocked = false;
    }
    
    public class PlayerData
    {
        public string playerName = "Archer";
        public int playerLevel = 1;
        public int playerExp = 0;
        public int currentChapter = 1;
        public int currentLevel = 1;
        public int maxUnlockedChapter = 1;
        public List<int> completedLevels = new List<int>();
        public Dictionary<int, int> levelStars = new Dictionary<int, int>();
        
        public int GetExpForNextLevel()
        {
            // Simple formula: 100 * level
            return 100 * playerLevel;
        }
    }
    #endregion
}