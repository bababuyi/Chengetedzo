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

namespace Meta.InstantGames
{
    public class AdInstanceData
    {
        public string adInstanceID; // string Unique identifier for the ad instance
        public string placementID; // string Audience Network placement ID for the ad

        public AdInstanceData(string jsonString)
        {
            SDKDebug.Log($"[AdInstanceData] Parsing: value=[{jsonString}]");
            JsonValue json = JsonParser.Parse(jsonString);
            SDKDebug.Log($"[AdInstanceData] json null={json == null}");
            var adIdVal = json["adInstanceID"];
            var placeVal = json["placementID"];
            SDKDebug.Log($"[AdInstanceData] adInstanceID null={adIdVal == null}, placementID null={placeVal == null}");
            adInstanceID = adIdVal?.ToStringTrimmed();
            placementID = placeVal?.ToStringTrimmed();
        }
        public AdInstanceData() { }
    }
}
