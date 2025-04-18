using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AuLacThanThu.Core;
using AuLacThanThu.Gameplay.Tower;
using AuLacThanThu.Gameplay.Enemy;
using AuLacThanThu.Gameplay.Hero;

namespace AuLacThanThu.UI
{
    /// <summary>
    /// Quản lý UI màn hình chiến đấu trong game
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        #region Properties
        [Header("Battle Info")]
        [SerializeField] private Text chapterLevelText;
        [SerializeField] private Text waveInfoText;
        [SerializeField] private Text timerText;
        [SerializeField] private Slider waveProgressBar;
        
        [Header("Fortress")]
        [SerializeField] private Slider fortressHealthBar;
        [SerializeField] private Text fortressHealthText;
        [SerializeField] private Image fortressIcon;
        
        [Header("Hero UI")]
        [SerializeField] private List<HeroSlotUI> heroSlots = new List<HeroSlotUI>();
        
        [Header("Skill UI")]
        [SerializeField] private Transform skillButtonContainer;
        [SerializeField] private GameObject skillButtonPrefab;
        private List<SkillButtonUI> skillButtons = new List<SkillButtonUI>();
        
        [Header("Auto Settings")]
        [SerializeField] private Toggle autoAttackToggle;
        
        [Header("Battle Controls")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button speedUpButton;
        [SerializeField] private Text speedText;
        
        [Header("Level Complete UI")]
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Text starsEarnedText;
        [SerializeField] private Transform rewardContainer;
        
        [Header("Game Over UI")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button gameOverMainMenuButton;
        
        [Header("Pause Menu")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        
        // Cached references
        private GameManager gameManager;
        private LevelManager levelManager;
        private FortressController fortressController;
        private CrossbowController crossbowController;
        private EnemyWaveController waveController;
        private HeroManager heroManager;
        
        // Current battle state
        private int currentChapter;
        private int currentLevel;
        private int currentWave;
        private int totalWaves;
        private float battleTimer;
        private float waveTimer;
        private bool isPaused;
        private float gameSpeed = 1f;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Get references
            gameManager = GameManager.Instance;
            levelManager = FindObjectOfType<LevelManager>();
            fortressController = FindObjectOfType<FortressController>();
            crossbowController = FindObjectOfType<CrossbowController>();
            waveController = FindObjectOfType<EnemyWaveController>();
            heroManager = HeroManager.Instance;
            
            // Hide completion panels
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
        }
        
        private void Start()
        {
            // Subscribe to events
            SubscribeToEvents();
            
            // Set up button listeners
            SetupButtonListeners();
            
            // Initialize UI
            InitializeUI();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            UnsubscribeFromEvents();
        }
        
        private void Update()
        {
            // Update timers
            UpdateTimers();
            
            // Update health displays
            UpdateHealthDisplays();
            
            // Update skill cooldowns
            UpdateSkillCooldowns();
        }
        #endregion
        
        #region Initialization
        private void SubscribeToEvents()
        {
            if (levelManager != null)
            {
                levelManager.OnWaveStarted += OnWaveStarted;
                levelManager.OnWaveCompleted += OnWaveCompleted;
                levelManager.OnLevelCompleted += OnLevelCompleted;
                levelManager.OnLevelFailed += OnLevelFailed;
            }
            
            if (fortressController != null)
            {
                fortressController.OnFortressDamaged += OnFortressDamaged;
                fortressController.OnFortressDestroyed += OnFortressDestroyed;
            }
            
            if (heroManager != null)
            {
                heroManager.OnTeamChanged += OnTeamChanged;
            }
            
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (levelManager != null)
            {
                levelManager.OnWaveStarted -= OnWaveStarted;
                levelManager.OnWaveCompleted -= OnWaveCompleted;
                levelManager.OnLevelCompleted -= OnLevelCompleted;
                levelManager.OnLevelFailed -= OnLevelFailed;
            }
            
            if (fortressController != null)
            {
                fortressController.OnFortressDamaged -= OnFortressDamaged;
                fortressController.OnFortressDestroyed -= OnFortressDestroyed;
            }
            
            if (heroManager != null)
            {
                heroManager.OnTeamChanged -= OnTeamChanged;
            }
            
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }
        
        private void SetupButtonListeners()
        {
            // Pause button
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(PauseGame);
            }
            
            // Speed up button
            if (speedUpButton != null)
            {
                speedUpButton.onClick.AddListener(ToggleGameSpeed);
            }
            
            // Auto attack toggle
            if (autoAttackToggle != null && crossbowController != null)
            {
                autoAttackToggle.onValueChanged.AddListener((value) => 
                {
                    crossbowController.ToggleAutoAttack(value);
                });
                
                // Set initial state
                autoAttackToggle.isOn = false;
            }
            
            // Level complete UI
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.AddListener(GoToNextLevel);
            }
            
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
            
            // Game over UI
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(RetryLevel);
            }
            
            if (gameOverMainMenuButton != null)
            {
                gameOverMainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
            
            // Pause menu
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(ResumeGame);
            }
            
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartLevel);
            }
            
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OpenSettings);
            }
            
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(ReturnToMainMenu);
            }
        }
        
        private void InitializeUI()
        {
            // Initialize level info
            if (levelManager != null)
            {
                currentChapter = levelManager.GetCurrentChapter();
                currentLevel = levelManager.GetCurrentLevel();
                totalWaves = levelManager.GetTotalWaves();
                
                if (chapterLevelText != null)
                {
                    chapterLevelText.text = $"Chapter {currentChapter}-{currentLevel}";
                }
                
                if (waveInfoText != null)
                {
                    waveInfoText.text = $"Wave: 0/{totalWaves}";
                }
                
                if (waveProgressBar != null)
                {
                    waveProgressBar.maxValue = totalWaves;
                    waveProgressBar.value = 0;
                }
            }
            
            // Initialize fortress health
            if (fortressController != null && fortressHealthBar != null)
            {
                fortressHealthBar.maxValue = fortressController.GetMaxHealth();
                fortressHealthBar.value = fortressController.GetCurrentHealth();
                
                if (fortressHealthText != null)
                {
                    fortressHealthText.text = $"{Mathf.CeilToInt(fortressController.GetCurrentHealth())}/{Mathf.CeilToInt(fortressController.GetMaxHealth())}";
                }
            }
            
            // Initialize hero slots
            UpdateHeroSlots();
            
            // Initialize skill buttons
            CreateSkillButtons();
            
            // Set game speed display
            UpdateGameSpeedDisplay();
        }
        
        private void UpdateHeroSlots()
        {
            if (heroManager == null)
                return;
                
            List<HeroBase> activeHeroes = heroManager.GetActiveHeroes();
            
            for (int i = 0; i < heroSlots.Count; i++)
            {
                if (i < activeHeroes.Count && activeHeroes[i] != null)
                {
                    // Show hero in slot
                    heroSlots[i].SetHero(activeHeroes[i]);
                }
                else
                {
                    // Show empty slot
                    heroSlots[i].SetEmpty();
                }
            }
        }
        
        private void CreateSkillButtons()
        {
            if (skillButtonContainer == null || skillButtonPrefab == null)
                return;
                
            // Clear existing buttons
            foreach (Transform child in skillButtonContainer)
            {
                Destroy(child.gameObject);
            }
            skillButtons.Clear();
            
            // Create crossbow skill buttons (if available)
            if (crossbowController != null)
            {
                // For now, just add dummy skill buttons for testing
                for (int i = 0; i < 3; i++)
                {
                    GameObject buttonObj = Instantiate(skillButtonPrefab, skillButtonContainer);
                    SkillButtonUI skillButton = buttonObj.GetComponent<SkillButtonUI>();
                    
                    if (skillButton != null)
                    {
                        switch (i)
                        {
                            case 0:
                                skillButton.Initialize("Skill_Fire", "Lửa Thiêu Đốt", null);
                                break;
                            case 1:
                                skillButton.Initialize("Skill_Ice", "Băng Cầm Tù", null);
                                break;
                            case 2:
                                skillButton.Initialize("Skill_Lightning", "Sét Lấp Lánh", null);
                                break;
                        }
                        
                        // Add button to list
                        skillButtons.Add(skillButton);
                        
                        // Set click handler
                        int index = i;
                        skillButton.SetClickHandler(() => UseSkill(index));
                    }
                }
            }
        }
        #endregion
        
        #region Update Methods
        private void UpdateTimers()
        {
            if (gameManager != null && gameManager.CurrentState == GameManager.GameState.Playing)
            {
                // Update battle timer
                battleTimer += Time.deltaTime;
                
                // Update wave timer if wave controller available
                if (waveController != null)
                {
                    waveTimer = waveController.GetWaveTimer();
                }
                
                // Update timer text
                if (timerText != null)
                {
                    int minutes = Mathf.FloorToInt(battleTimer / 60f);
                    int seconds = Mathf.FloorToInt(battleTimer % 60f);
                    timerText.text = $"{minutes:00}:{seconds:00}";
                }
            }
        }
        
        private void UpdateHealthDisplays()
        {
            // Update fortress health
            if (fortressController != null && fortressHealthBar != null)
            {
                fortressHealthBar.value = fortressController.GetCurrentHealth();
                
                if (fortressHealthText != null)
                {
                    fortressHealthText.text = $"{Mathf.CeilToInt(fortressController.GetCurrentHealth())}/{Mathf.CeilToInt(fortressController.GetMaxHealth())}";
                }
            }
            
            // Update hero health in slots
            foreach (HeroSlotUI heroSlot in heroSlots)
            {
                heroSlot.UpdateHealthDisplay();
            }
        }
        
        private void UpdateSkillCooldowns()
        {
            // Update skill button cooldowns
            foreach (SkillButtonUI skillButton in skillButtons)
            {
                // In a real implementation, get actual cooldown info from the skill system
                float randomCooldownProgress = Random.value;
                skillButton.UpdateCooldown(randomCooldownProgress);
            }
        }
        
        private void UpdateGameSpeedDisplay()
        {
            if (speedText != null)
            {
                speedText.text = gameSpeed == 1f ? "1x" : "2x";
            }
        }
        #endregion
        
        #region Event Handlers
        private void OnWaveStarted(int waveNumber, int totalWaves)
        {
            currentWave = waveNumber;
            
            // Update wave info
            if (waveInfoText != null)
            {
                waveInfoText.text = $"Wave: {waveNumber}/{totalWaves}";
            }
            
            // Update progress bar
            if (waveProgressBar != null)
            {
                waveProgressBar.value = waveNumber;
            }
        }
        
        private void OnWaveCompleted(int waveNumber, int totalWaves)
        {
            // Update wave info
            if (waveInfoText != null)
            {
                waveInfoText.text = $"Wave: {waveNumber}/{totalWaves}";
            }
            
            // Update progress bar
            if (waveProgressBar != null)
            {
                waveProgressBar.value = waveNumber;
            }
        }
        
        private void OnLevelCompleted()
        {
            // Show level complete panel
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(true);
                
                // Calculate stars based on fortress health
                int stars = CalculateStars();
                
                // Update stars display
                if (starsEarnedText != null)
                {
                    starsEarnedText.text = $"{stars} Stars";
                }
                
                // Show rewards
                ShowLevelRewards();
            }
        }
        
        private void OnLevelFailed()
        {
            // Show game over panel
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }
        
        private void OnFortressDamaged()
        {
            // Update fortress health display
            UpdateHealthDisplays();
            
            // Play damage effect if needed
        }
        
        private void OnFortressDestroyed()
        {
            // This will be handled by OnLevelFailed
        }
        
        private void OnTeamChanged(List<OwnedHero> team)
        {
            // Update hero slots
            UpdateHeroSlots();
        }
        
        private void OnGameStateChanged(GameManager.GameState newState)
        {
            if (newState == GameManager.GameState.Paused)
            {
                // Show pause panel
                if (pausePanel != null)
                {
                    pausePanel.SetActive(true);
                }
                
                isPaused = true;
            }
            else if (newState == GameManager.GameState.Playing)
            {
                // Hide pause panel
                if (pausePanel != null)
                {
                    pausePanel.SetActive(false);
                }
                
                isPaused = false;
            }
        }
        #endregion
        
        #region Button Actions
        private void PauseGame()
        {
            if (gameManager != null)
            {
                gameManager.PauseGame();
            }
        }
        
        private void ResumeGame()
        {
            if (gameManager != null)
            {
                gameManager.ResumeGame();
            }
        }
        
        private void ToggleGameSpeed()
        {
            // Toggle between 1x and 2x speed
            gameSpeed = gameSpeed == 1f ? 2f : 1f;
            
            // Apply new time scale
            Time.timeScale = gameSpeed;
            
            // Update display
            UpdateGameSpeedDisplay();
        }
        
        private void UseSkill(int skillIndex)
        {
            Debug.Log($"Using skill {skillIndex}");
            
            // In a real implementation, this would trigger the appropriate skill
            // For now, just play a visual effect on the button
            if (skillIndex >= 0 && skillIndex < skillButtons.Count)
            {
                skillButtons[skillIndex].PlayActivationEffect();
            }
        }
        
        private void GoToNextLevel()
        {
            if (gameManager != null)
            {
                // Reset time scale
                Time.timeScale = 1f;
                
                // Load next level
                int nextLevel = currentLevel + 1;
                gameManager.StartGame(currentChapter, nextLevel);
            }
        }
        
        private void RetryLevel()
        {
            if (gameManager != null)
            {
                // Reset time scale
                Time.timeScale = 1f;
                
                // Reload current level
                gameManager.StartGame(currentChapter, currentLevel);
            }
        }
        
        private void RestartLevel()
        {
            if (gameManager != null)
            {
                // Hide pause menu
                if (pausePanel != null)
                {
                    pausePanel.SetActive(false);
                }
                
                // Reset time scale
                Time.timeScale = 1f;
                
                // Reload current level
                gameManager.StartGame(currentChapter, currentLevel);
            }
        }
        
        private void ReturnToMainMenu()
        {
            if (gameManager != null)
            {
                // Reset time scale
                Time.timeScale = 1f;
                
                // Return to main menu
                gameManager.ReturnToMainMenu();
            }
        }
        
        private void OpenSettings()
        {
            // Show settings panel
            // This would typically open a settings popup
            Debug.Log("Settings button clicked");
        }
        #endregion
        
        #region Helper Methods
        private int CalculateStars()
        {
            // Calculate stars based on fortress health percentage
            float healthPercentage = 0f;
            
            if (fortressController != null)
            {
                healthPercentage = fortressController.GetHealthPercentage();
            }
            
            // 3 stars: 75%+ health
            // 2 stars: 40%+ health
            // 1 star: completed level
            if (healthPercentage >= 0.75f)
            {
                return 3;
            }
            else if (healthPercentage >= 0.4f)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }
        
        private void ShowLevelRewards()
        {
            if (rewardContainer == null)
                return;
                
            // Clear existing rewards
            foreach (Transform child in rewardContainer)
            {
                Destroy(child.gameObject);
            }
            
            // TODO: Get actual rewards from level manager or resource system
            
            // For now, show dummy rewards
            List<RewardItem> rewards = new List<RewardItem>
            {
                new RewardItem { name = "Gold", amount = 100, resourceType = ResourceType.Gold },
                new RewardItem { name = "Iron", amount = 20, resourceType = ResourceType.Iron },
                new RewardItem { name = "Fabric", amount = 15, resourceType = ResourceType.Fabric }
            };
            
            // Create reward UI elements
            GameObject rewardItemPrefab = Resources.Load<GameObject>("UI/RewardItem");
            
            if (rewardItemPrefab != null)
            {
                foreach (RewardItem reward in rewards)
                {
                    GameObject rewardItem = Instantiate(rewardItemPrefab, rewardContainer);
                    
                    // Set icon
                    Image iconImage = rewardItem.transform.Find("Icon")?.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        // Try to load icon for this resource type
                        Sprite icon = Resources.Load<Sprite>($"Icons/{reward.resourceType}");
                        if (icon != null)
                        {
                            iconImage.sprite = icon;
                        }
                    }
                    
                    // Set amount text
                    Text amountText = rewardItem.transform.Find("Amount")?.GetComponent<Text>();
                    if (amountText != null)
                    {
                        amountText.text = reward.amount.ToString();
                    }
                    
                    // Set name text
                    Text nameText = rewardItem.transform.Find("Name")?.GetComponent<Text>();
                    if (nameText != null)
                    {
                        nameText.text = reward.name;
                    }
                }
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Component that manages a hero slot in the battle UI
    /// </summary>
    [System.Serializable]
    public class HeroSlotUI
    {
        [SerializeField] private GameObject slotGameObject;
        [SerializeField] private Image heroIcon;
        [SerializeField] private Image elementIcon;
        [SerializeField] private Slider healthBar;
        [SerializeField] private Text levelText;
        [SerializeField] private Image cooldownOverlay;
        
        private HeroBase heroReference;
        
        public void SetHero(HeroBase hero)
        {
            heroReference = hero;
            
            if (slotGameObject != null)
            {
                slotGameObject.SetActive(true);
            }
            
            if (heroIcon != null && hero != null)
            {
                // Try to get hero data and set icon
                string heroId = hero.GetHeroId();
                Sprite heroSprite = Resources.Load<Sprite>($"Heroes/Icons/{heroId}");
                
                if (heroSprite != null)
                {
                    heroIcon.sprite = heroSprite;
                }
            }
            
            if (elementIcon != null && hero != null)
            {
                // Set element icon based on hero element
                ElementType element = hero.GetElementType();
                Sprite elementSprite = Resources.Load<Sprite>($"Elements/{element}");
                
                if (elementSprite != null)
                {
                    elementIcon.sprite = elementSprite;
                    elementIcon.gameObject.SetActive(true);
                }
                else
                {
                    elementIcon.gameObject.SetActive(false);
                }
            }
            
            if (levelText != null && hero != null)
            {
                // Set level text
                levelText.text = $"Lv.{hero.GetLevel()}";
            }
            
            // Initialize health bar
            UpdateHealthDisplay();
            
            // Reset cooldown overlay
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0f;
            }
        }
        
        public void SetEmpty()
        {
            heroReference = null;
            
            if (slotGameObject != null)
            {
                slotGameObject.SetActive(false);
            }
        }
        
        public void UpdateHealthDisplay()
        {
            if (healthBar != null && heroReference != null)
            {
                healthBar.value = heroReference.GetHealthPercentage();
            }
        }
        
        public void UpdateCooldown(float progress)
        {
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = progress;
            }
        }
        
        public void ActivateHero()
        {
            if (heroReference != null)
            {
                heroReference.UseActiveSkill();
            }
        }
    }
    
    /// <summary>
    /// Component that manages a skill button in the battle UI
    /// </summary>
    public class SkillButtonUI : MonoBehaviour
    {
        [SerializeField] private Image skillIcon;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private Text cooldownText;
        [SerializeField] private Button button;
        [SerializeField] private GameObject activationEffect;
        
        private string skillId;
        private System.Action onClickAction;
        
        public void Initialize(string id, string name, Sprite icon)
        {
            skillId = id;
            
            if (skillIcon != null)
            {
                if (icon != null)
                {
                    skillIcon.sprite = icon;
                }
                else
                {
                    // Try to load icon from resources
                    Sprite skillSprite = Resources.Load<Sprite>($"Skills/Icons/{id}");
                    if (skillSprite != null)
                    {
                        skillIcon.sprite = skillSprite;
                    }
                }
            }
            
            // Set up button
            if (button != null)
            {
                button.onClick.AddListener(() => onClickAction?.Invoke());
            }
            
            // Reset cooldown
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0f;
            }
            
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
        
        public void SetClickHandler(System.Action action)
        {
            onClickAction = action;
        }
        
        public void UpdateCooldown(float progress)
        {
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = progress;
                
                if (cooldownText != null)
                {
                    if (progress > 0)
                    {
                        // Calculate and show cooldown time
                        float cooldownTime = progress * 10f; // Arbitrary value for demo
                        cooldownText.text = Mathf.CeilToInt(cooldownTime).ToString();
                        cooldownText.gameObject.SetActive(true);
                    }
                    else
                    {
                        cooldownText.gameObject.SetActive(false);
                    }
                }
                
                // Enable/disable button based on cooldown
                if (button != null)
                {
                    button.interactable = progress <= 0;
                }
            }
        }
        
        public void PlayActivationEffect()
        {
            if (activationEffect != null)
            {
                activationEffect.SetActive(true);
                StartCoroutine(DisableEffectAfterDelay(0.5f));
            }
        }
        
        private IEnumerator DisableEffectAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (activationEffect != null)
            {
                activationEffect.SetActive(false);
            }
        }
    }
}