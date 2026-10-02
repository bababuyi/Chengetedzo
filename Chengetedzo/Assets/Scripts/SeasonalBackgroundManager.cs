using System.Collections;
using UnityEngine;

// Composes the simulation background from four layers: sky, clouds, skyline, and
// treeline. Skyline and Treeline are still plain swapped sprites (see BackgroundLayer
// below). Sky and Clouds changed this round - they're no longer single static sprites,
// each season wants genuinely different behaviour there:
//
//   Rainy:  no discrete clouds at all. Sky itself scrolls (city_rainy_001, a storm front
//           moving across). Cloud layer and cloud spawner both off.
//   Winter: sky is static (city_winter_001). A separate ScrollingLayer carries the wispy
//           winter cloud sheet across it. Cloud spawner off.
//   Summer: sky is static (the summer gradient). Discrete clouds spawn and drift
//           (CloudSpawner, picking random slices from the summer cloud sheet). Cloud
//           layer scroller off.
//
// Rainy always wins over season for Sky/Clouds, same priority the old single-image
// background used - a storm over a green summer treeline is meant to be possible.
// Skyline keeps its own separate priority (winter beats rainy beats summer) and Treeline
// is season/dryness only - neither of those changed this round.
public class SeasonalBackgroundManager : MonoBehaviour
{
    // Skyline and Treeline only - Sky and Clouds moved to ScrollingLayer/CloudSpawner
    // below, since they need to scroll or spawn rather than just swap a sprite.
    // fadeRenderer is optional - leave it unassigned for an instant swap instead of a
    // crossfade. If assigned, it must sit on the same sorting layer as renderer with a
    // sortingOrder one above it, so the fade-in sprite draws on top while the old one
    // fades out underneath.
    [System.Serializable]
    public class BackgroundLayer
    {
        public SpriteRenderer renderer;
        public SpriteRenderer fadeRenderer;

        [System.NonSerialized] public Sprite current;
        [System.NonSerialized] public Sprite pending;
        [System.NonSerialized] public Sprite target;
        [System.NonSerialized] public bool isTransitioning;
        [System.NonSerialized] public Coroutine activeFade;
    }

    [Header("Sky - scrolls for rainy (storm front), static otherwise")]
    public ScrollingLayer skyScroller;
    public Sprite skyRainySprite;
    public float skyRainyScrollSpeed = 0.3f;
    public bool skyRainyScrollRight = true;
    public Sprite skyWinterSprite;
    public Sprite skySummerSprite;

    [Header("Cloud layer - winter's scrolling wispy cloud sheet only. Off for rainy and summer.")]
    public ScrollingLayer cloudLayerScroller;
    public Sprite cloudLayerWinterSprite;
    public float cloudLayerWinterScrollSpeed = 0.3f;
    public bool cloudLayerWinterScrollRight = true;

    [Header("Discrete clouds - summer only. CloudSpawner handles its own sprites/prefab. Off for rainy and winter.")]
    public CloudSpawner cloudSpawner;

    [Header("Skyline - season and weather graded (winter beats rainy beats summer)")]
    public BackgroundLayer skyline;
    public Sprite skylineSummerSprite;
    public Sprite skylineWinterSprite;
    public Sprite skylineRainySprite;

    [Header("Treeline - season/dryness only, weather never touches this layer")]
    public BackgroundLayer treeline;
    public Sprite treelineGreenSprite;
    public Sprite treelineMidSprite;
    public Sprite treelineDrySprite;

    // Only two treeline variants exist right now (summer/green, winter/dry), so the
    // default bands match GameManager's own winter window exactly and the mid band is
    // collapsed (start > end, so it never matches anything). Once a third, mid-dryness
    // sprite exists, split dryBand and set midBandStartMonth/midBandEndMonth to a real
    // range and treelineMidSprite will start showing up automatically, no code change needed.
    [Header("Dryness bands (calendar month, 1-12) - tune to taste, does not touch GameManager.Season")]
    public int dryBandStartMonth = 4;
    public int dryBandEndMonth = 8;
    public int midBandStartMonth = 0;
    public int midBandEndMonth = -1;

    [Header("Crossfade (Skyline/Treeline only)")]
    public float fadeDuration = 1.2f;

    private void Start()
    {
        int month = GameManager.Instance != null ? GameManager.Instance.currentMonth : 1;
        bool winter = IsWinterMonth(month);

        ApplySky(false, winter);
        ApplyCloudLayer(false, winter);
        ApplyCloudSpawner(false, winter);

        SetImmediate(skyline, GetSkylineSprite(month, false));
        SetImmediate(treeline, GetTreelineSprite(month));
    }

    public void UpdateForMonth(int calendarMonth, bool hasWeatherEvent)
    {
        bool winter = IsWinterMonth(calendarMonth);

        ApplySky(hasWeatherEvent, winter);
        ApplyCloudLayer(hasWeatherEvent, winter);
        ApplyCloudSpawner(hasWeatherEvent, winter);

        RequestSprite(skyline, GetSkylineSprite(calendarMonth, hasWeatherEvent));
        RequestSprite(treeline, GetTreelineSprite(calendarMonth));
    }

    private bool IsWinterMonth(int calendarMonth)
    {
        int month = ((calendarMonth - 1) % 12) + 1;
        return month >= 4 && month <= 8;
    }

    // Rainy wins outright over season, same as the old single-image sky did. Only
    // repositions the scroller's two copies when the sprite actually changes, so speed/
    // direction tweaks (or a repeat call for the same condition) don't cause a visible snap.
    private void ApplySky(bool hasWeatherEvent, bool winter)
    {
        if (skyScroller == null) return;

        Sprite target;
        float speed;
        bool right;

        if (hasWeatherEvent && skyRainySprite != null)
        {
            target = skyRainySprite;
            speed = skyRainyScrollSpeed;
            right = skyRainyScrollRight;
        }
        else
        {
            target = winter ? skyWinterSprite : skySummerSprite;
            speed = 0f;
            right = true;
        }

        if (target != null && skyScroller.sprite != target)
            skyScroller.ApplySprite(target);
        skyScroller.speed = speed;
        skyScroller.moveRight = right;
    }

    // Winter only. Off (GameObject disabled) for rainy and summer.
    private void ApplyCloudLayer(bool hasWeatherEvent, bool winter)
    {
        if (cloudLayerScroller == null) return;

        bool active = !hasWeatherEvent && winter;
        cloudLayerScroller.gameObject.SetActive(active);

        if (active && cloudLayerWinterSprite != null)
        {
            if (cloudLayerScroller.sprite != cloudLayerWinterSprite)
                cloudLayerScroller.ApplySprite(cloudLayerWinterSprite);
            cloudLayerScroller.speed = cloudLayerWinterScrollSpeed;
            cloudLayerScroller.moveRight = cloudLayerWinterScrollRight;
        }
    }

    // Summer only. Off (GameObject disabled, stops its spawn coroutine) for rainy and winter.
    private void ApplyCloudSpawner(bool hasWeatherEvent, bool winter)
    {
        if (cloudSpawner == null) return;

        bool active = !hasWeatherEvent && !winter;
        cloudSpawner.gameObject.SetActive(active);
    }

    private Sprite GetSkylineSprite(int calendarMonth, bool hasWeatherEvent)
    {
        if (IsWinterMonth(calendarMonth)) return skylineWinterSprite;
        if (hasWeatherEvent) return skylineRainySprite;
        return skylineSummerSprite;
    }

    private Sprite GetTreelineSprite(int calendarMonth)
    {
        int month = ((calendarMonth - 1) % 12) + 1;
        if (month >= dryBandStartMonth && month <= dryBandEndMonth) return treelineDrySprite;
        if (month >= midBandStartMonth && month <= midBandEndMonth) return treelineMidSprite;
        return treelineGreenSprite;
    }

    private void SetImmediate(BackgroundLayer layer, Sprite sprite)
    {
        if (layer == null || layer.renderer == null) return;

        if (sprite != null)
        {
            layer.renderer.sprite = sprite;
            layer.current = sprite;
        }
        else
        {
            // No sprite assigned yet for this condition (art still pending) - leave
            // whatever is on the renderer already rather than blanking it.
            layer.current = layer.renderer.sprite;
        }

        if (layer.fadeRenderer != null)
        {
            layer.fadeRenderer.color = new Color(1f, 1f, 1f, 0f);
            layer.fadeRenderer.enabled = false;
        }
    }

    // If target is null (art not supplied for this condition yet) this quietly leaves the
    // layer showing whatever it last had, rather than erroring or blanking the screen.
    private void RequestSprite(BackgroundLayer layer, Sprite target)
    {
        if (layer == null || layer.renderer == null || target == null)
            return;

        if (layer.isTransitioning && layer.fadeRenderer != null && layer.fadeRenderer.color.a > 0f)
        {
            if (layer.activeFade != null) StopCoroutine(layer.activeFade);
            layer.isTransitioning = false;
            layer.renderer.sprite = layer.target != null ? layer.target : layer.current;
            layer.current = layer.renderer.sprite;
            layer.fadeRenderer.color = new Color(1f, 1f, 1f, 0f);
            layer.fadeRenderer.sprite = null;
            layer.pending = null;
            layer.target = null;
        }

        Sprite effectiveCurrent = layer.pending ??
            (layer.isTransitioning ? layer.target : layer.current);

        if (target == effectiveCurrent) return;

        if (layer.isTransitioning)
        {
            layer.pending = target;
            return;
        }

        layer.activeFade = StartCoroutine(CrossfadeTo(layer, target));
    }

    private IEnumerator CrossfadeTo(BackgroundLayer layer, Sprite newSprite)
    {
        layer.isTransitioning = true;
        layer.target = newSprite;
        layer.pending = null;

        if (layer.fadeRenderer == null)
        {
            layer.renderer.sprite = newSprite;
            layer.current = newSprite;
            layer.target = null;
            layer.isTransitioning = false;

            if (layer.pending != null && layer.pending != layer.current)
            {
                Sprite queued = layer.pending;
                layer.pending = null;
                layer.activeFade = StartCoroutine(CrossfadeTo(layer, queued));
            }

            yield break;
        }

        layer.fadeRenderer.enabled = true;
        layer.fadeRenderer.sprite = newSprite;
        layer.fadeRenderer.color = new Color(1f, 1f, 1f, 0f);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Clamp01(elapsed / fadeDuration);
            layer.fadeRenderer.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }

        layer.renderer.sprite = newSprite;
        layer.current = newSprite;

        layer.fadeRenderer.color = new Color(1f, 1f, 1f, 0f);
        layer.fadeRenderer.sprite = null;
        layer.fadeRenderer.enabled = false;

        layer.target = null;
        layer.isTransitioning = false;

        if (layer.pending != null && layer.pending != layer.current)
        {
            Sprite queued = layer.pending;
            layer.pending = null;
            layer.activeFade = StartCoroutine(CrossfadeTo(layer, queued));
        }
    }
}
