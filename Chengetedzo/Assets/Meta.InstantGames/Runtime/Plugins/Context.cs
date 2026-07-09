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
using System.Collections.Generic;

namespace Meta.InstantGames
{
    public class Context
    {

        public async Task<string> GetID()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("context.getID");
            return result;
        }
        public async Task SwitchAsync(string id, bool switchSilentlyIfSolo)
        {
            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", id)
                    )
                    .Add(item => item
                        .Prop("type", "bool")
                        .Prop("value", switchSilentlyIfSolo)
                    )
                )
            .ToString();
            string result = await WebGLBridge.CallAsyncJavaScript("context.switchAsync", payload);
        }

        public async Task<string> CreateAsync(string[] playerIds)
        {
            //0,1 or many.
            string result = "";
            switch (playerIds.Length)
            {
                case 0:
                    result = await WebGLBridge.CallAsyncJavaScript("context.createAsync");
                    break;
                case 1:
                    string singlePayload = Json.Object()
                    .ArrayOfObjects("args", args => args
                        .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", playerIds[0])))
                    .ToString();
                    result = await WebGLBridge.CallAsyncJavaScript("context.createAsync", singlePayload);
                    break;
                default:
                    string groupPayload = Json.Object()
                        .ArrayOfObjects("args", args => args
                            .Add(item => item
                            .Prop("type", "array")
                            .Array("value", playerIds)))
                        .ToString();
                    result = await WebGLBridge.CallAsyncJavaScript("context.createAsync", groupPayload);
                    break;
            }

            return result;

        }

        public async Task<ContextPlayer[]> GetPlayersAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("context.getPlayersAsync");

            try
            {
                JsonValue ids = JsonParser.Parse(result);
                if (ids != null)
                {
                    List<ContextPlayer> newList = new List<ContextPlayer>(ids.Count);
                    foreach (JsonValue id in ids.ArrayElements)
                    {
                        ContextPlayer newContextPlayer = new ContextPlayer(id.ToString());
                        newList.Add(newContextPlayer);
                    }
                    return newList.ToArray();
                }
            }
            catch (System.Exception ex)
            {
                SDKDebug.LogError("Failed to parse connected players JSON: " + ex.Message);
                return null;
            }
            return null;
        }

        public async Task ChooseAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("context.chooseAsync");
        }

    }
}
