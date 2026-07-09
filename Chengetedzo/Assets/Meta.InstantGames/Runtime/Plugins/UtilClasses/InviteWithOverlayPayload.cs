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

using System.Collections.Generic;

namespace Meta.InstantGames
{
    public class InviteWithOverlayPayload
    {
        public string image; // string? Optional, but must be specified if imageOverlayPath is not specified.
        public string imageOverlayPath; // string? A path relative from the root of the game to the XML that represents the image to be shared. Optional, but must be specified if the image property is not specified.
        public string pathToCSS; // string? A path to the CSS from index.html used for classes in the XML. Optional, but necessary if className is used in the imageOverlay XML.
        public Json initialData; // Object? A blob of data used in the text or in the imageOverlay XML. Optional, but necessary if either the text or imageOverlay uses developer parameters.
        public LocalizableContent text; // (string | LocalizableContent) A text message, or an object with the default text as the value of 'default' and another object mapping locale keys to translations as the value of 'localizations'. Can use the same user data and developer parameter keys that the overlays use.
        public LocalizableContent notificationText; // (string? | LocalizableContent?) A notification message, or an object with the default notification text as the value of 'default' and another object mapping locale keys to translations as the value of 'localizations'. This value will be used to separate message (see text parameter) and notifcation text. If not included we will use the text property for sending message in messenger and notification.
        public LocalizableContent cta; // (string? | LocalizableContent?) Optional call-to-action button text. By default we will use a localized 'Play' as the button text. To provide localized versions of your own call to action, pass an object with the default cta as the value of 'default' and another object mapping locale keys to translations as the value of 'localizations'.
        public LocalizableContent dialogTitle; // (string? | LocalizableContent?) An optional title to display at the top of the invite dialog instead of the generic title. This param is not sent as part of the message, but only displays in the dialog header. The title can be either a string or an object with the default text as the value of 'default' and another object mapping locale keys to translations as the value of 'localizations'.
        public Json data; // Object? A blob of data to attach to the share. All game sessions launched from the share will be able to access this blob through FBInstant.getEntryPointData().
        public List<InviteFilter> filters; // Array<InviteFilter>? The set of filters to apply to the suggestions. Multiple filters may be applied. If no results are returned when the filters are applied, the results will be generated without the filters.
        public List<InviteSection> sections; // Array<InviteSection>? The set of sections to be included in the dialog. Each section can be assigned a maximum number of results to be returned (up to a maximum of 10). If no max is included, a default max will be applied. Sections will be included in the order they are listed in the array. The last section will include a larger maximum number of results, and if a maxResults is provided, it will be ignored. If this array is left empty, default sections will be used.

        public Json ToJson()
        {
            Json payload = Json.Object();

            if (image != null) payload.Prop("image", image);
            if (imageOverlayPath != null) payload.Prop("imageOverlayPath", imageOverlayPath);
            if (pathToCSS != null) payload.Prop("pathToCSS", pathToCSS);
            if (text != null) payload.Obj("text", text.ToJson());
            if (notificationText != null) payload.Obj("notificationText", notificationText.ToJson());
            if (dialogTitle != null) payload.Obj("dialogTitle", dialogTitle.ToJson());
            if (cta != null) payload.Obj("cta", cta.ToJson());
            if (initialData != null) payload.Obj("initialData", initialData);
            if (data != null) payload.Obj("data", data);

            if (filters != null && filters.Count > 0)
            {
                string[] filterArray = new string[filters.Count];
                for (int i = 0; i < filters.Count; i++)
                {
                    filterArray[i] = filters[i].ToValue();
                }
                payload.Array("filters", filterArray);
            }

            if (sections != null && sections.Count > 0)
            {
                payload.ArrayOfObjects("sections", (inv) =>
                {
                    foreach (InviteSection section in sections)
                    {
                        inv.Add(section.ToJson());
                    }
                });
            }

            return payload;
        }
    }
}
