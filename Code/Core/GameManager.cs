using System.Collections;
using UnityEngine;

namespace AuLacThanThu.Core
{
    /// <summary>
    /// Quản lý trạng thái chính của game, vòng đời game và các tác vụ toàn cục
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            // Đảm bảo chỉ có một instance của GameManager
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region Game State
        public enum GameState
        {
            MainMenu,
            Playing,
            Paused,
            GameOver,
            Victory
        }

        public GameState CurrentState { get; private set; }

        // Sự kiện để thông báo khi trạng thái game thay đổi
        public delegate void GameStateChangedHandler(GameState newState);
        public event GameStateChangedHandler OnGameStateChanged;
        #endregion

        #region Dependencies
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private SaveDataManager saveDataManager;
        #endregion

        #region Initialization
        private void InitializeGame()
        {
            // Khởi tạo các manager cần thiết nếu chưa có
            if (levelManager == null) levelManager = GetComponentInChildren<LevelManager>();
            if (resourceManager == null) resourceManager = GetComponentInChildren<ResourceManager>();
            if (audioManager == null) audioManager = GetComponentInChildren<AudioManager>();
            if (saveDataManager == null) saveDataManager = GetComponentInChildren<SaveDataManager>();

            // Đặt trạng thái ban đầu
            ChangeGameState(GameState.MainMenu);
            
            // Load game data nếu có
            StartCoroutine(LoadGameData());
        }

        private IEnumerator LoadGameData()
        {
            // TODO: Load game data từ SaveDataManager
            yield return null;
        }
        #endregion

        #region Game Flow Control
        public void StartGame(int chapterId, int levelId)
        {
            // Bắt đầu level mới
            levelManager.LoadLevel(chapterId, levelId);
            ChangeGameState(GameState.Playing);
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                Time.timeScale = 0f;
                ChangeGameState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                Time.timeScale = 1f;
                ChangeGameState(GameState.Playing);
            }
        }

        public void GameOver()
        {
            ChangeGameState(GameState.GameOver);
            // TODO: Hiển thị UI game over
        }

        public void Victory()
        {
            ChangeGameState(GameState.Victory);
            // TODO: Tính toán phần thưởng và hiển thị UI victory
        }

        public void ReturnToMainMenu()
        {
            // Lưu dữ liệu game nếu cần
            saveDataManager.SaveGameData();
            
            // Chuyển về main menu
            ChangeGameState(GameState.MainMenu);
            // TODO: Load main menu scene
        }
        #endregion

        #region Helper Methods
        private void ChangeGameState(GameState newState)
        {
            CurrentState = newState;
            OnGameStateChanged?.Invoke(newState);
            
            Debug.Log($"Game state changed to: {newState}");
        }
        #endregion
    }
}