using UnityEngine;

// Drives one spawned cloud sprite in a straight line until it clears the camera, then
// despawns itself. No boundary marker objects needed - reads the camera's own edges
// directly, same approach ScrollingLayer uses.
public class CloudDrift : MonoBehaviour
{
    public float speed = 1f; // world units/sec
    public bool moveRight = true;

    [Tooltip("Defaults to Camera.main if left empty.")]
    public Camera targetCamera;
    public CloudSpawner spawner;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void Update()
    {
        float direction = moveRight ? 1f : -1f;
        transform.position += new Vector3(speed * direction * Time.deltaTime, 0f, 0f);

        if (targetCamera == null) return;

        float camHalfWidth = targetCamera.orthographicSize * targetCamera.aspect;
        float camX = targetCamera.transform.position.x;
        float rightEdge = camX + camHalfWidth;
        float leftEdge = camX - camHalfWidth;

        // A little slack past the edge so it despawns after it's actually out of view,
        // not the instant it touches the boundary.
        if (moveRight && transform.position.x > rightEdge + 1f)
        {
            if (spawner != null) spawner.cloudCount--;
            Destroy(gameObject);
        }
        else if (!moveRight && transform.position.x < leftEdge - 1f)
        {
            if (spawner != null) spawner.cloudCount--;
            Destroy(gameObject);
        }
    }
}
