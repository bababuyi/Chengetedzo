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
    private bool isTransitioning = false;

    private void Start()
    {
        currentBackground = backgroundImage.sprite;
    }

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }

    public void UpdateForMonth(int calendarMonth, bool hasWeatherEvent)
    {
        if (isTransitioning) return;

        Sprite target = GetSeasonSprite(calendarMonth, hasWeatherEvent);

        if (target == currentBackground) return;

        Debug.Log($"[BG] Switching to: {target.name}");

        if (target == null)
        {
            Debug.LogError($"[BG ERROR] Target sprite is NULL for month {calendarMonth}");
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

        if (fadeOverlayImage == null)
        {
            backgroundImage.sprite = newSprite;
            currentBackground = newSprite;
            isTransitioning = false;
            yield break;
        }

        if (backgroundImage.sprite == null)
            backgroundImage.sprite = currentBackground;

        fadeOverlayImage.sprite = newSprite;

        Color c = fadeOverlayImage.color;
        c.a = 0f;
        fadeOverlayImage.color = c;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeOverlayImage.color = c;
            yield return null;
        }

        backgroundImage.sprite = newSprite;
        currentBackground = newSprite;

        fadeOverlayImage.color = new Color(1, 1, 1, 0);
        fadeOverlayImage.sprite = null;

        isTransitioning = false;
    }
}