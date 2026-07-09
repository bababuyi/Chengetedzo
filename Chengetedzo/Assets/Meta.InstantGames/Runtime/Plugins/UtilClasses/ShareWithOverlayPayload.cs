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
    public enum ShareIntent
    {
        INVITE,
        REQUEST,
        CHALLENGE,
        SHARE,
        CUSTOM
    }
    public static class IShareIntentExtensions
    {
        public static string ToValue(this ShareIntent intent)
        {
            return intent switch
            {
                ShareIntent.INVITE => "INVITE",
                ShareIntent.REQUEST => "REQUEST",
                ShareIntent.CHALLENGE => "CHALLENGE",
                ShareIntent.SHARE => "SHARE",
                ShareIntent.CUSTOM => "CUSTOM",
                _ => intent.ToString()
            };
        }
    }

    public class ShareWithOverlayPayload
    {
        public ShareIntent intent; // ("INVITE" | "REQUEST" | "CHALLENGE" | "SHARE") Indicates the intent of the share.
        public string image; // string? Optional, but must be specified if imageOverlayPath is not specified.
        public string imageOverlayPath; // string? A path relative from the root of the game to the XML that represents the image to be shared. Optional, but must be specified if the image property is not specified.
        public string pathToCSS; // string? A path to the CSS from index.html used for classes in the XML. Optional, but necessary if className is used in the imageOverlay XML.
        public Json initialData; // Object? A blob of data used in the text or in the imageOverlay XML. Optional, but necessary if either the text or imageOverlay uses developer parameters.
        public MediaParams media; // MediaParams? Optional content for the gif or video.
        public string text; // string A text message to be shared. Can use the same user data and developer parameter keys that the overlays use.
        public string notificationText; // string? A text message to be used for notifications when sharing using to separate user message and notifcation text. If not included we will use the default text property for sharing in messenger and the notification.
        public Json data; // Object? A blob of data to attach to the share. All game sessions launched from the share will be able to access this blob through FBInstant.getEntryPointData().
        public ShareDestination[] shareDestination; // <Deprecated> Array<ShareDestination>? A optional array to set sharing destinations in the share dialog.If not specified all available sharing destinations will be displayed.
        public string surface; // ("FACEBOOK" | "MESSENGER" | "WHATSAPP")? Optional property to specify the surface where the share will be posted.
        public bool switchContext; // boolean? A flag indicating whether to switch the user into the new context created on sharing

        public Json ToJson()
        {
            string[] destinationArray = new string[] { "NEWSFEED", "GROUP", "COPY_LINK", "MESSENGER" };

            Json payload = Json.Object()
                    .Prop("intent", intent.ToValue())
                    .Prop("switchContext", switchContext)
                    .Array("shareDestination", destinationArray);

            if (pathToCSS != null) payload.Prop("pathToCSS", pathToCSS);
            if (text != null) payload.Prop("text", text);
            if (notificationText != null) payload.Prop("notificationText", notificationText);
            if (surface != null) payload.Prop("surface", surface);
            if (data != null) payload.Obj("data", data);
            if (initialData != null) payload.Obj("initialData", initialData);

            if (image != null)
            {
                payload.Prop("image", image);
            }
            else if (imageOverlayPath != null)
            {
                payload.Prop("imageOverlayPath", imageOverlayPath);
            }
            else
            {
                SDKDebug.LogError("no image specified!");
            }

            return payload;
        }
    }
}
