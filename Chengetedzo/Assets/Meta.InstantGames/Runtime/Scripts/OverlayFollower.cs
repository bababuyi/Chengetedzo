// Copyright (c) Facebook, Inc. and its affiliates. All rights reserved.
//
// The examples provided by Facebook are for non-commercial testing and evaluation
// purposes only. Facebook reserves all rights not expressly granted.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// FACEBOOK BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using Meta.InstantGames;
using System.Threading.Tasks;

/// <summary>
/// Component that creates and manages an overlay view that follows a UI element.
/// </summary>
public class OverlayFollower : MonoBehaviour
{
    bool activateOverlay = true;
    public Vector3 spinVec;
    public float moveSpeed = 200f;
    public float delayAfterReaching = 1f;

    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 destination;
    private float delayTimer;
    private bool isWaiting;
    RectTransform targetRect;
    public RectTransform canvasRect;
    public OverlayView overlayView;

    async void Start()
    {
        spinVec = new Vector3(
            Random.Range(10, 45),
            Random.Range(10, 45),
            Random.Range(10, 45));

        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        SetRandomDestination();

        // Get the parent RectTransform reference
        if (rectTransform != null && rectTransform.parent is RectTransform pr)
        {
            canvasRect = pr;
        }
        else
        {
            canvasRect = GetComponentInParent<RectTransform>();
        }

        // example of overlay follower
        if (activateOverlay)
        {
            targetRect = rectTransform;
            await LoadOverlayViewAsync();
        }

    }

    /// <summary>
    /// Loads and displays the overlay view asynchronously.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadOverlayViewAsync()
    {
        try
        {
            string overlayViewContentOverride = @"
                                        <View className=""ov-follower"" onTapEvent=""clicker"">
                                            <Image className=""badge"" src=""Icon_ImageIcon_Badge_Best.png"" />
                                        </View>";
            string domElementId = "unity-container";
            string iFrameStyle = "border:2px dashed red; z-index: 100; position:absolute; top:50%; left:50%; width:100px; height:100px; overflow:hidden; background-color:transparent; opacity:1;  transform:translate(-50%,-50%);";
            string pathToCSS = "ig_views/ov-style.css";
            string initialData = Json.Object().ToString();
            string pathToOverlayFiles = "ig_views";

            overlayView = await FBInstant.OverlayViews.CreateOverlayViewWithXMLStringAsync(
                overlayViewContentOverride,
                domElementId,
                iFrameStyle,
                pathToCSS,
                initialData,
                pathToOverlayFiles
            );
            SDKDebug.Log("overlayID " + overlayView);
            await overlayView.SetAttribute("scrolling", "no");
            await overlayView.ShowAsync();
        }
        catch (System.Exception ex)
        {
            SDKDebug.LogError($"[OverlayFollower] LoadOverlayView failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the overlay view position to match the target UI element.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task UpdateOverlayViewPositionAsync()
    {
        try
        {
            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;

            string canvasPositions = await FBInstant.GetCanvasRect();
            JsonValue parsed = JsonParser.Parse(canvasPositions);

            if (parsed == null)
            {
                SDKDebug.LogWarning("[OverlayFollower] Failed to parse canvas positions");
                return;
            }

            float canvasWidth2 = parsed["width"].AsFloat();
            float canvasHeight2 = parsed["height"].AsFloat();

            // Map goRect's anchored position (center-based in Unity canvas) to iframe (top-left origin, in CSS px).
            // 1) Compute normalized position of goRect within the Unity canvas (0..1 from left/top)
            float normX = (targetRect.anchoredPosition.x + canvasWidth * 0.5f) / canvasWidth;
            float normY = (canvasHeight * 0.5f - targetRect.anchoredPosition.y) / canvasHeight;

            // 2) Convert to iframe pixel space (top-left origin) using the actual canvas size reported by WebGL
            float targetX = normX * canvasWidth2;
            float targetY = normY * canvasHeight2;

            // Fire and forget style updates (intentionally not awaited for performance)
            _ = overlayView.SetStyle("left", targetX + "px");
            _ = overlayView.SetStyle("top", targetY + "px");
        }
        catch (System.Exception ex)
        {
            SDKDebug.LogError($"[OverlayFollower] UpdateOverlayViewPosition failed: {ex.Message}");
        }
    }

    void Update()
    {
        transform.Rotate(spinVec * Time.deltaTime, Space.Self);

        if (rectTransform == null || canvas == null) return;

        if (overlayView != null) { _ = UpdateOverlayViewPositionAsync(); }

        if (isWaiting)
        {
            delayTimer -= Time.deltaTime;
            if (delayTimer <= 0f)
            {
                isWaiting = false;
                SetRandomDestination();
            }
            return;
        }

        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 direction = (destination - currentPos).normalized;
        float distance = Vector2.Distance(currentPos, destination);

        if (distance <= moveSpeed * Time.deltaTime)
        {
            rectTransform.anchoredPosition = destination;
            isWaiting = true;
            delayTimer = delayAfterReaching;
        }
        else
        {
            rectTransform.anchoredPosition = currentPos + direction * moveSpeed * Time.deltaTime;
        }
    }

    void SetRandomDestination()
    {
        if (canvas == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        Vector2 imageSize = rectTransform.sizeDelta;

        // Allow 10% out of bounds beyond each side
        float halfWidth = (canvasSize.x - imageSize.x) * 0.5f * 1.1f;
        float halfHeight = (canvasSize.y - imageSize.y) * 0.5f * 1.1f;

        float randomX = Random.Range(-halfWidth, halfWidth);
        float randomY = Random.Range(-halfHeight, halfHeight);

        destination = new Vector2(randomX, randomY);
    }
}
