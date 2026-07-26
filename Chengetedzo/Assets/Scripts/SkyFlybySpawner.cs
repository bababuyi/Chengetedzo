using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ambient skyline life: an occasional airliner, a rare helicopter, and
/// birds lifting off the treeline. Runs forever in the background scene.
/// No prefabs needed — spawns UI Images from assigned sprites at runtime.
/// Follows the same canvas-space conventions as CloudSpawner.
/// </summary>
public class SkyFlybySpawner : MonoBehaviour
{
    [Header("Sprites (face right)")]
    public Sprite planeSprite;
    public Sprite helicopterSprite;
    public Sprite birdsSprite;

    [Header("Boundaries (reuse CloudSpawner's)")]
    public RectTransform spawnLeft;
    public RectTransform spawnRight;

    [Header("Plane — occasional")]
    public Vector2 planeIntervalRange = new Vector2(50f, 140f);
    public Vector2 planeSpeedRange = new Vector2(55f, 80f);
    [Range(0f, 1f)] public float planeBandTop = 0.86f;    // fraction of sky height
    [Range(0f, 1f)] public float planeBandBottom = 0.68f;
    public float planeScale = 0.55f;

    [Header("Helicopter — rare (the President is busy)")]
    public Vector2 heliIntervalRange = new Vector2(200f, 480f);
    public Vector2 heliSpeedRange = new Vector2(35f, 55f);
    [Range(0f, 1f)] public float heliBandTop = 0.62f;
    [Range(0f, 1f)] public float heliBandBottom = 0.45f;
    public float heliScale = 0.5f;

    [Header("Birds — from the trees")]
    public Vector2 birdIntervalRange = new Vector2(35f, 90f);
    public Vector2 birdSpeedRange = new Vector2(18f, 30f);
    public float birdLifetime = 9f;
    public float birdScale = 0.6f;

    private void Start()
    {
        StartCoroutine(SpawnLoop(planeSprite, planeIntervalRange, SpawnCrossing_Plane));
        StartCoroutine(SpawnLoop(helicopterSprite, heliIntervalRange, SpawnCrossing_Heli));
        StartCoroutine(SpawnLoop(birdsSprite, birdIntervalRange, SpawnBirds));
    }

    private IEnumerator SpawnLoop(Sprite sprite, Vector2 interval, System.Action spawn)
    {
        if (sprite == null) yield break;
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(interval.x, interval.y));
            if (spawnLeft == null || spawnRight == null) continue;
            if (GameManager.Instance != null && GameManager.Instance.IsHeadlessSimulation) continue;
            spawn();
        }
    }

    // ---------- helpers ----------

    private Image MakeFlyer(Sprite sprite, float scale)
    {
        var go = new GameObject(sprite.name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.sizeDelta = sprite.rect.size * scale;
        return img;
    }

    private float BandY(float bottomFrac, float topFrac)
    {
        // Interpret band fractions against the vertical span between the two
        // boundary markers' parent area, using this rect's height as the sky.
        var area = (RectTransform)transform;
        float h = area.rect.height;
        return Mathf.Lerp(-h * 0.5f, h * 0.5f, Random.Range(bottomFrac, topFrac));
    }

    private void SpawnCrossing_Plane() =>
        StartCoroutine(Crossing(planeSprite, planeScale, planeSpeedRange, planeBandBottom, planeBandTop));

    private void SpawnCrossing_Heli() =>
        StartCoroutine(Crossing(helicopterSprite, heliScale, heliSpeedRange, heliBandBottom, heliBandTop));

    private IEnumerator Crossing(Sprite sprite, float scale, Vector2 speedRange,
                                 float bandBottom, float bandTop)
    {
        bool fromLeft = Random.value > 0.5f;
        var img = MakeFlyer(sprite, scale);
        var rt = img.rectTransform;

        float startX = (fromLeft ? spawnLeft : spawnRight).anchoredPosition.x;
        float endX   = (fromLeft ? spawnRight : spawnLeft).anchoredPosition.x;
        float y = BandY(bandBottom, bandTop);
        rt.anchoredPosition = new Vector2(startX, y);
        // sprites face right; flip when travelling left
        rt.localScale = new Vector3(fromLeft ? 1f : -1f, 1f, 1f);

        float speed = Random.Range(speedRange.x, speedRange.y);
        float dir = fromLeft ? 1f : -1f;

        while (rt != null && Mathf.Sign(endX - rt.anchoredPosition.x) == Mathf.Sign(dir))
        {
            rt.anchoredPosition += new Vector2(dir * speed * Time.deltaTime, 0f);
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }

    private void SpawnBirds()
    {
        StartCoroutine(BirdFlight());
    }

    private IEnumerator BirdFlight()
    {
        var img = MakeFlyer(birdsSprite, birdScale);
        var rt = img.rectTransform;
        var area = (RectTransform)transform;
        float w = area.rect.width, h = area.rect.height;

        // rise from the lower third (treeline), drift up and across
        bool fromLeft = Random.value > 0.5f;
        float x = fromLeft ? Random.Range(-w * 0.45f, -w * 0.15f)
                           : Random.Range(w * 0.15f, w * 0.45f);
        float y = Random.Range(-h * 0.35f, -h * 0.15f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.localScale = new Vector3(fromLeft ? 1f : -1f, 1f, 1f);

        float speed = Random.Range(birdSpeedRange.x, birdSpeedRange.y);
        float dir = fromLeft ? 1f : -1f;
        float t = 0f;
        Color c = img.color;

        while (rt != null && t < birdLifetime)
        {
            t += Time.deltaTime;
            rt.anchoredPosition += new Vector2(dir * speed, speed * 0.55f) * Time.deltaTime;
            // fade in briefly, fade out over the last third
            float a = Mathf.Min(Mathf.Clamp01(t / 0.8f),
                                Mathf.Clamp01((birdLifetime - t) / (birdLifetime * 0.33f)));
            c.a = a;
            img.color = c;
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }
}
