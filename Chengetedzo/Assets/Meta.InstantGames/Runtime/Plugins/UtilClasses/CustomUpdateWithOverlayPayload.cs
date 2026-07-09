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
    public class CustomUpdateWithOverlayPayload
    {
        public string action = "CUSTOM"; // UpdateAction For custom updates, this should be 'CUSTOM'.
        public string template; // string ID of the template this custom update is using. Templates should be predefined in fbapp-config.json. See the [Bundle Config documentation][https://developers.facebook.com/docs/games/instant-games/bundle-config][67] for documentation about fbapp-config.json.
        public LocalizableContent cta; // (string? | LocalizableContent?) Optional call-to-action button text. By default we will use a localized 'Play' as the button text. To provide localized versions of your own call to action, pass an object with the default cta as the value of 'default' and another object mapping locale keys to translations as the value of 'localizations'.
        public string image; // string? Optional data URL of a base64 encoded image. Must be specified if imageOverlayPath or media are not specified.
        public string imageOverlayPath; // string? Optional path to the XML that represents the image to be shared. Must be specified if image or media are not specified.
        public string pathToCSS; // string? A path to the CSS from index.html used for classes in the XML. Optional, but necessary if className is used in the imageOverlay XML.
        public Json initialData; // Object? A blob of data used in the text or in the imageOverlay XML. Optional, but necessary if either the text or imageOverlay uses developer parameters.
        public MediaParams media; // MediaParams? Optional content for the gif or video. At least one image or media should be provided in order to render the update.
        public LocalizableContent text; // (string | LocalizableContent) A text message, or an object with the default text as the value of 'default' and another object mapping locale keys to translations as the value of 'localizations'. Can use the same user data and developer parameter keys that the overlays use.
        public LocalizableContent notificationText; // (string? | LocalizableContent?) A notification message, or an object with the default notification text as the value of 'default' and another object mapping locale keys to translations as the value of 'localizations'. This value will be used to separate message (see text parameter) and notification text. If not included we will use the text property for sending a message in messenger and notification.
        public Json data; // Object? A blob of data to attach to the update. All game sessions launched from the update will be able to access this blob through FBInstant.getEntryPointData(). Must be less than or equal to 1000 characters when stringified.
        public UpdateStrategy strategy = UpdateStrategy.IMMEDIATE; // string? Specifies how the update should be delivered. This can be one of the following: 'IMMEDIATE' - The update should be posted immediately. 'LAST' - The update should be posted when the game session ends. The most recent update sent using the 'LAST' strategy will be the one sent. 'IMMEDIATE_CLEAR' - The update is posted immediately, and clears any other pending updates (such as those sent with the 'LAST' strategy). If no strategy is specified, we default to 'IMMEDIATE'.
        public UpdateNotificationType notification = UpdateNotificationType.NO_PUSH; // string? Specifies notification setting for the custom update. This can be 'NO_PUSH' or 'PUSH', and defaults to 'NO_PUSH'. Use push notification only for updates that are high-signal and immediately actionable for the recipients. Also note that push notification is not always guaranteed, depending on user setting and platform policies.

        public Json ToJson()
        {
            Json json = Json.Object();
            if (action != null) json.Prop("action", action);
            if (template != null) json.Prop("template", template);
            if (cta != null) json.Obj("cta", cta.ToJson());
            if (image != null) json.Prop("image", image);
            if (imageOverlayPath != null) json.Prop("imageOverlayPath", imageOverlayPath);
            if (pathToCSS != null) json.Prop("pathToCSS", pathToCSS);
            if (initialData != null) json.Obj("initialData", initialData);
            // json.Obj("media", media.ToJson());
            // UnityEngine.Debug.Log("media");
            if (text != null) json.Obj("text", text.ToJson());
            if (notificationText != null) json.Obj("notificationText", notificationText.ToJson());
            if (data != null) json.Obj("data", data);
            json.Prop("strategy", strategy.ToValue());
            json.Prop("notification", notification.ToValue());

            return json;
        }
    }
    public enum UpdateStrategy
    {
        IMMEDIATE, // - The update should be posted immediately. This is the default.
        LAST, // - The update should be posted when the game session ends. The most recent update sent using the 'LAST' strategy will be the one sent.
        IMMEDIATE_CLEAR, // - The update is posted immediately, and clears any other pending updates (such as those sent with the 'LAST' strategy).
    }
    public static class UpdateStrategyExtensions
    {
        public static string ToValue(this UpdateStrategy strategy)
        {
            return strategy switch
            {
                UpdateStrategy.IMMEDIATE => "IMMEDIATE",
                UpdateStrategy.LAST => "LAST",
                UpdateStrategy.IMMEDIATE_CLEAR => "IMMEDIATE_CLEAR",
                _ => strategy.ToString()
            };
        }
    }

    public enum UpdateNotificationType
    {
        NO_PUSH,
        PUSH
    }
    public static class UpdateNotificationTypeExtensions
    {
        public static string ToValue(this UpdateNotificationType notificationType)
        {
            return notificationType switch
            {
                UpdateNotificationType.NO_PUSH => "NO_PUSH",
                UpdateNotificationType.PUSH => "PUSH",
                _ => notificationType.ToString()
            };
        }
    }
}
