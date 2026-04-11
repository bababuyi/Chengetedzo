using System.Collections;
using UnityEngine;

public class UIAnimator : MonoBehaviour
{
    public static UIAnimator Instance;

    [Header("Panel Fades")]
    [Tooltip("Standard panel fade-in duration. Keep short — this game is information-dense.")]
    public float panelFadeDuration = 0.15f;
    public float panelFadeOutDuration = 0.10f;

    [Header("Event Popup — Neutral Alert Card")]
    [Tooltip("The event popup fades in and settles very slightly downward. No bounce.")]
    public float eventPopupFadeDuration = 0.20f;
    [Tooltip("How far above resting position the popup starts (canvas units). Keep small.")]
    public float eventPopupDropAmount = 10f;

    [Header("Mentor / Loan Chat — Slide Up from Bottom")]
    [Tooltip("Chat-style panels slide up from below, like a messaging notification.")]
    public float chatSlideDuration = 0.22f;
    [Tooltip("How far below resting the panel starts (canvas units).")]
    public float chatSlideAmount = 50f;

    [Header("Forecast Cards")]
    [Tooltip("Delay between each card appearing.")]
    public float forecastStagger = 0.10f;
    [Tooltip("Each card's slide duration.")]
    public float forecastSlideDuration = 0.16f;
    [Tooltip("Cards start this far to the right of their resting position.")]
    public float forecastSlideOffsetX = 70f;

    [Header("Money Text Punch")]
    [Tooltip("Scale up briefly when money changes. Keep subtle — 1.12 max.")]
    public float moneyPunchDuration = 0.22f;
    [Range(1.02f, 1.15f)]
    public float moneyPunchScale = 1.10f;
    [Tooltip("Muted green for gain. The art doc says no extreme saturation.")]
    public Color moneyGainColor = new Color(0.45f, 0.78f, 0.50f);
    [Tooltip("Muted red for loss.")]
    public Color moneyLossColor = new Color(0.80f, 0.35f, 0.35f);

    [Header("Bubble Scale In")]
    [Tooltip("Used for chat bubbles and choice results.")]
    public float bubbleScaleDuration = 0.18f;
    [Range(1.02f, 1.15f)]
    public float bubbleOvershootScale = 1.08f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    public void FadeIn(GameObject panel, System.Action onComplete = null)
    {
        if (panel == null) return;
        var cg = GetOrAddCanvasGroup(panel);
        StartCoroutine(FadeCoroutine(cg, 0f, 1f, panelFadeDuration, onComplete));
    }

    public void FadeOut(GameObject panel, System.Action onComplete = null)
    {
        if (panel == null) return;
        var cg = GetOrAddCanvasGroup(panel);
        StartCoroutine(FadeCoroutine(cg, 1f, 0f, panelFadeOutDuration, () =>
        {
            panel.SetActive(false);
            onComplete?.Invoke();
        }));
    }

    public void ShowEventPopup(GameObject popup, System.Action onComplete = null)
    {
        if (popup == null) return;
        var cg = GetOrAddCanvasGroup(popup);
        StartCoroutine(FadeDropCoroutine(popup.transform as RectTransform,
                                         cg, onComplete));
    }

    public void SlideUpChat(RectTransform panel, System.Action onComplete = null)
    {
        if (panel == null) return;
        var cg = GetOrAddCanvasGroup(panel.gameObject);
        StartCoroutine(ChatSlideCoroutine(panel, cg, onComplete));
    }

    public void StaggerForecastCards(Transform forecastContainer,
                                     System.Action onComplete = null)
    {
        if (forecastContainer == null) return;
        StartCoroutine(StaggerCoroutine(forecastContainer, onComplete));
    }

    public void PunchMoneyText(RectTransform moneyRect,
                               bool isGain,
                               TMPro.TMP_Text colorTarget = null)
    {
        if (moneyRect == null) return;
        StartCoroutine(PunchCoroutine(moneyRect, isGain, colorTarget));
    }

    private IEnumerator FadeCoroutine(CanvasGroup cg, float from, float to,
                                       float duration, System.Action onComplete)
    {
        cg.alpha = from;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
        onComplete?.Invoke();
    }

    private IEnumerator FadeDropCoroutine(RectTransform rect, CanvasGroup cg,
                                           System.Action onComplete)
    {
        if (rect == null) { cg.alpha = 1f; onComplete?.Invoke(); yield break; }

        Vector2 rest = rect.anchoredPosition;
        Vector2 start = rest + new Vector2(0f, eventPopupDropAmount);

        rect.anchoredPosition = start;
        cg.alpha = 0f;
        float t = 0f;

        while (t < eventPopupFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOutCubic(t / eventPopupFadeDuration);
            rect.anchoredPosition = Vector2.Lerp(start, rest, p);
            cg.alpha = Mathf.Clamp01(t / (eventPopupFadeDuration * 0.6f));
            yield return null;
        }

        rect.anchoredPosition = rest;
        cg.alpha = 1f;
        onComplete?.Invoke();
    }

    private IEnumerator ChatSlideCoroutine(RectTransform rect, CanvasGroup cg,
                                            System.Action onComplete)
    {
        Vector2 rest = rect.anchoredPosition;
        Vector2 start = rest - new Vector2(0f, chatSlideAmount);

        rect.anchoredPosition = start;
        cg.alpha = 0f;
        float t = 0f;

        while (t < chatSlideDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOutCubic(t / chatSlideDuration);
            rect.anchoredPosition = Vector2.Lerp(start, rest, p);
            cg.alpha = Mathf.Clamp01(t / (chatSlideDuration * 0.5f));
            yield return null;
        }

        rect.anchoredPosition = rest;
        cg.alpha = 1f;
        onComplete?.Invoke();
    }

    private IEnumerator StaggerCoroutine(Transform parent, System.Action onComplete)
    {
        int count = parent.childCount;

        for (int i = 0; i < count; i++)
        {
            StartCoroutine(SlideInCardCoroutine(parent.GetChild(i)));
            AudioManager.Instance?.OnForecastCard();

            if (i < count - 1)
                yield return new WaitForSeconds(forecastStagger);
        }

        yield return new WaitForSeconds(forecastSlideDuration);
        onComplete?.Invoke();
    }

    private IEnumerator SlideInCardCoroutine(Transform card)
    {
        var rect = card as RectTransform;
        if (rect == null) yield break;

        var cg = GetOrAddCanvasGroup(card.gameObject);
        Vector2 rest = rect.anchoredPosition;
        Vector2 start = rest + new Vector2(forecastSlideOffsetX, 0f);

        rect.anchoredPosition = start;
        cg.alpha = 0f;
        float t = 0f;

        while (t < forecastSlideDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOutCubic(t / forecastSlideDuration);
            rect.anchoredPosition = Vector2.Lerp(start, rest, p);
            cg.alpha = Mathf.Clamp01(t / (forecastSlideDuration * 0.5f));
            yield return null;
        }

        rect.anchoredPosition = rest;
        cg.alpha = 1f;
    }

    private IEnumerator PunchCoroutine(RectTransform rect, bool isGain,
                                        TMPro.TMP_Text colorTarget)
    {
        if (colorTarget != null)
            colorTarget.color = isGain ? moneyGainColor : moneyLossColor;

        float half = moneyPunchDuration * 0.45f;
        float settle = moneyPunchDuration - half;
        float t = 0f;

        // Grow
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            rect.localScale = Vector3.one * Mathf.Lerp(1f, moneyPunchScale,
                                EaseOutCubic(t / half));
            yield return null;
        }

        t = 0f;

        // Return
        while (t < settle)
        {
            t += Time.unscaledDeltaTime;
            rect.localScale = Vector3.one * Mathf.Lerp(moneyPunchScale, 1f,
                                EaseOutCubic(t / settle));
            yield return null;
        }

        rect.localScale = Vector3.one;

        // Fade colour
        if (colorTarget != null)
        {
            t = 0f;
            float fadeDur = 0.5f;
            Color startCol = colorTarget.color;

            while (t < fadeDur)
            {
                t += Time.unscaledDeltaTime;
                colorTarget.color = Color.Lerp(startCol, Color.white, t / fadeDur);
                yield return null;
            }

            colorTarget.color = Color.white;
        }
    }

    public void ScaleBubbleIn(RectTransform rect, System.Action onComplete = null)
    {
        if (rect == null) return;
        StartCoroutine(ScaleBubbleCoroutine(rect, onComplete));
    }

    private IEnumerator ScaleBubbleCoroutine(RectTransform rect, System.Action onComplete)
    {
        Vector3 start = Vector3.one * 0.92f;
        Vector3 overshoot = Vector3.one * bubbleOvershootScale;
        Vector3 end = Vector3.one;

        float t = 0f;
        float half = bubbleScaleDuration * 0.6f;
        float settle = bubbleScaleDuration - half;

        rect.localScale = start;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOutCubic(t / half);
            rect.localScale = Vector3.Lerp(start, overshoot, p);
            yield return null;
        }

        t = 0f;

        while (t < settle)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOutCubic(t / settle);
            rect.localScale = Vector3.Lerp(overshoot, end, p);
            yield return null;
        }

        rect.localScale = end;
        onComplete?.Invoke();
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }
}