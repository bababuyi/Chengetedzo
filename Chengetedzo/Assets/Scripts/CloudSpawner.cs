using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    public enum SkySet { Summer, Winter, Rainy }

    [Header("Cloud Templates (inactive scene objects, one array per season)")]
    public RectTransform[] summerClouds;
    public RectTransform[] winterClouds;
    public RectTransform[] rainyClouds;

    [Header("Spawn Points")]
    public RectTransform spawnLeft;
    public RectTransform spawnRight;

    [Header("Spawn Settings")]
    public float minInterval = 2f;
    public float maxInterval = 5f;

    [Header("Cloud Variations")]
    public float minSpeed = 8f;
    public float maxSpeed = 20f;
    [Tooltip("Multiplied onto each template's own scale, so hand-tuned sizes are preserved.")]
    public float minScale = 0.85f;
    public float maxScale = 1.25f;
    [Tooltip("Random vertical offset from the template's own height.")]
    public float heightJitter = 40f;

    [Header("Cloud Limit")]
    public int maxClouds = 10;
    public int cloudCount = 0;

    [Header("State (driven by GameManager each month)")]
    public SkySet currentSet = SkySet.Summer;

    private void Awake()
    {
        cloudCount = 0;
    }

    private void Start()
    {
        StartCoroutine(SpawnCloud());
    }

    /// <summary>
    /// Mirrors SeasonalBackgroundManager.GetSeasonSprite's rules exactly,
    /// so the clouds always match the background behind them.
    /// </summary>
    public void UpdateForMonth(int calendarMonth, bool hasWeatherEvent)
    {
        int month = ((calendarMonth - 1) % 12) + 1;
        if (month >= 4 && month <= 8) currentSet = SkySet.Winter;
        else if (hasWeatherEvent) currentSet = SkySet.Rainy;
        else currentSet = SkySet.Summer;
    }

    private RectTransform[] ActiveTemplates() => currentSet switch
    {
        SkySet.Winter => winterClouds,
        SkySet.Rainy => rainyClouds,
        _ => summerClouds
    };

    private System.Collections.IEnumerator SpawnCloud()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            if (cloudCount >= maxClouds)
                continue;

            var set = ActiveTemplates();
            if (set == null || set.Length == 0 || spawnLeft == null || spawnRight == null)
                continue;

            RectTransform prefab = set[Random.Range(0, set.Length)];
            if (prefab == null)
                continue;

            bool spawnFromLeft = Random.value > 0.5f;
            RectTransform spawnPoint = spawnFromLeft ? spawnLeft : spawnRight;

            RectTransform cloud = Instantiate(prefab, transform);
            cloud.gameObject.SetActive(true); // templates stay inactive in the scene

            // spawn at the boundary, but keep the template's hand-placed height (+ jitter)
            cloud.anchoredPosition = new Vector2(
                spawnPoint.anchoredPosition.x,
                prefab.anchoredPosition.y + Random.Range(-heightJitter, heightJitter));

            float scale = Random.Range(minScale, maxScale);
            cloud.localScale = new Vector3(
                prefab.localScale.x * scale,
                prefab.localScale.y * scale, 1f);

            CloudDrift mover = cloud.GetComponent<CloudDrift>();
            if (mover == null)
                mover = cloud.gameObject.AddComponent<CloudDrift>();
            mover.speed = Random.Range(minSpeed, maxSpeed);
            mover.moveRight = spawnFromLeft;
            mover.leftBoundary = spawnLeft;
            mover.rightBoundary = spawnRight;
            mover.spawner = this;

            cloudCount++;
        }
    }
}
