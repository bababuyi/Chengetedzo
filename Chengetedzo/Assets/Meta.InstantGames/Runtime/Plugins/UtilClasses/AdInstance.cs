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

using System.Threading.Tasks;

namespace Meta.InstantGames
{
    public class AdInstance
    {
        public AdInstanceData data; // AdInstanceData The data for this ad instance
        public AdType type; // AdType The type of ad (interstitial, rewarded video, etc.)
        public string getPlacementID() => data.placementID;//Returns the Audience Network placement ID of this ad instance. Returns string The placement ID for this ad

        public async Task<string> LoadAsync()
        {
            //Preload the ad. The returned promise resolves when the preload completes, and rejects if it failed.
            string result = await WebGLBridge.AdFunction("loadAsync", data.adInstanceID);
            SDKDebug.Log("Ad Load Async complete " + result);
            return result;
        }
        public async Task<string> ShowAsync()
        {
            //Present the ad. The returned promise resolves when user finished watching the ad, and rejects if it failed to present or was closed during the ad.
            string result = await WebGLBridge.AdFunction("showAsync", data.adInstanceID);
            SDKDebug.Log("Ad Load Async complete " + result);
            return result;
        }

        public AdInstance(string jsonString)
        {
            SDKDebug.Log($"[AdInstance] Parsing: null={jsonString == null}, value=[{jsonString}]");
            JsonValue json = JsonParser.Parse(jsonString);
            SDKDebug.Log($"[AdInstance] JsonParser result: null={json == null}");
            var v1 = json["$1"];
            SDKDebug.Log($"[AdInstance] json[$1]: null={v1 == null}, value=[{v1}]");
            var v2 = json["$2"];
            SDKDebug.Log($"[AdInstance] json[$2]: null={v2 == null}, value=[{v2}]");
            data = new AdInstanceData(v1?.ToStringTrimmed());
            string adTypeStr = v2?.ToStringTrimmed() ?? "";
            // FBInstant SDK sometimes returns "REWAREDED_VIDEO" (typo) — normalize it
            if (adTypeStr.Equals("REWAREDED_VIDEO", System.StringComparison.OrdinalIgnoreCase))
                adTypeStr = "REWARDED_VIDEO";
            type = (AdType)System.Enum.Parse(typeof(AdType), adTypeStr, ignoreCase: true);
        }
        public AdInstance() { }
        public override string ToString()
        {
            return $"AdInstance(type={type}, placementID={data?.placementID}, adInstanceID={data?.adInstanceID})";
        }
    }
}
