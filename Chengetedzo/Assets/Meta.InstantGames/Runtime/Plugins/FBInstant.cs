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
using System;
using System.Collections.Generic;
namespace Meta.InstantGames
{
    public class FBInstant
    {
        public static string testBase64Image = "data:image/png;base64,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";

        // Simple Singleton pattern for FBInstant
        public static FBInstant instance;
        public Player _player;
        public static Player Player => Instance._player;
        public OverlayViews _overlayViews;
        public static OverlayViews OverlayViews => Instance._overlayViews;
        public Community _community;
        public static Community Community => Instance._community;
        public Context _context;
        public static Context Context => Instance._context;
        public Tournament _tournament;
        public static Tournament Tournament => Instance._tournament;
        public Payment _payment;
        public static Payment Payment => Instance._payment;
        public Room _room;
        public static Room Room => Instance._room;
        public static FBInstant Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new FBInstant();
                }
                return instance;
            }
        }

        public Action<string> callbackAction;

        public List<AdInstance> adInstanceList = new List<AdInstance>();

        // Private constructor prevents external instantiation
        private FBInstant()
        {
            _player = new Player();
            _overlayViews = new OverlayViews();
            _community = new Community();
            _context = new Context();
            _tournament = new Tournament();
            _payment = new Payment();
            _room = new Room();
        }
        public static async Task<string> GetScreenshot()
        {
            string base64Image = await WebGLBridge.GetScreenshotAsync();
            return base64Image;
        }
        public static async Task<string> GetCanvasRect()
        {
            string position = await WebGLBridge.GetCanvasRect();
            return position;
        }
        public static async Task<string> InviteAsync(InviteWithOverlayPayload invitePayload)
        {
            string payload = Json.Object()
               .ArrayOfObjects("args", args => args
                   .Add(item => item
                       .Prop("type", "object")
                       .Obj("value", invitePayload.ToJson())
                   )
               ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("inviteAsync", payload);
            return result;
        }
        public static async Task<string> GetLocale()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("getLocale");
            return result.Trim('"');
        }
        public static async Task<string> GetPlatform()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("getPlatform");
            return result.Trim('"');
        }
        public static async Task<string> GetSDKVersion()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("getSDKVersion");
            return result.Trim('"');
        }
        public static async Task<string> InitializeAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("initializeAsync");
            return result;
        }
        public static async Task<string> ShareAsync(ShareWithOverlayPayload sharePayload)
        {
            string payload = Json.Object()
               .ArrayOfObjects("args", args => args
                   .Add(item => item
                       .Prop("type", "object")
                       .Obj("value", sharePayload.ToJson())
                   )
               ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("shareAsync", payload);
            return result;
        }
        public static async Task<string> PostSessionScore(int score)
        {
            string payload = Json.Object()
             .ArrayOfObjects("args", args => args
                 .Add(item => item
                     .Prop("type", "int")
                     .Prop("value", score))
             ).ToString();
            string result = await WebGLBridge.CallAsyncJavaScript("postSessionScore", payload);
            return result;
        }

        public static async Task<string> PostSessionScoreAsync(int score)
        {
            string payload = Json.Object()
                 .ArrayOfObjects("args", args => args
                     .Add(item => item
                         .Prop("type", "int")
                         .Prop("value", score))
                 ).ToString();
            string result = await WebGLBridge.CallAsyncJavaScript("postSessionScoreAsync", payload);
            return result;
        }
        public static async Task<TournamentInstance> GetTournamentAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("getTournamentAsync");
            TournamentInstance tournament = new TournamentInstance(result);
            return tournament;
        }
        public static async Task<string> PerformHapticFeedbackAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("performHapticFeedbackAsync");
            return result;
        }
        public static async Task<string> UpdateAsync(CustomUpdateWithOverlayPayload updatePayload)
        {
            string payload = Json.Object()
            .ArrayOfObjects("args", args => args
                .Add(item => item
                    .Prop("type", "object")
                    .Obj("value", updatePayload.ToJson())
                )).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("updateAsync", payload);
            return result;
        }
        public static async Task<string> SwitchGameAsync(string gameId, string data)
        {
            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", gameId))
                    .Add(item => item
                        .Prop("type", "obj")
                        .Prop("value", data))
                ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("switchGameAsync", payload);
            return result;
        }
        public static async Task<string> CanCreateShortcutAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("canCreateShortcutAsync");
            return result;
        }
        public static async Task<string> CreateShortcutAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("createShortcutAsync");
            return result;
        }

        public static async Task<string> SetLoadingProgress(int loadingProgress)
        {
            string payload = Json.Object()
           .ArrayOfObjects("args", args => args
               .Add(item => item
                   .Prop("type", "int")
                   .Prop("value", loadingProgress))
           ).ToString();
            string result = await WebGLBridge.CallAsyncJavaScript("setLoadingProgress", payload);
            return result;

        }
        public static async Task<string> GetSupportedAPIs()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("getSupportedAPIs");
            return result;
        }
        public static async Task<string> GetEntryPointData()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("getEntryPointData");
            return result;

        }
        public static async Task<string> GetEntryPointAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("getEntryPointAsync");
            return result;

        }
        public static async Task<string> SetSessionData(string sessionData)
        {
            string payload = Json.Object()
           .ArrayOfObjects("args", args => args
               .Add(item => item
                   .Prop("type", "object")
                   .Prop("value", sessionData)
               )).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("setSessionData", payload);
            return result;
        }

        public static async Task<string> StartGameAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("startGameAsync");
            return result;
        }

        public static async void Quit()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("quit");
        }

        public static async Task<string> LogEvent(string eventName, string eventValue, string data)
        {
            string payload = Json.Object()
           .ArrayOfObjects("args", args => args
               .Add(item => item
                   .Prop("type", "string")
                   .Prop("value", eventName)
               )
               .Add(item => item
                   .Prop("type", "string")
                   .Prop("value", eventValue)
               )
               .Add(item => item
                   .Prop("type", "object")
                   .Prop("value", data)
               //    .Obj("value", payload => payload
               //        .Prop("someData1", "CUSTOM")
               //        .Prop("someData2", "Join The Fight")
               //        .Obj("someData3", data => data
               //            .Prop("level", "level-123")
               //            .Prop("score", 123)
               //            .Prop("isAdmin", true)
               //        )
               //    )
               )).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("logEvent", payload);
            return result;

        }

        public static async Task<string> LoadBannerAdAsync(string bannerId, string position)
        {
            string payload = Json.Object()
           .ArrayOfObjects("args", args => args
               .Add(item => item
                   .Prop("type", "string")
                   .Prop("value", bannerId))
               .Add(item => item
                   .Prop("type", "string")
                   .Prop("value", position))
           ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("loadBannerAdAsync", payload);
            return result;
        }
        public static async Task<string> HideBannerAdAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("hideBannerAdAsync");
            return result;


        }
        public static async Task<AdInstance> GetInterstitialAdAsync(string placementID)
        {

            string payload = Json.Object()
            .ArrayOfObjects("args", args => args
                .Add(item => item
                    .Prop("type", "string")
                    .Prop("value", placementID))
            ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("getInterstitialAdAsync", payload);
            AdInstance adInstance = new AdInstance(result);
            return adInstance;

        }

        public static async Task<AdInstance> GetRewardedVideoAsync(string placementID)
        {

            string payload = Json.Object()
            .ArrayOfObjects("args", args => args
                .Add(item => item
                    .Prop("type", "string")
                    .Prop("value", placementID))
            ).ToString();

            SDKDebug.Log("[FBInstant] GetRewardedVideoAsync: calling CallAsyncJavaScript...");
            string result = await WebGLBridge.CallAsyncJavaScript("getRewardedVideoAsync", payload);
            SDKDebug.Log($"[FBInstant] GetRewardedVideoAsync: await RESUMED, result is null={result == null}, length={result?.Length ?? -1}, value=[{result}]");
            AdInstance adInstance = new AdInstance(result);
            SDKDebug.Log($"[FBInstant] GetRewardedVideoAsync: AdInstance created: {adInstance}");
            return adInstance;
        }

        public static async Task<AdInstance> GetRewardedInterstitialAsync(string placementID)
        {

            string payload = Json.Object()
            .ArrayOfObjects("args", args => args
                .Add(item => item
                    .Prop("type", "string")
                    .Prop("value", placementID))
            ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("getRewardedInterstitialAsync", payload);
            AdInstance adInstance = new AdInstance(result);
            return adInstance;
        }


        public static async Task<string> CheckCanPlayerMatchAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("checkCanPlayerMatchAsync");
            return result;
        }

        public static async Task<string> MatchPlayerAsync(string matchTag, bool switchContextWhenMatched, bool offlineMatch)
        {
            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", matchTag))
                    .Add(item => item
                        .Prop("type", "boolean")
                        .Prop("value", switchContextWhenMatched))
                    .Add(item => item
                        .Prop("type", "boolean")
                        .Prop("value", offlineMatch))
                ).ToString();
            string result = await WebGLBridge.CallAsyncJavaScript("matchPlayerAsync", payload);//JsonUtility.ToJson(args));
            return result;

        }

        public static async Task<string> OpenExternalLinkAsync(string url)
        {
            string payload = Json.Object()
                .ArrayOfObjects("args", args => args
                    .Add(item => item
                        .Prop("type", "string")
                        .Prop("value", url))
                ).ToString();
            string result = await WebGLBridge.CallAsyncJavaScript("openExternalLinkAsync", payload);
            return result;
        }

        public static async Task<string> GetAssociatedAppsASIDAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("getAssociatedAppsASIDAsync");
            return result;
        }

        public static async Task<string> GetSignedAssociatedAppsASIDAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("getSignedAssociatedAppsASIDAsync");
            return result;
        }

        public static void OnPause(Action<string> callback)
        {
            WebGLBridge.RegisterPersistentCallback("onPause", callback);
        }

        public static void OnContextChange(Action<string> successCallback, Action<string> errorCallback)
        {
            WebGLBridge.RegisterPersistentCallback("onContextChangeSuccess", successCallback);
            WebGLBridge.RegisterPersistentCallback("onContextChangeError", errorCallback);
        }
    }
}
