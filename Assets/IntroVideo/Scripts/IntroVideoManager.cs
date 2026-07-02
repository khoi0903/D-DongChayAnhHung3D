using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Intro
{
    [DisallowMultipleComponent]
    public sealed class IntroVideoManager : MonoBehaviour
    {
        [Header("Scene Flow")]
        [SerializeField] private string targetSceneName = "S03";
        [SerializeField] private bool playOnStart;

        [Header("Videos")]
        [SerializeField] private VideoClip[] introClips = System.Array.Empty<VideoClip>();

        [Header("Input")]
        [SerializeField] private KeyCode skipCurrentKey = KeyCode.Space;
        [SerializeField] private KeyCode skipAllKey = KeyCode.Escape;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.35f;
        [SerializeField, Min(0.5f)] private float prepareTimeout = 10f;

        [Header("Optional References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button skipAllButton;
        [SerializeField] private RawImage videoDisplay;
 
        private RenderTexture videoTexture;
        private Coroutine sequenceRoutine;
        private bool clipFinished;
        private bool isLoadingScene;
        private bool isPlayingSequence;
        private bool skipCurrentRequested;

        public VideoClip[] IntroClips
        {
            get => introClips;
            set => introClips = value;
        }

        public string TargetSceneName
        {
            get => targetSceneName;
            set => targetSceneName = value;
        }

        public bool PlayOnStart
        {
            get => playOnStart;
            set => playOnStart = value;
        }

        private void Awake()
        {
            EnsureComponents();
            ConfigureVideoPlayer();
            EnsureMinimalUi();
            BindButtons();
            ShowOverlay(false);
        }

        private void Start()
        {
            if (playOnStart)
                PlayIntro();
        }

        private void OnEnable()
        {
            if (videoPlayer != null)
                videoPlayer.loopPointReached += HandleVideoFinished;
        }

        private void OnDisable()
        {
            if (videoPlayer != null)
                videoPlayer.loopPointReached -= HandleVideoFinished;

            if (videoPlayer != null && videoPlayer.isPlaying)
                videoPlayer.Stop();

            if (videoTexture != null)
            {
                if (videoPlayer != null)
                    videoPlayer.targetTexture = null;
                if (videoDisplay != null)
                    videoDisplay.texture = null;
                videoTexture.Release();
                Destroy(videoTexture);
                videoTexture = null;
            }
        }

        private void Update()
        {
            if (!isPlayingSequence || isLoadingScene)
                return;

            if (Input.GetKeyDown(skipAllKey))
            {
                SkipAllIntro();
                return;
            }

            if (Input.GetKeyDown(skipCurrentKey))
                SkipCurrentVideo();
        }

        public void PlayIntro(string nextSceneName = null)
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                targetSceneName = nextSceneName;
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (!enabled)
                enabled = true;

            if (sequenceRoutine != null)
                StopCoroutine(sequenceRoutine);

            sequenceRoutine = StartCoroutine(PlaySequence());
        }

        public void SkipCurrentVideo()
        {
            if (!isPlayingSequence || isLoadingScene)
                return;

            skipCurrentRequested = true;

            if (videoPlayer != null)
                videoPlayer.Stop();
        }

        public void SkipAllIntro()
        {
            if (isLoadingScene)
                return;

            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            StartCoroutine(LoadTargetScene());
        }

        private IEnumerator PlaySequence()
        {
            isPlayingSequence = true;
            isLoadingScene = false;
            ShowOverlay(true);

            SetFade(1f);

            if (introClips == null || introClips.Length == 0)
            {
                yield return LoadTargetScene();
                yield break;
            }

            for (int i = 0; i < introClips.Length; i++)
            {
                VideoClip clip = introClips[i];
                if (clip == null)
                    continue;

                yield return PlayClip(clip);

                if (isLoadingScene)
                    yield break;
            }

            yield return LoadTargetScene();
        }

        private IEnumerator PlayClip(VideoClip clip)
        {
            clipFinished = false;
            skipCurrentRequested = false;

            videoPlayer.Stop();
            videoPlayer.clip = clip;

            int width = clip.width > 0 ? (int)clip.width : 1920;
            int height = clip.height > 0 ? (int)clip.height : 1080;

            if (videoTexture != null)
            {
                videoPlayer.targetTexture = null;
                if (videoDisplay != null) videoDisplay.texture = null;
                videoTexture.Release();
                Destroy(videoTexture);
            }

            videoTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            videoTexture.Create();

            videoPlayer.targetTexture = videoTexture;
            if (videoDisplay != null) videoDisplay.texture = videoTexture;

            videoPlayer.Prepare();

            float prepareStartedAt = Time.unscaledTime;
            while (!videoPlayer.isPrepared && Time.unscaledTime - prepareStartedAt < prepareTimeout)
                yield return null;

            if (!videoPlayer.isPrepared)
                yield break;

            videoPlayer.Play();

            yield return FadeTo(0f);

            while (!clipFinished && !skipCurrentRequested && !isLoadingScene)
                yield return null;

            yield return FadeTo(1f);
            videoPlayer.Stop();
        }

        private IEnumerator LoadTargetScene()
        {
            isLoadingScene = true;
            isPlayingSequence = false;
            skipCurrentRequested = false;

            if (videoPlayer != null)
                videoPlayer.Stop();

            ShowOverlay(true);
            yield return FadeTo(1f);

            Time.timeScale = 1f;
            SceneManager.LoadScene(targetSceneName);
        }

        private void EnsureComponents()
        {
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
                videoPlayer = gameObject.AddComponent<VideoPlayer>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void ConfigureVideoPlayer()
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }

        private void ResolveTargetCamera()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera == null)
                targetCamera = CreateFallbackCamera();

            videoPlayer.targetCamera = targetCamera;
        }

        private Camera CreateFallbackCamera()
        {
            GameObject cameraObject = new GameObject("IntroVideoCamera");
            cameraObject.transform.SetParent(transform, false);

            Camera cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = Color.black;
            cameraComponent.cullingMask = 0;
            cameraComponent.depth = 100f;
            return cameraComponent;
        }

        private void EnsureMinimalUi()
        {
            if (rootGroup != null && fadeGroup != null && skipButton != null && skipAllButton != null && videoDisplay != null)
                return;

            GameObject canvasObject = new GameObject("IntroVideoCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            rootGroup = canvasObject.GetComponent<CanvasGroup>();

            if (videoDisplay == null)
                videoDisplay = CreateVideoDisplay(canvasObject.transform);
            else
                videoDisplay.transform.SetParent(canvasObject.transform, false);

            fadeGroup = CreateFade(canvasObject.transform);
            skipButton = CreateButton(canvasObject.transform, "SkipButton", "Skip", new Vector2(1f, 0f), new Vector2(-176f, 56f));
            skipAllButton = CreateButton(canvasObject.transform, "SkipAllButton", "Skip All", new Vector2(1f, 0f), new Vector2(-56f, 56f));

            EnsureEventSystem();
        }

        private static RawImage CreateVideoDisplay(Transform parent)
        {
            GameObject displayObject = new GameObject("VideoDisplay", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            displayObject.transform.SetParent(parent, false);

            RectTransform rect = displayObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage rawImage = displayObject.GetComponent<RawImage>();
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;

            return rawImage;
        }

        private static CanvasGroup CreateFade(Transform parent)
        {
            GameObject fadeObject = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            fadeObject.transform.SetParent(parent, false);

            RectTransform rect = fadeObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = fadeObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            return fadeObject.GetComponent<CanvasGroup>();
        }

        private static Button CreateButton(Transform parent, string objectName, string label, Vector2 anchor, Vector2 position)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(104f, 40f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.62f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0f, 0f, 0f, 0.62f);
            colors.highlightedColor = new Color(0.14f, 0.14f, 0.14f, 0.82f);
            colors.pressedColor = new Color(0.24f, 0.24f, 0.24f, 0.92f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0f, 0f, 0f, 0.25f);
            button.colors = colors;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text text = labelObject.GetComponent<Text>();
            text.text = label;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 16;
            text.raycastTarget = false;

            Font builtinFont = null;
            try
            {
                builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                try
                {
                    builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                catch
                {
                    // Fallback to default UI font if both fail
                }
            }
            text.font = builtinFont;

            return button;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void BindButtons()
        {
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(SkipCurrentVideo);
                skipButton.onClick.AddListener(SkipCurrentVideo);
            }

            if (skipAllButton != null)
            {
                skipAllButton.onClick.RemoveListener(SkipAllIntro);
                skipAllButton.onClick.AddListener(SkipAllIntro);
            }
        }

        private void HandleVideoFinished(VideoPlayer source)
        {
            if (source == videoPlayer)
                clipFinished = true;
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            if (fadeGroup == null || fadeDuration <= 0f)
            {
                SetFade(targetAlpha);
                yield break;
            }

            float startAlpha = fadeGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            fadeGroup.alpha = targetAlpha;
        }

        private void SetFade(float alpha)
        {
            if (fadeGroup != null)
                fadeGroup.alpha = alpha;
        }

        private void ShowOverlay(bool visible)
        {
            if (rootGroup == null)
                return;

            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.interactable = visible;
            rootGroup.blocksRaycasts = visible;
        }
    }
}
