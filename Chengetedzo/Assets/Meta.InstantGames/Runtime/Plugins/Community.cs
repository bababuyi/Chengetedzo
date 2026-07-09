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
using UnityEngine;

namespace Meta.InstantGames
{
    public class Community
    {
        public async Task<string> CanFollowOfficialPageAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("community.canFollowOfficialPageAsync");
            return result;
        }
        public async Task<string> CanJoinOfficialGroupAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("community.canJoinOfficialGroupAsync");
            return result;
        }
        public async Task FollowOfficialPageAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("community.followOfficialPageAsync");
        }
        public async Task JoinOfficialGroupAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("community.joinOfficialGroupAsync");
        }
    }
}
