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
    public enum InviteFilter
    {
        NEW_CONTEXT_ONLY,
        NEW_PLAYERS_ONLY,
        EXISTING_CONTEXT_ONLY,
        EXISTING_PLAYERS_ONLY
    }

    public static class InviteFilterExtensions
    {
        public static string ToValue(this InviteFilter filter)
        {
            return filter switch
            {
                InviteFilter.NEW_CONTEXT_ONLY => "NEW_CONTEXT_ONLY",
                InviteFilter.NEW_PLAYERS_ONLY => "NEW_PLAYERS_ONLY",
                InviteFilter.EXISTING_CONTEXT_ONLY => "EXISTING_CONTEXT_ONLY",
                InviteFilter.EXISTING_PLAYERS_ONLY => "EXISTING_PLAYERS_ONLY",
                _ => filter.ToString()
            };
        }
    }
}
