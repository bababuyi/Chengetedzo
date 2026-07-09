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
    public class Tournament
    {
        public async Task<string> CreateAsync(int initialScore, CreateTournamentConfig tournamentConfig, Json inputData = null)
        {
            var valueBuilder = Json.Object()
                .Prop("initialScore", initialScore)
                .Obj("config", tournamentConfig.ToJson());
            if (inputData != null) valueBuilder.Obj("data", inputData);

            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "obj")
                        .Obj("value", valueBuilder)
                )).ToString();

            SDKDebug.Log("pre CreateAsync payload: " + payload);
            string result = await WebGLBridge.CallAsyncJavaScript("tournament.createAsync", payload);
            SDKDebug.Log("post CreateAsync payload: " + payload);
            return result;
        }

        public async Task<TournamentInstance[]> GetTournamentsAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("tournament.getTournamentsAsync");

            try
            {
                JsonValue ids = JsonParser.Parse(result);
                if (ids != null)
                {
                    List<TournamentInstance> newList = new List<TournamentInstance>(ids.Count);
                    foreach (JsonValue id in ids.ArrayElements)
                    {
                        TournamentInstance newTournamentInstance = new TournamentInstance(id.ToString());
                        newList.Add(newTournamentInstance);
                    }
                    return newList.ToArray();
                }
            }
            catch (System.Exception ex)
            {
                SDKDebug.LogError("Failed to parse tournament instance JSON: " + ex.Message);
                return null;
            }
            return null;
        }
        public async Task<string> JoinAsync(string tournamentId)
        {
            string payload = Json.Object()
                   .ArrayOfObjects("args", args => args
                       .Add(item => item
                           .Prop("type", "string")
                           .Prop("value", tournamentId)
                       )
                   ).ToString();
            string result = await WebGLBridge.CallAsyncJavaScript("tournament.joinAsync", payload);
            return result;
        }
        public async Task<string> PostScoreAsync(int score)
        {
            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "int")
                        .Prop("value", score)
                    )
            ).ToString();
            string result = await WebGLBridge.CallAsyncJavaScript("tournament.postScoreAsync", payload);
            return result;
        }
        public async Task<string> ShareAsync(ShareTournamentPayload sharePayload)
        {
            var valueBuilder = Json.Object()
                .Prop("score", sharePayload.score);
            if (sharePayload.data != null) valueBuilder.Obj("data", sharePayload.data);

            string payload = Json.Object()
                        .ArrayOfObjects("args", args => args
                            .Add(item => item
                            .Prop("type", "obj")
                            .Obj("value", valueBuilder))
                    ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("tournament.shareAsync", payload);
            return result;
        }
    }


}
