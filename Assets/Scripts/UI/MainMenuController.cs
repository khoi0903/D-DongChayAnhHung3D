using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField] private string startSceneName = "S01_CityPrototype";

    [Header("Intro")]
    [SerializeField] private CanvasGroup blackFade;
    [SerializeField] private CanvasGroup logoGroup;
    [SerializeField] private CanvasGroup menuGroup;
    [SerializeField] private CanvasGroup footerGroup;
    [SerializeField] private RectTransform swordRoot;
    [SerializeField] private float introDuration = 1.6f;

    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button achievementsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Button achievementsCloseButton;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject achievementsPanel;

    [Header("Text")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text versionText;

    [Header("Audio")]
    [SerializeField] private bool playIntroDrum = true;
    [SerializeField, Range(0f, 1f)] private float introDrumVolume = 0.45f;

    private bool isTransitioning;
    private Vector2 swordStartPosition;
    private Vector2 swordEndPosition;
    private AudioSource audioSource;
    private AudioClip introDrumClip;

    public void Configure(
        CanvasGroup blackFade,
        CanvasGroup logoGroup,
        CanvasGroup menuGroup,
        CanvasGroup footerGroup,
        RectTransform swordRoot,
        Button startButton,
        Button settingsButton,
        Button achievementsButton,
        Button exitButton,
        Button settingsCloseButton,
        Button achievementsCloseButton,
        GameObject settingsPanel,
        GameObject achievementsPanel,
        TMP_Text statusText,
        TMP_Text versionText,
        string startSceneName)
    {
        this.blackFade = blackFade;
        this.logoGroup = logoGroup;
        this.menuGroup = menuGroup;
        this.footerGroup = footerGroup;
        this.swordRoot = swordRoot;
        this.startButton = startButton;
        this.settingsButton = settingsButton;
        this.achievementsButton = achievementsButton;
        this.exitButton = exitButton;
        this.settingsCloseButton = settingsCloseButton;
        this.achievementsCloseButton = achievementsCloseButton;
        this.settingsPanel = settingsPanel;
        this.achievementsPanel = achievementsPanel;
        this.statusText = statusText;
        this.versionText = versionText;
        this.startSceneName = startSceneName;
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        BindButtons();

        if (versionText != null)
            versionText.text = "v1.0.0";

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);

        if (swordRoot != null)
        {
            swordEndPosition = swordRoot.anchoredPosition;
            swordStartPosition = swordEndPosition + new Vector2(-18f, 220f);
            swordRoot.anchoredPosition = swordStartPosition;
        }

        SetGroup(blackFade, 1f, false);
        SetGroup(logoGroup, 0f, false);
        SetGroup(menuGroup, 0f, false);
        SetGroup(footerGroup, 0f, false);
    }

    private IEnumerator Start()
    {
        PlayIntroDrum();
        SetStatus("Âm trống Đông Sơn vang lên...");

        yield return FadeGroup(blackFade, 1f, 0f, introDuration * 0.45f);
        yield return FadeGroup(logoGroup, 0f, 1f, introDuration * 0.35f);
        yield return AnimateSwordDrop(introDuration * 0.35f);
        yield return FadeGroup(menuGroup, 0f, 1f, introDuration * 0.3f);
        yield return FadeGroup(footerGroup, 0f, 1f, introDuration * 0.2f);

        SetGroup(menuGroup, 1f, true);
        SetStatus("Chọn BẮT ĐẦU để vào game.");

        if (startButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (achievementsPanel != null && achievementsPanel.activeSelf)
        {
            CloseAchievements();
            return;
        }

        ExitGame();
    }

    public void StartGame()
    {
        if (isTransitioning)
            return;

        StartCoroutine(LoadStartScene());
    }

    public void OpenSettings()
    {
        ShowPanel(settingsPanel, true);
        ShowPanel(achievementsPanel, false);
        SetStatus("Cài đặt sẽ nối âm lượng, đồ họa và điều khiển ở bước sau.");
    }

    public void CloseSettings()
    {
        ShowPanel(settingsPanel, false);
        SetStatus("Chọn BẮT ĐẦU để vào game.");
        SelectStartButton();
    }

    public void OpenAchievements()
    {
        ShowPanel(achievementsPanel, true);
        ShowPanel(settingsPanel, false);
        SetStatus("Thành tựu sẽ dùng cho tiến trình sau này.");
    }

    public void CloseAchievements()
    {
        ShowPanel(achievementsPanel, false);
        SetStatus("Chọn BẮT ĐẦU để vào game.");
        SelectStartButton();
    }

    public void ExitGame()
    {
        SetStatus("Thoát game.");

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (achievementsButton != null)
        {
            achievementsButton.onClick.RemoveListener(OpenAchievements);
            achievementsButton.onClick.AddListener(OpenAchievements);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitGame);
            exitButton.onClick.AddListener(ExitGame);
        }

        if (settingsCloseButton != null)
        {
            settingsCloseButton.onClick.RemoveListener(CloseSettings);
            settingsCloseButton.onClick.AddListener(CloseSettings);
        }

        if (achievementsCloseButton != null)
        {
            achievementsCloseButton.onClick.RemoveListener(CloseAchievements);
            achievementsCloseButton.onClick.AddListener(CloseAchievements);
        }
    }

    private IEnumerator LoadStartScene()
    {
        isTransitioning = true;
        SetGroup(menuGroup, 1f, false);
        SetStatus("Đang vào game...");

        yield return FadeGroup(blackFade, blackFade != null ? blackFade.alpha : 0f, 1f, 0.55f);

        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }

    private IEnumerator AnimateSwordDrop(float duration)
    {
        if (swordRoot == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            swordRoot.anchoredPosition = Vector2.LerpUnclamped(swordStartPosition, swordEndPosition, t);
            swordRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-8f, 0f, t));
            yield return null;
        }

        swordRoot.anchoredPosition = swordEndPosition;
        swordRoot.localRotation = Quaternion.identity;
    }

    private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    private void ShowPanel(GameObject panel, bool show)
    {
        if (panel != null)
            panel.SetActive(show);
    }

    private void SelectStartButton()
    {
        if (startButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void PlayIntroDrum()
    {
        if (!playIntroDrum || audioSource == null)
            return;

        if (introDrumClip == null)
            introDrumClip = CreateIntroDrumClip();

        audioSource.PlayOneShot(introDrumClip, introDrumVolume);
    }

    private static AudioClip CreateIntroDrumClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.85f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float attack = Mathf.Clamp01(t / 0.018f);
            float decay = Mathf.Exp(-5.4f * t);
            float pitch = Mathf.Lerp(96f, 48f, t / duration);
            float body = Mathf.Sin(2f * Mathf.PI * pitch * t);
            float lowBody = Mathf.Sin(2f * Mathf.PI * 54f * t) * 0.45f;
            float noise = (Mathf.PerlinNoise(t * 92f, 0.37f) - 0.5f) * 0.22f;
            data[i] = (body * 0.75f + lowBody + noise) * attack * decay;
        }

        AudioClip clip = AudioClip.Create("MainMenu_DongSonDrum", sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static void SetGroup(CanvasGroup group, float alpha, bool interactive)
    {
        if (group == null)
            return;

        group.alpha = alpha;
        group.interactable = interactive;
        group.blocksRaycasts = interactive;
    }
}
