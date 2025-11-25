using UnityEngine;

public class WindScroller : MonoBehaviour
{
    public float scrollSpeed = 60f;
    public float verticalDriftAmount = 10f;
    public float verticalDriftSpeed = 1f;

    public RectTransform leftBoundary;
    public RectTransform rightBoundary;

    private RectTransform rect;
    private float baseY;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        baseY = rect.anchoredPosition.y;
    }

    private void Start()
    {
        StartCoroutine(WindLoop());
    }

    private System.Collections.IEnumerator WindLoop()
    {
        while (true)
        {
            // 20%-40% chance no wind appears for a few seconds
            if (Random.value < 0.3f)
            {
                gameObject.SetActive(false);
                yield return new WaitForSeconds(Random.Range(2f, 5f));
                gameObject.SetActive(true);

                // Reset position each time wind starts again
                ResetPositionRandomSide();
            }

            yield return null;
        }
    }

    private void ResetPositionRandomSide()
    {
        bool spawnFromLeft = Random.value > 0.5f;

        if (spawnFromLeft)
            rect.anchoredPosition = new Vector2(leftBoundary.anchoredPosition.x, baseY);
        else
            rect.anchoredPosition = new Vector2(rightBoundary.anchoredPosition.x, baseY);

        scrollSpeed = Mathf.Abs(scrollSpeed) * (spawnFromLeft ? 1 : -1);
    }

    private void Update()
    {
        // Horizontal movement
        rect.anchoredPosition += new Vector2(scrollSpeed * Time.deltaTime, 0);

        // Vertical drift (sin wave)
        float yOffset = Mathf.Sin(Time.time * verticalDriftSpeed) * verticalDriftAmount;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, baseY + yOffset);

        // If fully off-screen ? teleport to opposite side
        if (rect.anchoredPosition.x > rightBoundary.anchoredPosition.x + 200)
        {
            rect.anchoredPosition = new Vector2(leftBoundary.anchoredPosition.x - 200, rect.anchoredPosition.y);
        }
        else if (rect.anchoredPosition.x < leftBoundary.anchoredPosition.x - 200)
        {
            rect.anchoredPosition = new Vector2(rightBoundary.anchoredPosition.x + 200, rect.anchoredPosition.y);
        }
    }
}