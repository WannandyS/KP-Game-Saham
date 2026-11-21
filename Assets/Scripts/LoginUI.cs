using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoginSceneUI : MonoBehaviour
{
    [Header("Auth Panel (Login / Register)")]
    public GameObject authPanel;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button registerButton;
    public TMP_Text statusMessageText;

    [Header("Save Slots Panel (After Login)")]
    public GameObject saveSlotsPanel;
    public TMP_Text loggedInUserText;
    public Button logoutButton;
    public Button newGameButton;
    public TMP_InputField newSaveNameInput;
    public Transform saveSlotsContainer;
    public GameObject saveSlotRowPrefab;

    [Header("Scene Navigation")]
    [Tooltip("Name of the main trading / market scene to load after starting or loading a save.")]
    public string tradingSceneName = "MarketOverview";

    private void Start()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(OnClickLogin);
        }

        if (registerButton != null)
        {
            registerButton.onClick.RemoveAllListeners();
            registerButton.onClick.AddListener(OnClickRegister);
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveAllListeners();
            logoutButton.onClick.AddListener(OnClickLogout);
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(OnClickNewGame);
        }

        if (SaveSystemManager.Instance != null)
        {
            SaveSystemManager.Instance.OnSaveSystemMessage += HandleStatusMessage;
            SaveSystemManager.Instance.OnAccountChanged += HandleAccountChanged;
        }

        RefreshPanels();
    }

    private void OnDestroy()
    {
        if (SaveSystemManager.Instance != null)
        {
            SaveSystemManager.Instance.OnSaveSystemMessage -= HandleStatusMessage;
            SaveSystemManager.Instance.OnAccountChanged -= HandleAccountChanged;
        }
    }

    private void HandleStatusMessage(string msg)
    {
        if (statusMessageText != null)
        {
            statusMessageText.text = msg;
        }
    }

    private void HandleAccountChanged(string username)
    {
        RefreshPanels();
    }

    private void RefreshPanels()
    {
        bool isLoggedIn = SaveSystemManager.Instance != null &&
                          !string.IsNullOrEmpty(SaveSystemManager.Instance.CurrentLoggedInUser);

        if (authPanel != null) authPanel.SetActive(!isLoggedIn);
        if (saveSlotsPanel != null) saveSlotsPanel.SetActive(isLoggedIn);

        if (isLoggedIn)
        {
            if (loggedInUserText != null)
            {
                loggedInUserText.text = $"Account: {SaveSystemManager.Instance.CurrentLoggedInUser}";
            }
            RebuildSaveSlotsList();
        }
    }

    public void OnClickLogin()
    {
        if (SaveSystemManager.Instance == null) return;

        string user = usernameInput != null ? usernameInput.text : "";
        string pass = passwordInput != null ? passwordInput.text : "";

        if (SaveSystemManager.Instance.Login(user, pass))
        {
            if (passwordInput != null) passwordInput.text = "";
            RefreshPanels();
        }
    }

    public void OnClickRegister()
    {
        if (SaveSystemManager.Instance == null) return;

        string user = usernameInput != null ? usernameInput.text : "";
        string pass = passwordInput != null ? passwordInput.text : "";

        if (SaveSystemManager.Instance.RegisterAccount(user, pass))
        {
            if (passwordInput != null) passwordInput.text = "";
            RefreshPanels();
        }
    }

    public void OnClickLogout()
    {
        if (SaveSystemManager.Instance != null)
        {
            SaveSystemManager.Instance.Logout();
        }
        RefreshPanels();
    }

    public void OnClickNewGame()
    {
        if (SaveSystemManager.Instance == null || string.IsNullOrEmpty(SaveSystemManager.Instance.CurrentLoggedInUser))
            return;

        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.RestoreState(
                PortfolioManager.Instance.startingCash,
                PortfolioManager.Instance.startingCash,
                new List<SaveSystemManager.SavedPositionData>(),
                new List<SaveSystemManager.SavedTransactionData>()
            );
        }

        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.StartNewRandomRun();
        }

        string saveLabel = (newSaveNameInput != null && !string.IsNullOrWhiteSpace(newSaveNameInput.text))
            ? newSaveNameInput.text.Trim()
            : null;

        // Immediately create an initial save file for this new run
        SaveSystemManager.Instance.SaveGame(saveLabel);

        LoadTradingScene();
    }

    private void RebuildSaveSlotsList()
    {
        if (saveSlotsContainer == null || saveSlotRowPrefab == null || SaveSystemManager.Instance == null)
            return;

        foreach (Transform child in saveSlotsContainer)
        {
            Destroy(child.gameObject);
        }

        List<SaveSystemManager.PlayerSaveGameData> saves = SaveSystemManager.Instance.GetCurrentUserSaves();

        foreach (var save in saves)
        {
            GameObject row = Instantiate(saveSlotRowPrefab, saveSlotsContainer);

            // Expects a TMP_Text for save info and a Button to load the save
            TMP_Text label = row.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = $"{save.saveName} — Day {save.spoofedDay}/{save.maxTradingDays} | Cash: {save.currentCash:N0}";
            }

            Button loadBtn = row.GetComponentInChildren<Button>();
            if (loadBtn != null)
            {
                string capturedSaveId = save.saveId;
                loadBtn.onClick.RemoveAllListeners();
                loadBtn.onClick.AddListener(() =>
                {
                    if (SaveSystemManager.Instance.LoadGame(capturedSaveId))
                    {
                        LoadTradingScene();
                    }
                });
            }
        }
    }

    private void LoadTradingScene()
    {
        if (!string.IsNullOrWhiteSpace(tradingSceneName))
        {
            SceneManager.LoadScene(tradingSceneName);
        }
    }
}