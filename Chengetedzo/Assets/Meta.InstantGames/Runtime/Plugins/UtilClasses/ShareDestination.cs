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
    public enum ShareDestination
    {
        NEWSFEED, //: Enable share to newsfeed option
        GROUP, //: Enable share to official game group option. This is only available for games with official game group. To set up official game group, add a page in the game app setting in https://www.developers.facebook.com, and then create a group for the page in https://facebook.com.
        COPY_LINK, //: Enable copy the game link in clipboard
        MESSENGER, //: Enable share game to messenger option
    }

    public static class ShareDestinationExtensions
    {
        public static string ToValue(this ShareDestination destination)
        {
            return destination switch
            {
                ShareDestination.NEWSFEED => "NEWSFEED",
                ShareDestination.GROUP => "GROUP",
                ShareDestination.COPY_LINK => "COPY_LINK",
                ShareDestination.MESSENGER => "MESSENGER",
                _ => destination.ToString()
            };
        }
    }
}
