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
    private Sprite pendingBackground;
    private Sprite _targetBackground;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }

    private void Start()
    {
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

        if (target == null)
        {
            Debug.LogError($"[BG ERROR] Target sprite is NULL for month {calendarMonth}.");
            return;
        }

        Sprite effectiveCurrent = pendingBackground ?? (isTransitioning ? _targetBackground : currentBackground);
        if (target == effectiveCurrent)
            return;

        if (isTransitioning)
        {
            pendingBackground = target;
            return;
        }

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
        _targetBackground = newSprite;
        pendingBackground = null;

        if (backgroundImage.sprite == null)
            backgroundImage.sprite = currentBackground;

        if (fadeOverlayImage == null)
        {
            backgroundImage.sprite = newSprite;
            currentBackground = newSprite;
            _targetBackground = null;
            isTransitioning = false;

            if (pendingBackground != null && pendingBackground != currentBackground)
            {
                Sprite queued = pendingBackground;
                pendingBackground = null;
                StartCoroutine(CrossfadeTo(queued));
            }

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

        _targetBackground = null;
        isTransitioning = false;

        if (pendingBackground != null && pendingBackground != currentBackground)
        {
            Sprite queued = pendingBackground;
            pendingBackground = null;
            StartCoroutine(CrossfadeTo(queued));
        }
    }
}