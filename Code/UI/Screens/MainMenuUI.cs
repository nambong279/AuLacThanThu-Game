using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AuLacThanThu.Core;

namespace AuLacThanThu.UI
{
    /// <summary>
    /// Quản lý UI màn hình chính của game, bao gồm các nút và chức năng menu
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        #region Properties
        [Header("Main Buttons")]
        [SerializeField] private Button campaignButton;
        [SerializeField] private Button heroesButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button gachaButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button questButton;
        [SerializeField] private Button settingsButton;
        
        [Header("User Info")]
        [SerializeField] private Text playerNameText;
        [SerializeField] private Text playerLevelText;
        [SerializeField] private Slider experienceBar;
        [SerializeField] private Text vipLevelText;
        
        [Header("Resource Display")]
        [SerializeField] private Text goldText;
        [SerializeField] private Text diamondText;
        [SerializeField] private Text energyText;
        [SerializeField] private Slider energyBar;
        [SerializeField] private Text energyTimerText;
        
        [Header("News & Events")]
        [SerializeField] private GameObject newsPanel;
        [SerializeField] private Button closeNewsButton;
        [SerializeField] private Transform eventBannerContainer;
        [SerializeField] private GameObject eventBannerPrefab;
        
        [Header("Level Select")]
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private Button closeLevelSelectButton;
        [SerializeField] private Transform chapterContainer;
        [SerializeField] private GameObject chapterButtonPrefab;
        
        [Header("Daily Rewards")]
        [SerializeField] private GameObject dailyRewardPanel;
        [SerializeField] private Button closeDailyRewardButton;
        [SerializeField] private Transform dailyRewardContainer;
        [SerializeField] private GameObject dailyRewardItemPrefab;
        
        [Header("Login & First Time UI")]
        [SerializeField] private GameObject welcomePanel;
        [SerializeField] private InputField playerNameInput;
        [SerializeField] private Button confirmNameButton;
        
        [Header("Visual Elements")]
        [SerializeField] private GameObject backgroundEffect;
        [SerializeField] private GameObject titleLogo;
        [SerializeField] private AudioClip menuBGM;
        
        // Cached references
        private GameManager gameManager;
        private SaveDataManager saveDataManager;
        private ResourceManager resourceManager;
        private UIManager uiManager;
        private AudioManager audioManager;
        
        // Runtime state
        private List<GameObject> activePanels = new List<GameObject>();
        private bool isFirstLogin = false;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Get references
            gameManager = GameManager.Instance;
            saveDataManager = SaveDataManager.Instance;
            resourceManager = ResourceManager.Instance;
            uiManager = UIManager.Instance;
            audioManager = AudioManager.Instance;
        }
        
        private void Start()
        {
            // Set up button listeners
            SetupButtonListeners();
            
            // Initialize UI
            InitializeUI();
            
            // Check for first login
            CheckFirstLogin();
            
            // Check for daily rewards
            CheckDailyRewards();
            
            // Set up news and events
            SetupNewsAndEvents();
            
            // Play menu music
            PlayMenuMusic();
        }
        
        private void Update()
        {
            // Update energy timer if needed
            UpdateEnergyTimer();
            
            // Update resource displays
            UpdateResourceDisplays();
        }
        #endregion
        
        #region Initialization
        private void SetupButtonListeners()
        {
            // Main menu buttons
            if (campaignButton != null)
                campaignButton.onClick.AddListener(OpenLevelSelect);
                
            if (heroesButton != null)
                heroesButton.onClick.AddListener(OpenHeroInventory);
                
            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OpenUpgradeScreen);
                
            if (gachaButton != null)
                gachaButton.onClick.AddListener(OpenGachaScreen);
                
            if (shopButton != null)
                shopButton.onClick.AddListener(OpenShopScreen);
                
            if (questButton != null)
                questButton.onClick.AddListener(OpenQuestScreen);
                
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettingsScreen);
                
            // Close buttons
            if (closeNewsButton != null)
                closeNewsButton.onClick.AddListener(() => ClosePanel(newsPanel));
                
            if (closeLevelSelectButton != null)
                closeLevelSelectButton.onClick.AddListener(() => ClosePanel(levelSelectPanel));
                
            if (closeDailyRewardButton != null)
                closeDailyRewardButton.onClick.AddListener(() => ClosePanel(dailyRewardPanel));
                
            // First login
            if (confirmNameButton != null)
                confirmNameButton.onClick.AddListener(ConfirmPlayerName);
        }
        
        private void InitializeUI()
        {
            // Hide all panels initially
            if (newsPanel != null) newsPanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
            if (dailyRewardPanel != null) dailyRewardPanel.SetActive(false);
            if (welcomePanel != null) welcomePanel.SetActive(false);
            
            // Initialize player info
            UpdatePlayerInfo();
            
            // Initialize resource displays
            UpdateResourceDisplays();
        }
        
        private void CheckFirstLogin()
        {
            if (saveDataManager != null)
            {
                string playerName = saveDataManager.GetPlayerName();
                isFirstLogin = string.IsNullOrEmpty(playerName);
                
                if (isFirstLogin && welcomePanel != null)
                {
                    // Show first login UI
                    welcomePanel.SetActive(true);
                }
            }
        }
        
        private void CheckDailyRewards()
        {
            // Check if daily rewards are available
            bool hasDailyRewards = true; // This would come from a DailyRewardManager
            
            if (hasDailyRewards && dailyRewardPanel != null && !isFirstLogin)
            {
                // Show daily rewards after a short delay
                StartCoroutine(ShowDailyRewardsDelayed(1.5f));
            }
        }
        
        private IEnumerator ShowDailyRewardsDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            ShowDailyRewards();
        }
        
        private void SetupNewsAndEvents()
        {
            // Clear existing event banners
            if (eventBannerContainer != null)
            {
                foreach (Transform child in eventBannerContainer)
                {
                    Destroy(child.gameObject);
                }
                
                // Add event banners (would come from a service in a real app)
                if (eventBannerPrefab != null)
                {
                    // Add some sample events
                    CreateEventBanner("Sự Kiện Trung Thu", "Tham gia sự kiện Trung Thu nhận quà độc quyền", "Events/MidAutumn");
                    CreateEventBanner("Triệu Hồi Đặc Biệt", "Tỉ lệ triệu hồi anh hùng Huyền Thoại tăng gấp đôi", "Events/SpecialSummon");
                    CreateEventBanner("Nhập Code", "Nhập code AULACTHAN2024 để nhận 1000 kim cương", "Events/RedeemCode");
                }
            }
        }
        
        private void CreateEventBanner(string title, string description, string imagePath)
        {
            GameObject bannerObj = Instantiate(eventBannerPrefab, eventBannerContainer);
            EventBannerUI banner = bannerObj.GetComponent<EventBannerUI>();
            
            if (banner != null)
            {
                Sprite bannerImage = Resources.Load<Sprite>(imagePath);
                banner.Initialize(title, description, bannerImage);
                
                // Add click handler
                banner.SetClickHandler(() => OpenEventDetails(title));
            }
        }
        
        private void PlayMenuMusic()
        {
            if (audioManager != null && menuBGM != null)
            {
                audioManager.PlayMusic(menuBGM.name);
            }
        }
        #endregion
        
        #region UI Updates
        private void UpdatePlayerInfo()
        {
            if (saveDataManager != null)
            {
                // Update player name
                if (playerNameText != null)
                {
                    string playerName = saveDataManager.GetPlayerName();
                    playerNameText.text = playerName;
                }
                
                // Update player level
                if (playerLevelText != null)
                {
                    int playerLevel = saveDataManager.GetPlayerLevel();
                    playerLevelText.text = $"Lv.{playerLevel}";
                }
                
                // TODO: Get player experience and update bar
                if (experienceBar != null)
                {
                    experienceBar.value = 0.6f; // Placeholder value
                }
                
                // Update VIP level if applicable
                if (vipLevelText != null)
                {
                    vipLevelText.text = "VIP 0";
                }
            }
        }
        
        private void UpdateResourceDisplays()
        {
            if (resourceManager != null)
            {
                // Update gold
                if (goldText != null)
                {
                    int gold = resourceManager.GetResourceAmount(ResourceType.Gold);
                    goldText.text = FormatNumber(gold);
                }
                
                // Update diamonds
                if (diamondText != null)
                {
                    int diamonds = resourceManager.GetResourceAmount(ResourceType.Diamond);
                    diamondText.text = FormatNumber(diamonds);
                }
                
                // Update energy
                if (energyText != null)
                {
                    int energy = resourceManager.GetResourceAmount(ResourceType.Energy);
                    int maxEnergy = 100; // This would come from resourceManager
                    energyText.text = $"{energy}/{maxEnergy}";
                    
                    // Update energy bar
                    if (energyBar != null)
                    {
                        energyBar.maxValue = maxEnergy;
                        energyBar.value = energy;
                    }
                }
            }
        }
        
        private void UpdateEnergyTimer()
        {
            if (energyTimerText != null)
            {
                // For now, just show a static time
                // In a real app, this would show time until next energy point
                energyTimerText.text = "05:34";
            }
        }
        #endregion
        
        #region Panel Management
        private void OpenPanel(GameObject panel)
        {
            if (panel == null) return;
            
            // Activate panel
            panel.SetActive(true);
            
            // Add to active panels
            if (!activePanels.Contains(panel))
            {
                activePanels.Add(panel);
            }
            
            // Play sound effect
            PlaySoundEffect("ui_panel_open");
        }
        
        private void ClosePanel(GameObject panel)
        {
            if (panel == null) return;
            
            // Deactivate panel
            panel.SetActive(false);
            
            // Remove from active panels
            activePanels.Remove(panel);
            
            // Play sound effect
            PlaySoundEffect("ui_panel_close");
        }
        
        private void CloseAllPanels()
        {
            // Create a copy to avoid modification during iteration
            List<GameObject> panelsCopy = new List<GameObject>(activePanels);
            
            foreach (GameObject panel in panelsCopy)
            {
                ClosePanel(panel);
            }
        }
        #endregion
        
        #region Button Actions
        private void OpenLevelSelect()
        {
            if (levelSelectPanel != null)
            {
                // Show level select panel
                OpenPanel(levelSelectPanel);
                
                // Populate chapters if needed
                PopulateChapters();
            }
            
            // Play button sound
            PlaySoundEffect("ui_button_click");
        }
        
        private void OpenHeroInventory()
        {
            if (uiManager != null)
            {
                uiManager.ShowScreen(UIScreenType.HeroInventory);
            }
            
            // Play button sound
            PlaySoundEffect("ui_button_click");
        }
        
        private void OpenUpgradeScreen()
        {
            if (uiManager != null)
            {
                uiManager.ShowScreen(UIScreenType.Upgrade);
            }
            
            // Play button sound
            PlaySoundEffect("ui_button_click");
        }
        
        private void OpenGachaScreen()
        {
            if (uiManager != null)
            {
                uiManager.ShowScreen(UIScreenType.Gacha);
            }
            
            // Play button sound
            PlaySoundEffect("ui_button_click");
        }
        
        private void OpenShopScreen()
        {
            if (uiManager != null)
            {
                uiManager.ShowScreen(UIScreenType.Shop);
            }
            
            // Play button sound
            PlaySoundEffect("ui_button_click");
        }
        
        private void OpenQuestScreen()
        {
            if (uiManager != null)
            {
                uiManager.ShowScreen(UIScreenType.Quest);
            }
            
            // Play button sound
            PlaySoundEffect("ui_button_click");
        }
        
        private void OpenSettingsScreen()
        {
            if (uiManager != null)
            {
                uiManager.ShowScreen(UIScreenType.Settings);
            }
            
            // Play button sound
            PlaySoundEffect("ui_button_click");
        }
        
        private void OpenEventDetails(string eventTitle)
        {
            // Show event details panel or navigate to event screen
            Debug.Log($"Opening event details for: {eventTitle}");
            
            // Play button sound
            PlaySoundEffect("ui_button_click");
        }
        
        private void ConfirmPlayerName()
        {
            if (playerNameInput != null && saveDataManager != null)
            {
                string newName = playerNameInput.text.Trim();
                
                if (!string.IsNullOrEmpty(newName))
                {
                    // Save player name
                    saveDataManager.SetPlayerName(newName);
                    
                    // Update player info display
                    UpdatePlayerInfo();
                    
                    // Close welcome panel
                    if (welcomePanel != null)
                    {
                        ClosePanel(welcomePanel);
                    }
                    
                    // Show tutorial or first time user experience
                    StartFirstTimeUserExperience();
                }
                else
                {
                    // Show error message
                    if (uiManager != null)
                    {
                        uiManager.ShowMessagePopup("Vui lòng nhập tên người chơi");
                    }
                }
            }
        }
        
        private void StartLevel(int chapterId, int levelId)
        {
            if (gameManager != null)
            {
                // Check if enough energy
                if (resourceManager != null)
                {
                    int energyCost = 5; // This would be determined by level
                    int currentEnergy = resourceManager.GetResourceAmount(ResourceType.Energy);
                    
                    if (currentEnergy >= energyCost)
                    {
                        // Spend energy
                        resourceManager.SpendResource(ResourceType.Energy, energyCost);
                        
                        // Start level
                        gameManager.StartGame(chapterId, levelId);
                    }
                    else
                    {
                        // Show not enough energy popup
                        if (uiManager != null)
                        {
                            uiManager.ShowConfirmationPopup(
                                "Không đủ năng lượng. Bạn có muốn nạp thêm năng lượng không?",
                                "Nạp",
                                "Hủy",
                                () => OpenShopScreen()
                            );
                        }
                    }
                }
                else
                {
                    // Start level without energy check
                    gameManager.StartGame(chapterId, levelId);
                }
            }
        }
        #endregion
        
        #region Helper Methods
        private void PopulateChapters()
        {
            if (chapterContainer == null || chapterButtonPrefab == null)
                return;
                
            // Clear existing chapters
            foreach (Transform child in chapterContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Get unlocked chapters from SaveDataManager
            int maxUnlockedChapter = 1;
            if (saveDataManager != null)
            {
                // In a real app, this would get the actual unlocked chapters
                maxUnlockedChapter = 3; // Placeholder value
            }
            
            // Create chapter buttons
            for (int i = 1; i <= 13; i++)
            {
                GameObject chapterObj = Instantiate(chapterButtonPrefab, chapterContainer);
                ChapterButtonUI chapterButton = chapterObj.GetComponent<ChapterButtonUI>();
                
                if (chapterButton != null)
                {
                    bool isUnlocked = i <= maxUnlockedChapter;
                    string chapterName = GetChapterName(i);
                    
                    chapterButton.Initialize(i, chapterName, isUnlocked);
                    
                    // Add click handler
                    int chapterId = i;
                    chapterButton.SetClickHandler(() => OnChapterSelected(chapterId));
                }
            }
        }
        
        private string GetChapterName(int chapterId)
        {
            // Return localized chapter names
            switch (chapterId)
            {
                case 1: return "Thời Hồng Bàng - Âu Lạc";
                case 2: return "Khởi nghĩa Hai Bà Trưng";
                case 3: return "Khởi nghĩa Bà Triệu";
                case 4: return "Thời kỳ Vạn Xuân";
                case 5: return "Mai Thúc Loan và Phùng Hưng";
                case 6: return "Ngô Quyền và Bạch Đằng";
                case 7: return "Nhà Đinh - Tiền Lê";
                case 8: return "Nhà Lý";
                case 9: return "Nhà Trần và kháng Mông Nguyên";
                case 10: return "Khởi nghĩa Lam Sơn";
                case 11: return "Thời Mạc - Trịnh - Nguyễn";
                case 12: return "Nhà Tây Sơn và Nguyễn Huệ";
                case 13: return "Chống thế lực tổng hợp";
                default: return $"Chapter {chapterId}";
            }
        }
        
        private void OnChapterSelected(int chapterId)
        {
            Debug.Log($"Chapter {chapterId} selected");
            
            // In a real app, this would show level selection UI for this chapter
            // For now, just start the first level of the chapter
            StartLevel(chapterId, 1);
        }
        
        private void ShowDailyRewards()
        {
            if (dailyRewardPanel == null || dailyRewardContainer == null)
                return;
                
            // Show daily rewards panel
            OpenPanel(dailyRewardPanel);
            
            // Clear existing rewards
            foreach (Transform child in dailyRewardContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Populate daily rewards
            if (dailyRewardItemPrefab != null)
            {
                // Get current login day
                int currentLoginDay = 3; // This would come from a DailyRewardManager
                
                // Create 7 day rewards
                for (int i = 1; i <= 7; i++)
                {
                    GameObject rewardObj = Instantiate(dailyRewardItemPrefab, dailyRewardContainer);
                    DailyRewardUI rewardUI = rewardObj.GetComponent<DailyRewardUI>();
                    
                    if (rewardUI != null)
                    {
                        // Define reward for this day
                        RewardItem reward = GetDailyReward(i);
                        
                        // Determine state
                        DailyRewardState state;
                        if (i < currentLoginDay)
                            state = DailyRewardState.Claimed;
                        else if (i == currentLoginDay)
                            state = DailyRewardState.Available;
                        else
                            state = DailyRewardState.Locked;
                            
                        // Initialize reward UI
                        rewardUI.Initialize(i, reward, state);
                        
                        // Add click handler for available rewards
                        if (state == DailyRewardState.Available)
                        {
                            int day = i;
                            rewardUI.SetClickHandler(() => ClaimDailyReward(day, reward));
                        }
                    }
                }
            }
        }
        
        private RewardItem GetDailyReward(int day)
        {
            // Define rewards for each day
            switch (day)
            {
                case 1:
                    return new RewardItem { name = "Gold", amount = 1000, resourceType = ResourceType.Gold };
                case 2:
                    return new RewardItem { name = "Kim Cương", amount = 50, resourceType = ResourceType.Diamond };
                case 3:
                    return new RewardItem { name = "Năng Lượng", amount = 20, resourceType = ResourceType.Energy };
                case 4:
                    return new RewardItem { name = "Sắt", amount = 50, resourceType = ResourceType.Iron };
                case 5:
                    return new RewardItem { name = "Vải", amount = 50, resourceType = ResourceType.Fabric };
                case 6:
                    return new RewardItem { name = "Đá Thần", amount = 5, resourceType = ResourceType.DivineStone };
                case 7:
                    return new RewardItem { name = "Dấu Ấn Lạc Hồng", amount = 10, resourceType = ResourceType.SilverLacHongMark };
                default:
                    return new RewardItem { name = "Gold", amount = 500, resourceType = ResourceType.Gold };
            }
        }
        
        private void ClaimDailyReward(int day, RewardItem reward)
        {
            Debug.Log($"Claiming day {day} reward: {reward.amount} {reward.name}");
            
            // Add reward to player resources
            if (resourceManager != null)
            {
                resourceManager.AddResource(reward.resourceType, reward.amount);
                
                // Update UI
                UpdateResourceDisplays();
                
                // Mark as claimed in the UI
                Transform rewardTransform = dailyRewardContainer.GetChild(day - 1);
                DailyRewardUI rewardUI = rewardTransform?.GetComponent<DailyRewardUI>();
                
                if (rewardUI != null)
                {
                    rewardUI.SetState(DailyRewardState.Claimed);
                }
                
                // Show reward animation
                PlayRewardAnimation(reward);
                
                // Save daily rewards progress
                // This would be handled by a DailyRewardManager
                
                // Close panel after a delay
                StartCoroutine(CloseDailyRewardsDelayed(2f));
            }
        }
        
        private IEnumerator CloseDailyRewardsDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (dailyRewardPanel != null)
            {
                ClosePanel(dailyRewardPanel);
            }
        }
        
        private void PlayRewardAnimation(RewardItem reward)
        {
            // Play particle effect or animation for claiming reward
            // This would be implemented in a real game
        }
        
        private void StartFirstTimeUserExperience()
        {
            // In a real game, this would start a tutorial or initial story sequence
            Debug.Log("Starting first time user experience");
            
            // For now, just show daily rewards
            ShowDailyRewards();
        }
        
        private string FormatNumber(int number)
        {
            if (number >= 1000000)
            {
                return $"{number / 1000000f:0.#}M";
            }
            else if (number >= 1000)
            {
                return $"{number / 1000f:0.#}K";
            }
            else
            {
                return number.ToString();
            }
        }
        
        private void PlaySoundEffect(string sfxName)
        {
            if (audioManager != null)
            {
                audioManager.PlaySFX(sfxName);
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Component to manage chapter selection buttons
    /// </summary>
    public class ChapterButtonUI : MonoBehaviour
    {
        [SerializeField] private Text chapterNumberText;
        [SerializeField] private Text chapterNameText;
        [SerializeField] private Image chapterImage;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private Button button;
        
        private int chapterId;
        private System.Action onClickAction;
        
        public void Initialize(int id, string name, bool unlocked)
        {
            chapterId = id;
            
            if (chapterNumberText != null)
                chapterNumberText.text = $"Chapter {id}";
                
            if (chapterNameText != null)
                chapterNameText.text = name;
                
            if (chapterImage != null)
            {
                // Try to load chapter image
                Sprite chapterSprite = Resources.Load<Sprite>($"Chapters/{id}");
                if (chapterSprite != null)
                    chapterImage.sprite = chapterSprite;
            }
            
            if (lockIcon != null)
                lockIcon.SetActive(!unlocked);
                
            if (button != null)
            {
                button.interactable = unlocked;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClickAction?.Invoke());
            }
        }
        
        public void SetClickHandler(System.Action action)
        {
            onClickAction = action;
        }
    }
    
    /// <summary>
    /// Component to manage event banner display
    /// </summary>
    public class EventBannerUI : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Image bannerImage;
        [SerializeField] private Button button;
        
        private System.Action onClickAction;
        
        public void Initialize(string title, string description, Sprite image)
        {
            if (titleText != null)
                titleText.text = title;
                
            if (descriptionText != null)
                descriptionText.text = description;
                
            if (bannerImage != null && image != null)
                bannerImage.sprite = image;
                
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClickAction?.Invoke());
            }
        }
        
        public void SetClickHandler(System.Action action)
        {
            onClickAction = action;
        }
    }
    
    /// <summary>
    /// Component to manage daily reward items
    /// </summary>
    public class DailyRewardUI : MonoBehaviour
    {
        [SerializeField] private Text dayText;
        [SerializeField] private Text rewardAmountText;
        [SerializeField] private Image rewardIcon;
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject claimedMark;
        [SerializeField] private GameObject lockIcon;
        
        private int day;
        private RewardItem reward;
        private System.Action onClickAction;
        
        public void Initialize(int day, RewardItem reward, DailyRewardState state)
        {
            this.day = day;
            this.reward = reward;
            
            if (dayText != null)
                dayText.text = $"Day {day}";
                
            if (rewardAmountText != null)
                rewardAmountText.text = reward.amount.ToString();
                
            if (rewardIcon != null)
            {
                // Try to load resource icon
                Sprite resourceSprite = Resources.Load<Sprite>($"Icons/{reward.resourceType}");
                if (resourceSprite != null)
                    rewardIcon.sprite = resourceSprite;
            }
            
            SetState(state);
        }
        
        public void SetState(DailyRewardState state)
        {
            if (claimButton != null)
                claimButton.interactable = state == DailyRewardState.Available;
                
            if (claimedMark != null)
                claimedMark.SetActive(state == DailyRewardState.Claimed);
                
            if (lockIcon != null)
                lockIcon.SetActive(state == DailyRewardState.Locked);
                
            if (claimButton != null)
            {
                claimButton.onClick.RemoveAllListeners();
                
                if (state == DailyRewardState.Available)
                {
                    claimButton.onClick.AddListener(() => onClickAction?.Invoke());
                }
            }
        }
        
        public void SetClickHandler(System.Action action)
        {
            onClickAction = action;
        }
    }
    
    public enum DailyRewardState
    {
        Locked,
        Available,
        Claimed
    }
}