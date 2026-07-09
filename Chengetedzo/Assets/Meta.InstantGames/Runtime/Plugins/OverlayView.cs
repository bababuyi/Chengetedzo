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
using System.Threading.Tasks;

namespace Meta.InstantGames
{
    public class OverlayView
    {
        public string id;
        public OverlayView() { }
        public override string ToString()
        {
            return $"{nameof(OverlayView)}(id={id})";
        }

        public async Task ShowAsync()
        {
            await WebGLBridge.OverlayFunction("showAsync", id);
            await SetStyle("opacity", "1");
            await SetStyle("pointer-events", "auto");
        }

        public async Task UpdateAsync(string data)
        {
            await WebGLBridge.OverlayFunction("updateAsync", id, data);
        }

        public async Task DismissAsync()
        {
            await WebGLBridge.OverlayFunction("dismissAsync", id);
            await SetStyle("opacity", "0");
            await SetStyle("pointer-events", "none");
        }
        public async Task<string> GetStatus()
        {
            string result = await WebGLBridge.OverlayFunction("getStatus", id);
            return result;
        }
        public async Task<string> GetInitialData()
        {
            string result = await WebGLBridge.OverlayFunction("getInitialData", id);
            return result;
        }
        public async Task<string> GetErrors()
        {
            string result = await WebGLBridge.OverlayFunction("getErrors", id);
            return result;
        }

        #region New functions
        public async Task MoveRandom()
        {
            await SetStyle("position", "absolute");
            await SetStyle("transition-duration", "0.5s");
            await SetStyle("transform", "translate(-50%,-50%)");
            await SetStyle("top", Random.Range(0, 100) + "%");
            await SetStyle("left", Random.Range(0, 100) + "%");
        }
        public async Task SizeRandom()
        {
            await SetStyle("height", Random.Range(100, 300) + "px");
            await SetStyle("width", Random.Range(100, 300) + "px");
        }

        public async Task SetStyle(string styleName, string styleValue)
        {
            Json json = Json.Object()
            .Prop("styleName", styleName)
            .Prop("value", styleValue);
            await WebGLBridge.OverlayFunction("setStyle", id, json.ToString());
        }

        public async Task SetAttribute(string attributeName, string attributeValue)
        {
            Json json = Json.Object()
            .Prop("attributeName", attributeName)
            .Prop("value", attributeValue);
            await WebGLBridge.OverlayFunction("setAttribute", id, json.ToString());
        }

        public async Task DestroyOverlayView()
        {
            await WebGLBridge.OverlayFunction("destroyOverlayView", id);
        }
        #endregion
    }
}
