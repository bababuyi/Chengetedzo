using UnityEngine;

public class CloudDrift : MonoBehaviour
{
    public float speed = 10f;
    public bool moveRight = true;

    public RectTransform leftBoundary;
    public RectTransform rightBoundary;

    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        float direction = moveRight ? 1f : -1f;

        rect.anchoredPosition += new Vector2(speed * direction * Time.deltaTime, 0);

        if (moveRight && rect.anchoredPosition.x >= rightBoundary.anchoredPosition.x)
        {
            CloudSpawner.cloudCount--;
            Destroy(gameObject);
        }
        else if (!moveRight && rect.anchoredPosition.x <= leftBoundary.anchoredPosition.x)
        {
            CloudSpawner.cloudCount--;
            Destroy(gameObject);
        }
    }
}
