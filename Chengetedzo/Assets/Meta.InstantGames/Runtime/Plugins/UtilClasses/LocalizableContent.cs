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
    public class Localization
    {
        public string country;
        public string text;
        public Localization(string _country, string _text)
        {
            this.country = _country;
            this.text = _text;
        }
        public Json ToJson()
        {
            return Json.Object()
            .Prop(country, text);
        }
        public override string ToString()
        {
            return "\"" + country + "\" : \"" + text + "\"";
        }
    }

    public class LocalizableContent
    {
        public string defaultText;// string The default value of the string to use if the viewer's locale is not a key in the localizations object.
                                  // public Dictionary<string, string> localizations = new Dictionary<string, string>();// LocalizationsDict Specifies what string to use for viewers in each locale. See https://lookaside.facebook.com/developers/resources/?id=FacebookLocales.xml for a complete list of supported locale values.
        public List<Localization> localizations = new List<Localization>();
        public LocalizableContent() { }
        public LocalizableContent(string _defaultText)
        {
            defaultText = _defaultText;
            localizations = new List<Localization>();
        }

        public override string ToString() { return ToJson().ToString(); }

        public Json ToJson()
        {
            if (localizations.Count == 0)
            {
                return Json.Object().Prop("default", defaultText);
            }

            return Json.Object()
                .Prop("default", defaultText)
                .Obj("localizations", (args) =>
                {
                    foreach (var loc in localizations)
                    {
                        args.Add(loc.country, loc.text);
                    }
                }
            );

        }
    }
}
