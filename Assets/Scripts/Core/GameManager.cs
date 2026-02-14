using UnityEngine;
using OrbWanderer.Inventory;
using OrbWanderer.World;
using OrbWanderer.Wildlife;
using OrbWanderer.Equipment;

namespace OrbWanderer.Core
{
    /// <summary>
    /// Top-level game manager. Handles initialization order, game state,
    /// save/load, and scene transitions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;

        [Header("Save Settings")]
        [SerializeField] private float autoSaveInterval = 120f;
        [SerializeField] private string saveFileName = "orbwanderer_save";

        private float autoSaveTimer;

        public GameState CurrentState => currentState;
        public System.Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            InitializeSystems();
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                autoSaveTimer += Time.deltaTime;
                if (autoSaveTimer >= autoSaveInterval)
                {
                    autoSaveTimer = 0f;
                    SaveGame();
                }
            }
        }

        private void InitializeSystems()
        {
            // Systems auto-initialize via their own Awake/Start
            // This just verifies they exist
            Debug.Log("[GameManager] Initializing The Orb Wanderer...");

            if (SatchelManager.Instance == null)
                Debug.LogWarning("[GameManager] SatchelManager not found!");

            if (WorldMapManager.Instance == null)
                Debug.LogWarning("[GameManager] WorldMapManager not found!");

            if (CompanionManager.Instance == null)
                Debug.LogWarning("[GameManager] CompanionManager not found!");

            if (EquipmentManager.Instance == null)
                Debug.LogWarning("[GameManager] EquipmentManager not found!");

            Debug.Log("[GameManager] Systems initialized.");
        }

        public void StartNewGame()
        {
            SetState(GameState.Playing);
            Debug.Log("[GameManager] New game started. Welcome to the green lands!");
        }

        public void LoadGame()
        {
            string json = PlayerPrefs.GetString(saveFileName, "");
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("[GameManager] No save data found. Starting new game.");
                StartNewGame();
                return;
            }

            var saveData = JsonUtility.FromJson<GameSaveData>(json);
            if (saveData == null)
            {
                Debug.LogError("[GameManager] Failed to parse save data.");
                StartNewGame();
                return;
            }

            // Restore world map state
            if (saveData.worldMap != null)
            {
                WorldMapManager.Instance?.LoadSaveData(saveData.worldMap);
            }

            SetState(GameState.Playing);
            Debug.Log("[GameManager] Game loaded successfully.");
        }

        public void SaveGame()
        {
            var saveData = new GameSaveData();

            // Save world map
            saveData.worldMap = WorldMapManager.Instance?.GetSaveData();

            // Save satchel
            saveData.satchel = SatchelManager.Instance?.GetSaveData();

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(saveFileName, json);
            PlayerPrefs.Save();

            Debug.Log("[GameManager] Game saved.");
        }

        public void PauseGame()
        {
            if (currentState != GameState.Playing) return;
            SetState(GameState.Paused);
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            if (currentState != GameState.Paused) return;
            SetState(GameState.Playing);
            Time.timeScale = 1f;
        }

        public void ReturnToMainMenu()
        {
            SaveGame();
            SetState(GameState.MainMenu);
            Time.timeScale = 1f;
        }

        private void SetState(GameState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && currentState == GameState.Playing)
            {
                SaveGame();
            }
        }

        private void OnApplicationQuit()
        {
            if (currentState == GameState.Playing)
            {
                SaveGame();
            }
        }
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        MapView,
        Dialogue
    }

    [System.Serializable]
    public class GameSaveData
    {
        public WorldMapSaveData worldMap;
        public SatchelSaveData satchel;
        public string lastRegion;
        public float playTime;
    }
}
