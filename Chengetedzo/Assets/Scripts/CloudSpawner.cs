using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    [Header("Cloud Prefabs")]
    public RectTransform whiteCloudPrefab;
    public RectTransform grayCloudPrefab;

    [Header("Spawn Points")]
    public RectTransform spawnLeft;
    public RectTransform spawnRight;

    [Header("Spawn Settings")]
    public float minInterval = 2f;
    public float maxInterval = 5f;

    [Header("Cloud Variations")]
    public float minSpeed = 8f;
    public float maxSpeed = 20f;
    public float minScale = 0.8f;
    public float maxScale = 1.5f;

    [Header("Season Settings")]
    public bool spawnWhiteClouds = true;
    public bool spawnGrayClouds = false;

    [Header("Cloud Limit")]
    public int maxClouds = 10;
    public static int cloudCount = 0;

    private void Start()
    {
        StartCoroutine(SpawnCloud());
    }

    private System.Collections.IEnumerator SpawnCloud()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            if (cloudCount >= maxClouds)
                continue;

            bool spawnFromLeft = Random.value > 0.5f;
            RectTransform spawnPoint = spawnFromLeft ? spawnLeft : spawnRight;

            RectTransform prefab =
                spawnWhiteClouds && spawnGrayClouds ? (Random.value > 0.5f ? whiteCloudPrefab : grayCloudPrefab)
                : spawnWhiteClouds ? whiteCloudPrefab
                : grayCloudPrefab;

            RectTransform cloud = Instantiate(prefab, transform);
            cloud.anchoredPosition = spawnPoint.anchoredPosition;

            float scale = Random.Range(minScale, maxScale);
            cloud.localScale = new Vector3(scale, scale, 1);

            CloudDrift mover = cloud.GetComponent<CloudDrift>();
            mover.speed = Random.Range(minSpeed, maxSpeed);
            mover.moveRight = spawnFromLeft;
            mover.leftBoundary = spawnLeft;
            mover.rightBoundary = spawnRight;

            cloudCount++;
        }
    }
}
