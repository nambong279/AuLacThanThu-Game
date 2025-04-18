using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AuLacThanThu.Core;

namespace AuLacThanThu.UI
{
    /// <summary>
    /// Quản lý toàn bộ UI trong game, bao gồm chuyển đổi các màn hình khác nhau
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("UIManager");
                        _instance = obj.AddComponent<UIManager>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        [Header("Screen References")]
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject battleScreen;
        [SerializeField] private GameObject heroInventoryScreen;
        [SerializeField] private GameObject upgradeScreen;
        [SerializeField] private GameObject gachaScreen;
        [SerializeField] private GameObject questScreen;
        [SerializeField] private GameObject shopScreen;
        [SerializeField] private GameObject settingsScreen;
        
        [Header("Popup References")]
        [SerializeField] private GameObject loadingPopup;
        [SerializeField] private GameObject confirmationPopup;
        [SerializeField] private GameObject messagePopup;
        [SerializeField] private GameObject rewardPopup;
        
        [Header("Common UI Elements")]
        [SerializeField] private GameObject topBar;
        [SerializeField] private GameObject bottomBar;
        
        [Header("Settings")]
        [SerializeField] private bool showTransitions = true;
        [SerializeField] private float transitionDuration = 0.3f;
        
        // Current active screen
        private GameObject currentScreen;
        private List<GameObject> activePopups = new List<GameObject>();
        
        // Screen history for back navigation
        private Stack<GameObject> screenHistory = new Stack<GameObject>();
        
        // UI scaling reference
        private CanvasScaler canvasScaler;
        
        // Screen transition animation
        private Coroutine screenTransitionCoroutine;
        #endregion
        
        #region Events
        public delegate void ScreenChangeHandler(string screenName);
        public event ScreenChangeHandler OnScreenChanged;
        
        public delegate void PopupEventHandler(string popupName);
        public event PopupEventHandler OnPopupOpened;
        public event PopupEventHandler OnPopupClosed;
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
            
            // Get canvas scaler
            canvasScaler = GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                canvasScaler = gameObject.AddComponent<CanvasScaler>();
                SetupDefaultCanvasScaler();
            }
        }
        
        private void Start()
        {
            // Hide all screens initially
            HideAllScreens();
            
            // Default to main menu if available
            if (mainMenuScreen != null)
            {
                ShowScreen(mainMenuScreen);
            }
            
            // Subscribe to game state events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
            
            // Initialize UI elements
            InitializeUI();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }
        #endregion
        
        #region Initialization
        private void SetupDefaultCanvasScaler()
        {
            // Set up for mobile with appropriate scaling
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920); // Portrait mode
            canvasScaler.matchWidthOrHeight = 0.5f; // Balance between width and height matching
        }
        
        private void InitializeUI()
        {
            // Ensure all screens have CanvasGroup for transitions
            EnsureCanvasGroup(mainMenuScreen);
            EnsureCanvasGroup(battleScreen);
            EnsureCanvasGroup(heroInventoryScreen);
            EnsureCanvasGroup(upgradeScreen);
            EnsureCanvasGroup(gachaScreen);
            EnsureCanvasGroup(questScreen);
            EnsureCanvasGroup(shopScreen);
            EnsureCanvasGroup(settingsScreen);
            
            // Initialize popups
            EnsureCanvasGroup(loadingPopup);
            EnsureCanvasGroup(confirmationPopup);
            EnsureCanvasGroup(messagePopup);
            EnsureCanvasGroup(rewardPopup);
            
            // Hide all popups
            if (loadingPopup != null) loadingPopup.SetActive(false);
            if (confirmationPopup != null) confirmationPopup.SetActive(false);
            if (messagePopup != null) messagePopup.SetActive(false);
            if (rewardPopup != null) rewardPopup.SetActive(false);
        }
        
        private void EnsureCanvasGroup(GameObject screen)
        {
            if (screen != null && !screen.GetComponent<CanvasGroup>())
            {
                screen.AddComponent<CanvasGroup>();
            }
        }
        
        private void HideAllScreens()
        {
            if (mainMenuScreen != null) mainMenuScreen.SetActive(false);
            if (battleScreen != null) battleScreen.SetActive(false);
            if (heroInventoryScreen != null) heroInventoryScreen.SetActive(false);
            if (upgradeScreen != null) upgradeScreen.SetActive(false);
            if (gachaScreen != null) gachaScreen.SetActive(false);
            if (questScreen != null) questScreen.SetActive(false);
            if (shopScreen != null) shopScreen.SetActive(false);
            if (settingsScreen != null) settingsScreen.SetActive(false);
        }
        #endregion
        
        #region Screen Management
        public void ShowScreen(UIScreenType screenType, bool addToHistory = true)
        {
            GameObject screenToShow = GetScreenByType(screenType);
            
            if (screenToShow != null)
            {
                ShowScreen(screenToShow, addToHistory);
            }
            else
            {
                Debug.LogWarning($"Screen of type {screenType} not found!");
            }
        }
        
        public void ShowScreen(GameObject screen, bool addToHistory = true)
        {
            if (screen == null)
                return;
                
            // Add current screen to history if needed
            if (currentScreen != null && addToHistory)
            {
                screenHistory.Push(currentScreen);
            }
            
            GameObject previousScreen = currentScreen;
            currentScreen = screen;
            
            // Transition between screens
            if (showTransitions && previousScreen != null)
            {
                if (screenTransitionCoroutine != null)
                {
                    StopCoroutine(screenTransitionCoroutine);
                }
                
                screenTransitionCoroutine = StartCoroutine(TransitionBetweenScreens(previousScreen, currentScreen));
            }
            else
            {
                // Immediately hide previous and show current
                if (previousScreen != null)
                {
                    previousScreen.SetActive(false);
                }
                
                currentScreen.SetActive(true);
            }
            
            // Update top and bottom bars visibility based on screen
            UpdateBarsVisibility(screenType: GetScreenType(screen));
            
            // Trigger event
            OnScreenChanged?.Invoke(screen.name);
            
            // Trigger UI event
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.TriggerEvent(GameEventType.ScreenOpened, 
                    new UIEventArgs(screen.name));
            }
        }
        
        public void BackToPreviousScreen()
        {
            if (screenHistory.Count > 0)
            {
                GameObject previousScreen = screenHistory.Pop();
                ShowScreen(previousScreen, false);
            }
            else if (currentScreen != mainMenuScreen && mainMenuScreen != null)
            {
                // Default to main menu if no history
                ShowScreen(mainMenuScreen, false);
            }
        }
        
        public void ShowMainMenu()
        {
            if (mainMenuScreen != null)
            {
                // Clear history and show main menu
                screenHistory.Clear();
                ShowScreen(mainMenuScreen, false);
            }
        }
        
        private IEnumerator TransitionBetweenScreens(GameObject fromScreen, GameObject toScreen)
        {
            // Ensure both screens have CanvasGroup
            CanvasGroup fromCanvasGroup = fromScreen.GetComponent<CanvasGroup>();
            CanvasGroup toCanvasGroup = toScreen.GetComponent<CanvasGroup>();
            
            if (fromCanvasGroup == null || toCanvasGroup == null)
            {
                // Fallback to immediate transition
                fromScreen.SetActive(false);
                toScreen.SetActive(true);
                yield break;
            }
            
            // Activate both screens during transition
            fromScreen.SetActive(true);
            toScreen.SetActive(true);
            
            // Set initial alpha values
            fromCanvasGroup.alpha = 1f;
            toCanvasGroup.alpha = 0f;
            
            // Fade out from screen, fade in to screen
            float elapsedTime = 0f;
            while (elapsedTime < transitionDuration)
            {
                float normalizedTime = elapsedTime / transitionDuration;
                
                fromCanvasGroup.alpha = Mathf.Lerp(1f, 0f, normalizedTime);
                toCanvasGroup.alpha = Mathf.Lerp(0f, 1f, normalizedTime);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure final values
            fromCanvasGroup.alpha = 0f;
            toCanvasGroup.alpha = 1f;
            
            // Deactivate from screen
            fromScreen.SetActive(false);
            
            screenTransitionCoroutine = null;
        }
        
        private GameObject GetScreenByType(UIScreenType screenType)
        {
            switch (screenType)
            {
                case UIScreenType.MainMenu:
                    return mainMenuScreen;
                    
                case UIScreenType.Battle:
                    return battleScreen;
                    
                case UIScreenType.HeroInventory:
                    return heroInventoryScreen;
                    
                case UIScreenType.Upgrade:
                    return upgradeScreen;
                    
                case UIScreenType.Gacha:
                    return gachaScreen;
                    
                case UIScreenType.Quest:
                    return questScreen;
                    
                case UIScreenType.Shop:
                    return shopScreen;
                    
                case UIScreenType.Settings:
                    return settingsScreen;
                    
                default:
                    return null;
            }
        }
        
        private UIScreenType GetScreenType(GameObject screen)
        {
            if (screen == mainMenuScreen) return UIScreenType.MainMenu;
            if (screen == battleScreen) return UIScreenType.Battle;
            if (screen == heroInventoryScreen) return UIScreenType.HeroInventory;
            if (screen == upgradeScreen) return UIScreenType.Upgrade;
            if (screen == gachaScreen) return UIScreenType.Gacha;
            if (screen == questScreen) return UIScreenType.Quest;
            if (screen == shopScreen) return UIScreenType.Shop;
            if (screen == settingsScreen) return UIScreenType.Settings;
            
            return UIScreenType.Unknown;
        }
        
        private void UpdateBarsVisibility(UIScreenType screenType)
        {
            if (topBar != null)
            {
                topBar.SetActive(screenType != UIScreenType.Battle);
            }
            
            if (bottomBar != null)
            {
                bottomBar.SetActive(screenType != UIScreenType.Battle);
            }
        }
        #endregion
        
        #region Popup Management
        public void ShowLoadingPopup(string message = "Loading...")
        {
            if (loadingPopup != null)
            {
                // Set loading message
                Text messageText = loadingPopup.GetComponentInChildren<Text>();
                if (messageText != null)
                {
                    messageText.text = message;
                }
                
                // Show popup
                ShowPopup(loadingPopup);
            }
        }
        
        public void HideLoadingPopup()
        {
            if (loadingPopup != null)
            {
                HidePopup(loadingPopup);
            }
        }
        
        public void ShowConfirmationPopup(string message, string confirmText, string cancelText, System.Action onConfirm, System.Action onCancel = null)
        {
            if (confirmationPopup != null)
            {
                // Set message
                Text messageText = confirmationPopup.transform.Find("Message")?.GetComponent<Text>();
                if (messageText != null)
                {
                    messageText.text = message;
                }
                
                // Set confirm button
                Button confirmButton = confirmationPopup.transform.Find("ConfirmButton")?.GetComponent<Button>();
                Text confirmButtonText = confirmButton?.GetComponentInChildren<Text>();
                
                if (confirmButton != null)
                {
                    // Clear previous listeners
                    confirmButton.onClick.RemoveAllListeners();
                    
                    // Add new listener
                    confirmButton.onClick.AddListener(() => 
                    {
                        HidePopup(confirmationPopup);
                        onConfirm?.Invoke();
                    });
                    
                    // Set button text
                    if (confirmButtonText != null)
                    {
                        confirmButtonText.text = confirmText;
                    }
                }
                
                // Set cancel button
                Button cancelButton = confirmationPopup.transform.Find("CancelButton")?.GetComponent<Button>();
                Text cancelButtonText = cancelButton?.GetComponentInChildren<Text>();
                
                if (cancelButton != null)
                {
                    // Clear previous listeners
                    cancelButton.onClick.RemoveAllListeners();
                    
                    // Add new listener
                    cancelButton.onClick.AddListener(() => 
                    {
                        HidePopup(confirmationPopup);
                        onCancel?.Invoke();
                    });
                    
                    // Set button text
                    if (cancelButtonText != null)
                    {
                        cancelButtonText.text = cancelText;
                    }
                }
                
                // Show popup
                ShowPopup(confirmationPopup);
            }
        }
        
        public void ShowMessagePopup(string message, string buttonText = "OK", System.Action onClose = null)
        {
            if (messagePopup != null)
            {
                // Set message
                Text messageText = messagePopup.transform.Find("Message")?.GetComponent<Text>();
                if (messageText != null)
                {
                    messageText.text = message;
                }
                
                // Set button
                Button closeButton = messagePopup.transform.Find("CloseButton")?.GetComponent<Button>();
                Text buttonTextComponent = closeButton?.GetComponentInChildren<Text>();
                
                if (closeButton != null)
                {
                    // Clear previous listeners
                    closeButton.onClick.RemoveAllListeners();
                    
                    // Add new listener
                    closeButton.onClick.AddListener(() => 
                    {
                        HidePopup(messagePopup);
                        onClose?.Invoke();
                    });
                    
                    // Set button text
                    if (buttonTextComponent != null)
                    {
                        buttonTextComponent.text = buttonText;
                    }
                }
                
                // Show popup
                ShowPopup(messagePopup);
            }
        }
        
        public void ShowRewardPopup(List<RewardItem> rewards, string title = "Rewards", System.Action onClose = null)
        {
            if (rewardPopup != null)
            {
                // Set title
                Text titleText = rewardPopup.transform.Find("Title")?.GetComponent<Text>();
                if (titleText != null)
                {
                    titleText.text = title;
                }
                
                // Get reward container
                Transform rewardContainer = rewardPopup.transform.Find("RewardContainer");
                
                if (rewardContainer != null)
                {
                    // Clear existing rewards
                    foreach (Transform child in rewardContainer)
                    {
                        Destroy(child.gameObject);
                    }
                    
                    // Add reward items
                    GameObject rewardItemPrefab = Resources.Load<GameObject>("UI/RewardItem");
                    
                    if (rewardItemPrefab != null)
                    {
                        foreach (RewardItem reward in rewards)
                        {
                            GameObject rewardItem = Instantiate(rewardItemPrefab, rewardContainer);
                            
                            // Set icon
                            Image iconImage = rewardItem.transform.Find("Icon")?.GetComponent<Image>();
                            if (iconImage != null && reward.icon != null)
                            {
                                iconImage.sprite = reward.icon;
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
                
                // Set close button
                Button closeButton = rewardPopup.transform.Find("CloseButton")?.GetComponent<Button>();
                
                if (closeButton != null)
                {
                    // Clear previous listeners
                    closeButton.onClick.RemoveAllListeners();
                    
                    // Add new listener
                    closeButton.onClick.AddListener(() => 
                    {
                        HidePopup(rewardPopup);
                        onClose?.Invoke();
                    });
                }
                
                // Show popup
                ShowPopup(rewardPopup);
            }
        }
        
        private void ShowPopup(GameObject popup)
        {
            if (popup == null) return;
            
            // Add to active popups
            if (!activePopups.Contains(popup))
            {
                activePopups.Add(popup);
            }
            
            // Show popup
            popup.SetActive(true);
            
            // Play show animation
            AnimatePopup(popup, true);
            
            // Trigger event
            OnPopupOpened?.Invoke(popup.name);
            
            // Trigger UI event
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.TriggerEvent(GameEventType.ScreenOpened, 
                    new UIEventArgs(popup.name));
            }
        }
        
        private void HidePopup(GameObject popup)
        {
            if (popup == null) return;
            
            // Play hide animation
            AnimatePopup(popup, false, () => 
            {
                // Hide popup
                popup.SetActive(false);
                
                // Remove from active popups
                activePopups.Remove(popup);
            });
            
            // Trigger event
            OnPopupClosed?.Invoke(popup.name);
            
            // Trigger UI event
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.TriggerEvent(GameEventType.ScreenClosed, 
                    new UIEventArgs(popup.name));
            }
        }
        
        private void AnimatePopup(GameObject popup, bool show, System.Action onComplete = null)
        {
            RectTransform rect = popup.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
            
            if (rect != null && canvasGroup != null)
            {
                StartCoroutine(AnimatePopupCoroutine(rect, canvasGroup, show, onComplete));
            }
            else
            {
                onComplete?.Invoke();
            }
        }
        
        private IEnumerator AnimatePopupCoroutine(RectTransform rect, CanvasGroup canvasGroup, bool show, System.Action onComplete)
        {
            float duration = 0.2f;
            float elapsedTime = 0f;
            
            // Initial values
            float startScale = show ? 0.8f : 1f;
            float endScale = show ? 1f : 0.8f;
            float startAlpha = show ? 0f : 1f;
            float endAlpha = show ? 1f : 0f;
            
            rect.localScale = new Vector3(startScale, startScale, 1f);
            canvasGroup.alpha = startAlpha;
            
            // Animate
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                float easedT = EaseInOutQuad(t);
                
                float scale = Mathf.Lerp(startScale, endScale, easedT);
                float alpha = Mathf.Lerp(startAlpha, endAlpha, easedT);
                
                rect.localScale = new Vector3(scale, scale, 1f);
                canvasGroup.alpha = alpha;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure final values
            rect.localScale = new Vector3(endScale, endScale, 1f);
            canvasGroup.alpha = endAlpha;
            
            // Complete
            onComplete?.Invoke();
        }
        
        private float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }
        #endregion
        
        #region Event Handlers
        private void OnGameStateChanged(GameManager.GameState newState)
        {
            // Update UI based on game state
            switch (newState)
            {
                case GameManager.GameState.MainMenu:
                    ShowScreen(mainMenuScreen, false);
                    break;
                    
                case GameManager.GameState.Playing:
                    ShowScreen(battleScreen, false);
                    break;
                    
                case GameManager.GameState.Paused:
                    // Show pause menu
                    break;
                    
                case GameManager.GameState.GameOver:
                    // Show game over screen
                    break;
                    
                case GameManager.GameState.Victory:
                    // Show victory screen
                    break;
            }
        }
        #endregion
        
        #region Public Utility Methods
        public UIScreenType GetCurrentScreenType()
        {
            return GetScreenType(currentScreen);
        }
        
        public bool IsScreenActive(UIScreenType screenType)
        {
            GameObject screen = GetScreenByType(screenType);
            return screen != null && screen.activeSelf;
        }
        
        public bool IsPopupActive(GameObject popup)
        {
            return activePopups.Contains(popup);
        }
        
        public void CloseAllPopups()
        {
            // Create a copy to avoid modification during iteration
            List<GameObject> popupsCopy = new List<GameObject>(activePopups);
            
            foreach (GameObject popup in popupsCopy)
            {
                HidePopup(popup);
            }
        }
        
        public void SetTransitionsEnabled(bool enabled)
        {
            showTransitions = enabled;
        }
        #endregion
    }
    
    public enum UIScreenType
    {
        Unknown,
        MainMenu,
        Battle,
        HeroInventory,
        Upgrade,
        Gacha,
        Quest,
        Shop,
        Settings
    }
    
    [System.Serializable]
    public class RewardItem
    {
        public string name;
        public int amount;
        public Sprite icon;
        public ResourceType resourceType;
    }
}