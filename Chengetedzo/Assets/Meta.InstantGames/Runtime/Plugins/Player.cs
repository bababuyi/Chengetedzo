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
using System;

namespace Meta.InstantGames
{
    /// <summary>
    /// Exception thrown when JSON parsing fails in the Instant Games SDK.
    /// </summary>
    public class InstantGamesParseException : Exception
    {
        public InstantGamesParseException(string message) : base(message) { }
        public InstantGamesParseException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Provides access to player-related functionality in the FBInstant API.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Gets the unique identifier for the current player.
        /// </summary>
        /// <returns>A task that resolves to the player's unique ID.</returns>
        public async Task<string> GetID()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("player.getID");
            return result;
        }
        public async Task<string> GetASIDAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("player.getASIDAsync");
            return result;

        }
        public async Task<SignedASID> GetSignedASIDAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("player.getSignedASIDAsync");
            if (string.IsNullOrEmpty(result) || result == "null") return null;
            SignedASID signedASID = new SignedASID(result);
            return signedASID;
        }
        public async Task<SignedPlayerInfo> GetSignedPlayerInfoAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("player.getSignedPlayerInfoAsync");
            SignedPlayerInfo signedPlayerInfo = new SignedPlayerInfo(result);
            return signedPlayerInfo;
        }
        public async Task<string> CanSubscribeBotAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("player.canSubscribeBotAsync");
            return result;
        }
        public async Task<string> IsSubscribedBotAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("player.isSubscribedBotAsync");
            return result;
        }
        public async Task<string> SubscribeBotAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("player.subscribeBotAsync");
            return result;
        }
        public async Task<string> GetDataAsync(string[] getData)
        {
            string payload = Json.Object()
            .ArrayOfObjects("args", args => args
                .Add(item => item
                    .Prop("type", "array")
                    .Array("value", getData)
                )
            ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("player.getDataAsync", payload);
            return result;

        }
        public async Task<string> SetDataAsync(string setData)
        {
            string payload = Json.Object()
          .ArrayOfObjects("args", args => args
              .Add(item => item
                  .Prop("type", "object")
                  .Prop("value", setData)
                )
           ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("player.setDataAsync", payload);
            return result;
        }
        public async Task<string> FlushDataAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("player.flushDataAsync");
            return result;
        }
        /// <summary>
        /// Gets a list of players who have played the game with the current player.
        /// </summary>
        /// <returns>A task that resolves to an array of connected players.</returns>
        /// <exception cref="InstantGamesParseException">Thrown when the response JSON cannot be parsed.</exception>
        public async Task<string> CreateNEZPNotificationContentAsync(string label, string value)
        {
            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", label))
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", value))
                ).ToString();
            string result = await WebGLBridge.CallAsyncJavaScript("player.createNEZPNotificationContentAsync", payload);
            return result;
        }

        public async Task<ConnectedPlayer[]> GetConnectedPlayersAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("player.getConnectedPlayersAsync");
            try
            {
                JsonValue ids = JsonParser.Parse(result);
                if (ids != null)
                {
                    List<ConnectedPlayer> newList = new List<ConnectedPlayer>(ids.Count);
                    foreach (JsonValue id in ids.ArrayElements)
                    {
                        ConnectedPlayer newConnectedPlayer = new ConnectedPlayer(id.ToString());
                        newList.Add(newConnectedPlayer);
                    }
                    return newList.ToArray();
                }
                return Array.Empty<ConnectedPlayer>();
            }
            catch (System.Exception ex)
            {
                throw new InstantGamesParseException("Failed to parse connected players JSON: " + ex.Message, ex);
            }
        }
    }

}
