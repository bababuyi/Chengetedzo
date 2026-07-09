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

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Meta.InstantGames
{
    /// <summary>
    /// Manages a collection of overlay views and provides factory methods for creating them.
    /// Overlay views are HTML/CSS based UI elements rendered on top of the Unity WebGL canvas.
    /// </summary>
    public class OverlayViews
    {
        /// <summary>
        /// List of all overlay views created through this manager.
        /// </summary>
        public List<OverlayView> overlayViewList;

        /// <summary>
        /// Creates a new OverlayViews manager instance.
        /// </summary>
        public OverlayViews() { overlayViewList = new List<OverlayView>(); }

        /// <summary>
        /// Adds an overlay view to the internal tracking list from a JSON result.
        /// </summary>
        /// <param name="jsonResult">JSON string containing the overlay view ID.</param>
        /// <returns>The created OverlayView instance, or null if parsing failed.</returns>
        public OverlayView AddOverlayViewToList(string jsonResult)
        {
            SDKDebug.Log("AddOverlayViewToList received: " + jsonResult);

            if (string.IsNullOrEmpty(jsonResult))
            {
                SDKDebug.LogError("AddOverlayViewToList: jsonResult is null or empty");
                return null;
            }

            JsonValue parsed = JsonParser.Parse(jsonResult);
            if (parsed == null)
            {
                SDKDebug.LogError("AddOverlayViewToList: Failed to parse JSON result");
                return null;
            }

            var idValue = parsed["id"];
            if (idValue == null)
            {
                SDKDebug.LogError("AddOverlayViewToList: 'id' key not found in JSON result. Keys present: " + jsonResult);
                return null;
            }

            string id = idValue.ToStringTrimmed();
            if (string.IsNullOrEmpty(id))
            {
                SDKDebug.LogError("AddOverlayViewToList: 'id' value is null or empty");
                return null;
            }

            OverlayView newOverlayView = new OverlayView() { id = id };
            overlayViewList.Add(newOverlayView);
            SDKDebug.Log("OverlayView ID stored: " + id);

            return newOverlayView;
        }

        /// <summary>
        /// Creates an overlay view from an XML file path with callbacks.
        /// </summary>
        /// <param name="pathToXML">Path to the XML file defining the overlay view.</param>
        /// <param name="pathToCSS">Path to the CSS file for styling.</param>
        /// <param name="initialData">JSON string of initial data for the overlay.</param>
        /// <param name="onLoadCallback">Callback invoked when the overlay loads successfully.</param>
        /// <param name="onErrorCallback">Callback invoked when an error occurs.</param>
        /// <returns>A task that resolves to the created OverlayView.</returns>
        public async Task<OverlayView> CreateOverlayView(
            string pathToXML,
            string pathToCSS,
            string initialData,
            Action<int, string> onLoadCallback,
            Action<int, string> onErrorCallback)
        {
            if (Application.isEditor) { return new OverlayView() { id = "dummy-id" }; }

            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", pathToXML)
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", pathToCSS)
                    )
                    .Add(item => item
                        .Prop("type", "obj")
                        .Prop("value", initialData)
                    )
                    .Add(item => item
                        .Prop("type", "callback")
                        .Prop("value", "onLoadCallback")
                    )
                    .Add(item => item
                        .Prop("type", "callback")
                        .Prop("value", "onErrorCallback")
                    )
                ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("overlayViews.createOverlayView", payload);
            SDKDebug.Log("overlayView set :" + result);
            return AddOverlayViewToList(result);
        }

        /// <summary>
        /// Creates an overlay view attached to a specific DOM element.
        /// </summary>
        /// <param name="pathToXML">Path to the XML file defining the overlay view.</param>
        /// <param name="domElementId">The ID of the DOM element to attach to.</param>
        /// <param name="iFrameStyle">CSS style string for the iframe.</param>
        /// <param name="pathToCSS">Path to the CSS file for styling.</param>
        /// <param name="initialData">JSON string of initial data for the overlay.</param>
        /// <returns>A task that resolves to the created OverlayView.</returns>
        public async Task<OverlayView> CreateOverlayViewAsync(
            string pathToXML,
            string domElementId,
            string iFrameStyle,
            string pathToCSS,
            string initialData)
        {
            if (Application.isEditor) { return new OverlayView() { id = "dummy-id" }; }

            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", pathToXML)
                    )
                    .Add(item => item
                        .Prop("type", "domElementId")
                        .Prop("value", domElementId)
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", iFrameStyle)
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", pathToCSS)
                    )
                    .Add(item => item
                        .Prop("type", "obj")
                        .Prop("value", initialData)
                    )
                ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("overlayViews.createOverlayViewAsync", payload);
            SDKDebug.Log("overlayView set :" + result);
            return AddOverlayViewToList(result);
        }

        /// <summary>
        /// Creates an overlay view from an XML string with callbacks.
        /// </summary>
        /// <param name="overlayViewContentOverride">The XML content as a string.</param>
        /// <param name="pathToCSS">Path to the CSS file for styling.</param>
        /// <param name="initialData">JSON string of initial data for the overlay.</param>
        /// <param name="onLoadCallback">Callback invoked when the overlay loads successfully.</param>
        /// <param name="onErrorCallback">Callback invoked when an error occurs.</param>
        /// <param name="pathToOverlayFiles">Base path for overlay resource files.</param>
        /// <returns>A task that resolves to the created OverlayView.</returns>
        public async Task<OverlayView> CreateOverlayViewWithXMLString(
            string overlayViewContentOverride,
            string pathToCSS,
            string initialData,
            Action<int, string> onLoadCallback,
            Action<int, string> onErrorCallback,
            string pathToOverlayFiles)
        {
            if (Application.isEditor) { return new OverlayView() { id = "dummy-id" }; }

            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", overlayViewContentOverride)
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", pathToCSS)
                    )
                    .Add(item => item
                        .Prop("type", "obj")
                        .Prop("value", initialData)
                    )
                    .Add(item => item
                        .Prop("type", "callback")
                        .Prop("value", "onLoadCallback")
                    )
                    .Add(item => item
                        .Prop("type", "callback")
                        .Prop("value", "onErrorCallback")
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", pathToOverlayFiles)
                    )
                ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("overlayViews.createOverlayViewWithXMLString", payload);
            SDKDebug.Log("overlayView set :" + result);
            return AddOverlayViewToList(result);
        }

        /// <summary>
        /// Creates an overlay view from an XML string attached to a specific DOM element.
        /// </summary>
        /// <param name="overlayViewContentOverride">The XML content as a string.</param>
        /// <param name="domElementId">The ID of the DOM element to attach to.</param>
        /// <param name="iFrameStyle">CSS style string for the iframe.</param>
        /// <param name="pathToCSS">Path to the CSS file for styling.</param>
        /// <param name="initialData">JSON string of initial data for the overlay.</param>
        /// <param name="pathToOverlayFiles">Base path for overlay resource files.</param>
        /// <returns>A task that resolves to the created OverlayView.</returns>
        public async Task<OverlayView> CreateOverlayViewWithXMLStringAsync(
            string overlayViewContentOverride,
            string domElementId,
            string iFrameStyle,
            string pathToCSS,
            string initialData,
            string pathToOverlayFiles)
        {

            if (Application.isEditor) { return new OverlayView() { id = "dummy-id" }; }


            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", overlayViewContentOverride)
                    )
                    .Add(item => item
                        .Prop("type", "domElement")
                        .Prop("value", domElementId)
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", iFrameStyle)
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", pathToCSS)
                    )
                    .Add(item => item
                        .Prop("type", "obj")
                        .Prop("value", initialData)
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", pathToOverlayFiles)
                    )
                ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("overlayViews.createOverlayViewWithXMLStringAsync", payload);
            SDKDebug.Log("overlayView set :" + result);
            return AddOverlayViewToList(result);
        }

        /// <summary>
        /// Creates an overlay view displaying the player's profile picture.
        /// </summary>
        /// <param name="domElementId">The ID of the DOM element to attach to.</param>
        /// <param name="imageStyle">CSS style string for the image.</param>
        /// <param name="iFrameStyle">CSS style string for the iframe.</param>
        /// <returns>A task that resolves to the created OverlayView.</returns>
        public async Task<OverlayView> CreateProfilePictureOverlayViewAsync(string domElementId, string imageStyle, string iFrameStyle)
        {

            if (Application.isEditor) { return new OverlayView() { id = "dummy-id" }; }

            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "domElementId")
                        .Prop("value", domElementId)
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", imageStyle)
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", iFrameStyle)
                    )
                ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("overlayViews.createProfilePictureOverlayViewAsync", payload);
            SDKDebug.Log("overlayView set :" + result);
            return AddOverlayViewToList(result);
        }

        /// <summary>
        /// Creates an overlay view displaying the player's profile name.
        /// </summary>
        /// <param name="domElementId">The ID of the DOM element to attach to.</param>
        /// <param name="textStyle">CSS style string for the text.</param>
        /// <param name="iFrameStyle">CSS style string for the iframe.</param>
        /// <param name="pathToCSS">Path to the CSS file for additional styling.</param>
        /// <returns>A task that resolves to the created OverlayView.</returns>
        public async Task<OverlayView> CreateProfileNameOverlayViewAsync(string domElementId, string textStyle, string iFrameStyle, string pathToCSS)
        {
            if (Application.isEditor) { return new OverlayView() { id = "dummy-id" }; }

            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "domElementId")
                        .Prop("value", domElementId)
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", textStyle)
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", iFrameStyle)
                    )
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", pathToCSS)
                    )
                ).ToString();


            string result = await WebGLBridge.CallAsyncJavaScript("overlayViews.createProfileNameOverlayViewAsync", payload);
            SDKDebug.Log("overlayView set :" + result);
            return AddOverlayViewToList(result);
        }

        /// <summary>
        /// Registers a callback to handle custom events from overlay views.
        /// </summary>
        /// <param name="callback">The callback function receiving event string and overlay view ID.</param>
        public void SetCustomEventHandler(Action<string, string> callback)
        {
            WebGLBridge.RegisterPersistentCallback("overlayViews.setCustomEventHandler", (string jsonString) =>
            {
                JsonValue json = JsonParser.Parse(jsonString);
                string eventStr = json["eventStr"].ToStringTrimmed();
                string overlayID = json["overlayViewId"].ToStringTrimmed();
                callback.Invoke(eventStr, overlayID);
            });
        }

        /// <summary>
        /// Gets all overlay views from the JavaScript side.
        /// </summary>
        /// <returns>A task that resolves to a JSON string containing all overlay view IDs.</returns>
        public async Task<string> GetOverlayViews()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("overlayViews.getOverlayViews");

            JsonValue jsonValue = JsonParser.Parse(result);
            foreach (JsonValue id in jsonValue.ArrayElements)
            {
                try
                {
                    // SDKDebug.Log(id);
                }
                catch (Exception e)
                {
                    SDKDebug.LogError($"Error iterating jsonValue at index : {e.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Shows all tracked overlay views.
        /// </summary>
        /// <returns>A task that completes when all overlays are shown.</returns>
        /// <exception cref="Exception">Thrown when no overlay views are saved.</exception>
        public async Task ShowAsyncAll()
        {
            if (overlayViewList.Count == 0)
            {
                throw new Exception("no overlayViews saved");
            }
            foreach (OverlayView overlayView in overlayViewList)
            {
                if (overlayView == null)
                {
                    SDKDebug.Log("overlayView not defined yet.");
                    continue;
                }
                await overlayView.ShowAsync();
                await overlayView.SetAttribute("scrolling", "no");
                await overlayView.SetStyle("opacity", "1");
            }
        }



        /// <summary>
        /// Updates all tracked overlay views with new data and shows them.
        /// </summary>
        /// <param name="data">JSON string of data to pass to the overlays.</param>
        /// <returns>A task that completes when all overlays are updated.</returns>
        /// <exception cref="Exception">Thrown when no overlay views are saved.</exception>
        public async Task UpdateAsyncAll(string data)
        {
            if (overlayViewList.Count == 0)
            {
                throw new Exception("no overlayViews saved");
            }
            foreach (OverlayView overlayView in overlayViewList)
            {
                if (overlayView == null)
                {
                    SDKDebug.Log("overlayView not defined yet.");
                    continue;
                }
                string payload = data;
                await overlayView.UpdateAsync(payload);
                await overlayView.ShowAsync();
            }
        }



        /// <summary>
        /// Dismisses all tracked overlay views.
        /// </summary>
        /// <returns>A task that completes when all overlays are dismissed.</returns>
        /// <exception cref="Exception">Thrown when no overlay views are saved.</exception>
        public async Task DismissAsyncAll()
        {
            if (overlayViewList.Count == 0)
            {
                throw new Exception("no overlayViews saved");
            }
            foreach (OverlayView overlayView in overlayViewList)
            {
                if (overlayView == null)
                {
                    SDKDebug.Log("overlayView not defined yet.");
                    continue;
                }
                await overlayView.DismissAsync();
            }
        }



        /// <summary>
        /// Sets random styles on all tracked overlay views (for testing/demo purposes).
        /// </summary>
        /// <returns>A task that completes when all styles are set.</returns>
        /// <exception cref="Exception">Thrown when no overlay views are saved.</exception>
        public async Task SetStyleAll()
        {
            if (overlayViewList.Count == 0)
            {
                throw new Exception("no overlayViews saved");
            }
            foreach (OverlayView overlayView in overlayViewList)
            {
                if (overlayView == null)
                {
                    SDKDebug.Log("overlayView not defined yet.");
                    continue;
                }
                string randomHexColor = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV(0f, 1f, 0f, 1f, 1f, 1f));

                await overlayView.SetStyle("background-color", randomHexColor);
                await overlayView.SetStyle("opacity", "1");
                await overlayView.SetStyle("height", UnityEngine.Random.Range(100, 300) + "px");
                await overlayView.SetStyle("width", UnityEngine.Random.Range(100, 300) + "px");
            }
        }



        /// <summary>
        /// Moves all tracked overlay views to random positions (for testing/demo purposes).
        /// </summary>
        /// <exception cref="Exception">Thrown when no overlay views are saved.</exception>
        public void MoveRandomAll()
        {
            if (overlayViewList.Count == 0)
            {
                throw new Exception("no overlayViews saved");
            }
            foreach (OverlayView overlayView in overlayViewList)
            {
                if (overlayView == null)
                {
                    SDKDebug.Log("overlayView not defined yet.");
                    continue;
                }

                _ = overlayView.MoveRandom();
                _ = overlayView.SizeRandom();
            }
        }

        /// <summary>
        /// Destroys all tracked overlay views and clears the list.
        /// </summary>
        /// <returns>A task that completes when all overlays are destroyed.</returns>
        /// <exception cref="Exception">Thrown when no overlay views are saved.</exception>
        public async Task DestroyAllAsync()
        {
            if (overlayViewList.Count == 0)
            {
                throw new Exception("no overlayViews saved");
            }
            for (int i = overlayViewList.Count - 1; i >= 0; i--)
            {
                SDKDebug.Log("destroy " + overlayViewList[i].id);
                await overlayViewList[i].DestroyOverlayView();
                overlayViewList.RemoveAt(i);
            }
            SDKDebug.Log("overlayviewlist cleared? " + overlayViewList.Count);
        }


    }

}
