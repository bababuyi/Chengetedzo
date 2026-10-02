using UnityEngine;

// A two-copy treadmill scroller. Two SpriteRenderers showing the same sprite, placed edge
// to edge, both moving at a fixed speed. When one's trailing edge clears the camera it
// teleports to sit immediately after the other, so the strip reads as continuous. No
// texture-offset/UV tricks - those fight Unity's sprite batching pipeline, this is just
// two GameObjects and some position math.
//
// Speed 0 leaves both copies sitting still, which reads as an ordinary static image since
// one copy exactly fills the camera frame and the other sits just out of view.
//
// Used for: the rainy sky (city_rainy_001 scrolling as a storm front) and the winter
// cloud layer (the wispy cloud sheet scrolling over a static winter sky). Not used for
// Skyline or Treeline - those stay as plain swapped sprites.
public class ScrollingLayer : MonoBehaviour
{
    public Sprite sprite;

    // World units/sec, magnitude only - direction is the separate moveRight flag below.
    // 0 means static.
    public float speed = 0f;
    public bool moveRight = true;

    public string sortingLayerName = "Sky";
    public int orderInLayer = 0;

    [Tooltip("Defaults to Camera.main if left empty.")]
    public Camera targetCamera;

    private SpriteRenderer copyA;
    private SpriteRenderer copyB;
    private float width;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        copyA = MakeCopy("Copy A");
        copyB = MakeCopy("Copy B");

        if (sprite != null) ApplySprite(sprite);
    }

    private SpriteRenderer MakeCopy(string copyName)
    {
        var go = new GameObject(copyName);
        go.transform.SetParent(transform, false);
        return go.AddComponent<SpriteRenderer>();
    }

    // Call this whenever the sprite changes at runtime. Resets both copies to the new
    // sprite's own width and puts them back edge to edge at the start position. Also
    // re-applies sortingLayerName/orderInLayer, so changing those in the Inspector at
    // runtime (or via script) takes effect on the next sprite change.
    public void ApplySprite(Sprite newSprite)
    {
        sprite = newSprite;
        if (sprite == null || copyA == null || copyB == null) return;

        width = sprite.bounds.size.x;

        copyA.sprite = sprite;
        copyB.sprite = sprite;
        copyA.sortingLayerName = sortingLayerName;
        copyB.sortingLayerName = sortingLayerName;
        copyA.sortingOrder = orderInLayer;
        copyB.sortingOrder = orderInLayer;

        copyA.transform.localPosition = Vector3.zero;
        copyB.transform.localPosition = new Vector3(width, 0f, 0f);
    }

    private void Update()
    {
        if (sprite == null || speed <= 0f || targetCamera == null || copyA == null || copyB == null)
            return;

        float signedSpeed = moveRight ? speed : -speed;
        float delta = signedSpeed * Time.deltaTime;
        copyA.transform.position += new Vector3(delta, 0f, 0f);
        copyB.transform.position += new Vector3(delta, 0f, 0f);

        float camHalfWidth = targetCamera.orthographicSize * targetCamera.aspect;
        float camLeft = targetCamera.transform.position.x - camHalfWidth;
        float camRight = targetCamera.transform.position.x + camHalfWidth;

        TryWrap(copyA, copyB, camLeft, camRight, signedSpeed);
        TryWrap(copyB, copyA, camLeft, camRight, signedSpeed);
    }

    private void TryWrap(SpriteRenderer mover, SpriteRenderer other, float camLeft, float camRight, float signedSpeed)
    {
        float halfWidth = width * 0.5f;
        float left = mover.transform.position.x - halfWidth;
        float right = mover.transform.position.x + halfWidth;

        if (signedSpeed > 0f && left > camRight)
        {
            // Scrolled fully past the right edge - recycle to sit just left of the other copy.
            float otherLeft = other.transform.position.x - halfWidth;
            SetX(mover, otherLeft - halfWidth);
        }
        else if (signedSpeed < 0f && right < camLeft)
        {
            // Scrolled fully past the left edge - recycle to sit just right of the other copy.
            float otherRight = other.transform.position.x + halfWidth;
            SetX(mover, otherRight + halfWidth);
        }
    }

    private static void SetX(SpriteRenderer sr, float x)
    {
        Vector3 p = sr.transform.position;
        p.x = x;
        sr.transform.position = p;
    }
}
