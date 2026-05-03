using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SeasonalBackgroundManager : MonoBehaviour
{
    [Header("Background Sprites")]
    [SerializeField] private Sprite rainyBackground;
    [SerializeField] private Sprite summerBackground;
    [SerializeField] private Sprite winterBackground;

    [Header("Crossfade")]
    [SerializeField] private float fadeDuration = 1.2f;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image fadeOverlayImage;

    private Sprite currentBackground;
    private Sprite pendingBackground;   // queued while a transition is running
    private bool isTransitioning = false;

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }

    private void Start()
    {
        // Initialise to the correct sprite for the current month immediately,
        // with no crossfade, so the screen is never blank at game start.
        int month = GameManager.Instance != null ? GameManager.Instance.currentMonth : 1;
        Sprite initial = GetSeasonSprite(month, false);

        if (initial != null)
        {
            backgroundImage.sprite = initial;
            currentBackground = initial;
        }
        else
        {
            currentBackground = backgroundImage.sprite;
        }

        if (fadeOverlayImage != null)
            fadeOverlayImage.color = new Color(1f, 1f, 1f, 0f);
    }

    public void UpdateForMonth(int calendarMonth, bool hasWeatherEvent)
    {
        Sprite target = GetSeasonSprite(calendarMonth, hasWeatherEvent);

        Debug.Log($"[BG] UpdateForMonth called | month={calendarMonth} | hasWeather={hasWeatherEvent} | target={(target != null ? target.name : "NULL")} | currentBG={(currentBackground != null ? currentBackground.name : "NULL")} | bgImage={(backgroundImage != null ? backgroundImage.gameObject.activeSelf.ToString() : "NULL")} | fadeOverlay={(fadeOverlayImage != null ? fadeOverlayImage.gameObject.activeSelf.ToString() : "NULL")}");

        if (target == null)
        {
            Debug.LogError($"[BG ERROR] Target sprite is NULL for month {calendarMonth}. Check Inspector assignments.");
            return;
        }

        if (target == currentBackground)
        {
            Debug.Log($"[BG] Same sprite as current — skipping transition.");
            return;
        }

        if (isTransitioning)
        {
            pendingBackground = target;
            Debug.Log($"[BG] Transition in progress — queued: {target.name}");
            return;
        }

        Debug.Log($"[BG] Switching to: {target.name}");
        StartCoroutine(CrossfadeTo(target));
    }

    private Sprite GetSeasonSprite(int calendarMonth, bool hasWeatherEvent)
    {
        int month = ((calendarMonth - 1) % 12) + 1;

        if (month >= 4 && month <= 8)
            return winterBackground;

        if (hasWeatherEvent)
            return rainyBackground;

        return summerBackground;
    }

    private IEnumerator CrossfadeTo(Sprite newSprite)
    {
        isTransitioning = true;
        pendingBackground = null;

        Debug.Log($"[BG-FADE] CrossfadeTo START | newSprite={newSprite.name} | bgImage.sprite={(backgroundImage.sprite != null ? backgroundImage.sprite.name : "NULL")} | bgImage.active={backgroundImage.gameObject.activeSelf} | bgImage.enabled={backgroundImage.enabled}");

        // Ensure the background image always has something visible as a base...NOT WORKING AHHHHHHHHH
        if (backgroundImage.sprite == null)
            backgroundImage.sprite = currentBackground;

        if (fadeOverlayImage == null)
        {
            backgroundImage.sprite = newSprite;
            currentBackground = newSprite;

            Debug.Log($"[BG-FADE] CrossfadeTo END | backgroundImage.sprite={backgroundImage.sprite.name} | active={backgroundImage.gameObject.activeSelf} | enabled={backgroundImage.enabled}");

            isTransitioning = false;
            yield break;
        }

        fadeOverlayImage.sprite = newSprite;
        fadeOverlayImage.color = new Color(1f, 1f, 1f, 0f);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeOverlayImage.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }

        backgroundImage.sprite = newSprite;
        currentBackground = newSprite;

        fadeOverlayImage.color = new Color(1f, 1f, 1f, 0f);
        fadeOverlayImage.sprite = null;

        isTransitioning = false;

        // If a new target arrived while we were transitioning, run it now.
        if (pendingBackground != null && pendingBackground != currentBackground)
        {
            Sprite queued = pendingBackground;
            pendingBackground = null;
            Debug.Log($"[BG] Running queued transition to: {queued.name}");
            StartCoroutine(CrossfadeTo(queued));
        }
    }
}