using UnityEngine;

// Summer's discrete drifting clouds. Winter and rainy don't use this at all - winter gets
// a ScrollingLayer with a wispy cloud sheet, rainy gets a scrolling storm sky instead.
// SeasonalBackgroundManager turns this component on and off per season/weather; this
// script itself has no calendar awareness anymore.
//
// Replaces the old template-cloning setup entirely. No more scene object templates, no
// more RectTransform boundary markers - spawns straight from a sprite array onto a
// reusable prefab, and reads the camera's own edges to know where to spawn and when a
// cloud has drifted off (see CloudDrift).
public class CloudSpawner : MonoBehaviour
{
    [Header("Source art - the summer cloud sheet, sliced into individual sprites")]
    public Sprite[] cloudSprites;

    [Header("Prefab - a plain GameObject with a SpriteRenderer and CloudDrift already on it")]
    public GameObject cloudPrefab;

    [Tooltip("Defaults to Camera.main if left empty.")]
    public Camera targetCamera;

    [Header("Spawn Settings")]
    public float minInterval = 2f;
    public float maxInterval = 5f;

    [Header("Cloud Variations (world units - see Baba's numbers in the report)")]
    public float minSpeed = 0.5f;
    public float maxSpeed = 1.5f;
    [Tooltip("Multiplied onto the prefab's own scale, so a hand-tuned base size is preserved.")]
    public float minScale = 0.85f;
    public float maxScale = 1.25f;
    public float heightJitter = 0.4f;
    [Tooltip("Vertical spawn band, as a fraction of the camera's half-height above and below its center.")]
    public float verticalRangeFraction = 0.6f;

    [Header("Cloud Limit")]
    public int maxClouds = 10;
    public int cloudCount = 0;

    [Header("Sorting")]
    public string cloudsSortingLayer = "Clouds";

    private void Awake()
    {
        cloudCount = 0;
        if (targetCamera == null) targetCamera = Camera.main;
    }

    // OnEnable/OnDisable rather than Start, since SeasonalBackgroundManager toggles this
    // whole component on and off per season by disabling the GameObject.
    private void OnEnable()
    {
        StartCoroutine(SpawnCloud());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private System.Collections.IEnumerator SpawnCloud()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            if (cloudCount >= maxClouds) continue;
            if (cloudSprites == null || cloudSprites.Length == 0) continue;
            if (cloudPrefab == null || targetCamera == null) continue;

            Sprite sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
            if (sprite == null) continue;

            bool spawnFromLeft = Random.value > 0.5f;

            float camHalfWidth = targetCamera.orthographicSize * targetCamera.aspect;
            float camHalfHeight = targetCamera.orthographicSize;
            float camX = targetCamera.transform.position.x;
            float camY = targetCamera.transform.position.y;

            float spawnX = camX + (spawnFromLeft ? -camHalfWidth : camHalfWidth);
            float spawnY = camY
                + Random.Range(-camHalfHeight, camHalfHeight) * verticalRangeFraction
                + Random.Range(-heightJitter, heightJitter);

            GameObject cloud = Instantiate(cloudPrefab, transform);
            cloud.transform.position = new Vector3(spawnX, spawnY, cloud.transform.position.z);
            cloud.SetActive(true);

            var sr = cloud.GetComponent<SpriteRenderer>();
            if (sr == null) sr = cloud.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (!string.IsNullOrEmpty(cloudsSortingLayer))
                sr.sortingLayerName = cloudsSortingLayer;

            float scale = Random.Range(minScale, maxScale);
            cloud.transform.localScale = new Vector3(scale, scale, 1f);

            CloudDrift mover = cloud.GetComponent<CloudDrift>();
            if (mover == null) mover = cloud.AddComponent<CloudDrift>();
            mover.speed = Random.Range(minSpeed, maxSpeed);
            mover.moveRight = spawnFromLeft;
            mover.targetCamera = targetCamera;
            mover.spawner = this;

            cloudCount++;
        }
    }
}
