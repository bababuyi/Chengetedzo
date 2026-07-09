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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Meta.InstantGames;
using System;
public class PluginExample : MonoBehaviour
{
    public CanvasGroup cg;
    public Transform parentTransform;
    public List<GameObject> buttons;
    public DebugLogRedirect debug;

    void Start() { CreateFunctionMenu(); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) WebGLBridge.LogCache();
        if (Input.GetKeyDown(KeyCode.X)) debug.ToggleCG();
        if (Input.GetKeyDown(KeyCode.C)) ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (cg.alpha == 0)
        {
            cg.alpha = 1;
            cg.blocksRaycasts = true;
        }
        else
        {
            cg.alpha = 0;
            cg.blocksRaycasts = false;
        }
    }

    [ContextMenu("CreateFunctionMenu")]
    public void CreateFunctionMenu()
    {
        AddHelperFunctions();
        AddRootFunctions();
        AddPlayerFunctions();
        AddCommunityFunctions();
        AddContextFunctions();
        AddTournamentFunctions();
        AddPaymentFunctions();
        AddRoomFunctions();
        AddOverlayViewFunctions();
    }
    #region Helper Functions
    void AddHelperFunctions()
    {
        buttons.Add(CreateButton("Helpers", Color.white, null));
        buttons.Add(CreateButton("Init Callbacks", Color.green, () =>
               {
                   //register this callback action in the startup
                   WebGLBridge.InitCallbacks();
               }));

        buttons.Add(CreateButton("GetCanvasRect", Color.green, async () =>
       {
           string canvasRect = await FBInstant.GetCanvasRect();
           SDKDebug.Log("getCanvasRect :" + canvasRect);
       }));

    }
    #endregion
    #region FBInstant Root Functions
    void AddRootFunctions()
    {
        buttons.Add(CreateButton("FBInstant", Color.white, null));

        #region Social Functions
        buttons.Add(CreateButton("InviteAsync", Color.green, async () =>
        {
            // string screenshot = await FBInstant.getScreenshot();
            // SDKDebug.Log("base64Image:" + screenshot);

            InviteWithOverlayPayload payloadObj = new InviteWithOverlayPayload()
            {
                image = FBInstant.testBase64Image,
                pathToCSS = "./overlays/",
                // data = Json.Object(),
                text = new LocalizableContent()
                {
                    defaultText = "Default Text Here",
                    localizations = new List<Localization>(){
                       new Localization("en_US", "English Text Here"),
                       new Localization("ar_AR", "\u0627\u0646\u0636\u0645 \u0625\u0644\u0649"
                        +                "\u0627\u0644\u0642\u062A\u0627\u0644!"),
                       new Localization("es_LA", "\u00A1\u00DAnete a la pelea!")
                    }
                },
                notificationText = new LocalizableContent()
                {
                    defaultText = "Notification Text Here",
                    localizations = new List<Localization>(){
                       new Localization("en_US", "English Text Here"),
                       new Localization("ar_AR", "\u0627\u0646\u0636\u0645 \u0625\u0644\u0649"
                        +                "\u0627\u0644\u0642\u062A\u0627\u0644!"),
                       new Localization("es_LA", "\u00A1\u00DAnete a la pelea!")
                    }
                },
                cta = new LocalizableContent()
                {
                    defaultText = "CTA Text Here",
                    localizations = new List<Localization>(){
                       new Localization("en_US", "English Text Here"),
                       new Localization("ar_AR", "\u0627\u0646\u0636\u0645 \u0625\u0644\u0649"
                        +                "\u0627\u0644\u0642\u062A\u0627\u0644!"),
                       new Localization("es_LA", "\u00A1\u00DAnete a la pelea!")
                    }
                },
                dialogTitle = new LocalizableContent()
                {
                    defaultText = "Dialog Text Here",
                    localizations = new List<Localization>(){
                       new Localization("en_US", "English Text Here"),
                       new Localization("ar_AR", "\u0627\u0646\u0636\u0645 \u0625\u0644\u0649"
                        +                "\u0627\u0644\u0642\u062A\u0627\u0644!"),
                       new Localization("es_LA", "\u00A1\u00DAnete a la pelea!")
                    }
                },
                // data = "",
                filters = new List<InviteFilter>
                {
                    InviteFilter.NEW_CONTEXT_ONLY,
                    InviteFilter.EXISTING_PLAYERS_ONLY
                },
                sections = new List<InviteSection>
                {
                    new InviteSection(InviteSectionType.GROUPS, 2),
                    new InviteSection(InviteSectionType.USERS, 10)
                }
            };

            await FBInstant.InviteAsync(payloadObj);
        }));

        buttons.Add(CreateButton("ShareAsync", Color.green, async () =>
                {
                    string screenshot = FBInstant.testBase64Image;

                    ShareWithOverlayPayload sharePayload = new ShareWithOverlayPayload()
                    {
                        intent = ShareIntent.SHARE,
                        image = screenshot,
                        initialData = Json.Object(),
                        media = null,
                        text = "Share Text here",
                        notificationText = "Notification Text here",
                        data = Json.Object(),
                        shareDestination = new ShareDestination[] {
                            ShareDestination.NEWSFEED,
                            ShareDestination.GROUP,
                            ShareDestination.COPY_LINK,
                            ShareDestination.MESSENGER,
                        },
                        surface = "FACEBOOK",
                        switchContext = true,
                    };

                    await FBInstant.ShareAsync(sharePayload);
                }));

        buttons.Add(CreateButton("ShareAsyncOverlay", Color.green, async () =>
        {
            ShareWithOverlayPayload sharePayload = new ShareWithOverlayPayload()
            {
                intent = ShareIntent.SHARE,
                imageOverlayPath = "./ig_views/share.xml",
                pathToCSS = "./ig_views/share.css",
                text = "Overlay Share Text here",
                notificationText = "Overlay Notification Text here",
                initialData = Json.Object()
                    .Prop("score", 6767)
                    .Prop("imagePath", "/share_image.jpeg")
                    ,
                data = Json.Object()
                    .Prop("division", "result_high_score")
                    ,
                surface = "FACEBOOK",
                switchContext = true,
            };

            await FBInstant.ShareAsync(sharePayload);
        }));
        buttons.Add(CreateButton("PostSessionScore", Color.green, async () =>
        {
            int score = 123455;
            await FBInstant.PostSessionScore(score);
        }));
        buttons.Add(CreateButton("PostSessionScoreAsync", Color.green, async () =>
        {
            int score = 123455;
            await FBInstant.PostSessionScoreAsync(score);
        }));
        buttons.Add(CreateButton("GetTournamentAsync", Color.green, async () =>
        {
            await FBInstant.GetTournamentAsync();
        }));


        buttons.Add(CreateButton("UpdateAsync", Color.green, async () =>
        {
            CustomUpdateWithOverlayPayload payload = new CustomUpdateWithOverlayPayload()
            {
                action = "CUSTOM",
                cta = new LocalizableContent("Join The Fight"),
                text = new LocalizableContent("X just invaded Y's village!"),
                notificationText = new LocalizableContent("X just performed B action, play now!"),
                template = "play_turn", //found in fbapp-config.json
                strategy = UpdateStrategy.IMMEDIATE,
                notification = UpdateNotificationType.PUSH,
                image = FBInstant.testBase64Image,
                data = Json.Object()
                        .Obj("data", data => data
                            .Prop("level", "level-123")
                            .Prop("score", 123)
                            .Prop("isAdmin", true)
                        )
            };
            SDKDebug.Log("updateAsync object created? ");
            string result = await FBInstant.UpdateAsync(payload);
            SDKDebug.Log(result);
        }));
        #endregion

        #region Platform Functions

        buttons.Add(CreateButton("GetLocale", Color.green, async () =>
    {
        string locale = await FBInstant.GetLocale();
        SDKDebug.Log(locale);
    }));
        buttons.Add(CreateButton("GetPlatform", Color.green, async () =>
        {
            string platform = await FBInstant.GetPlatform();
            SDKDebug.Log(platform);
        }));
        buttons.Add(CreateButton("GetSDKVersion", Color.green, async () =>
        {
            string version = await FBInstant.GetSDKVersion();
            SDKDebug.Log(version);
        }));
        buttons.Add(CreateButton("InitializeAsync", Color.green, async () =>
        {
            await FBInstant.InitializeAsync();
        }));

        buttons.Add(CreateButton("PerformHapticFeedbackAsync", Color.green, async () =>
        {
            await FBInstant.PerformHapticFeedbackAsync();
        }));

        buttons.Add(CreateButton("SwitchGameAsync", Color.green, async () =>
        {
            string gameId = "1871240696529468";
            string payload = Json.Object().ToString();
            await FBInstant.SwitchGameAsync(gameId, payload);
        }));

        buttons.Add(CreateButton("CanCreateShortcutAsync", Color.green, async () =>
        {
            await FBInstant.CanCreateShortcutAsync();
        }));
        buttons.Add(CreateButton("CreateShortcutAsync", Color.green, async () =>
        {
            await FBInstant.CreateShortcutAsync();
        }));
        buttons.Add(CreateButton("SetLoadingProgress", Color.green, async () =>
        {
            int loadingProgress = 50;
            await FBInstant.SetLoadingProgress(loadingProgress);
        }));
        buttons.Add(CreateButton("GetSupportedAPIs", Color.green, async () =>
        {
            string supportedAPIs = await FBInstant.GetSupportedAPIs();
            SDKDebug.Log(supportedAPIs);
        }));
        buttons.Add(CreateButton("GetEntryPointData", Color.green, async () =>
        {
            string entryPointData = await FBInstant.GetEntryPointData();
            SDKDebug.Log(entryPointData);
        }));
        buttons.Add(CreateButton("GetEntryPointAsync", Color.green, async () =>
        {
            string entryPointData = await FBInstant.GetEntryPointAsync();
            SDKDebug.Log(entryPointData);
        }));
        buttons.Add(CreateButton("SetSessionData", Color.green, async () =>
        {
            string sessionData = Json.Object()
                .Prop("someData1", "CUSTOM")
                .Prop("someData2", "Join The Fight")
                .Obj("someData3", data => data
                    .Prop("level", "level-123")
                    .Prop("score", 123)
                    .Prop("isAdmin", true)
            ).ToString();
            await FBInstant.SetSessionData(sessionData);
        }));
        buttons.Add(CreateButton("StartGameAsync", Color.green, async () =>
        {
            await FBInstant.StartGameAsync();
        }));
        buttons.Add(CreateButton("Quit", Color.green, () =>
        {
            FBInstant.Quit();
        }));
        buttons.Add(CreateButton("LogEvent", Color.white, async () =>
        {
            string eventName = "event_name_test";
            string eventValue = "event_value";
            string data = Json.Object()
                        .Prop("someData1", "CUSTOM")
                        .Prop("someData2", "Join The Fight")
                        .Obj("someData3", data => data
                            .Prop("level", "level-123")
                            .Prop("score", 123)
                            .Prop("isAdmin", true)
                        ).ToString();

            await FBInstant.LogEvent(eventName, eventValue, data);
        }));

        #endregion

        #region Ad Functions
        buttons.Add(CreateButton("LoadBannerAdAsync", Color.green, async () =>
        {
            //  new string[]{ "instant_games_space","banner","Banner","receving_ads","736549963194277_1618014621714469"},
            string bannerId = "736549963194277_1618014621714469";
            string position = "bottom";
            string result = await FBInstant.LoadBannerAdAsync(bannerId, position);
            SDKDebug.Log(result);
        }));
        buttons.Add(CreateButton("HideBannerAdAsync", Color.green, async () =>
        {
            await FBInstant.HideBannerAdAsync();
        }));
        buttons.Add(CreateButton("GetInterstitialAdAsync", Color.green, async () =>
        {
            // new string[]{ "instant_games_space","interstitial","Interstitial","receving_ads","736549963194277_766542526861687"},
            string adId = "736549963194277_766542526861687";
            AdInstance adInstance = await FBInstant.GetInterstitialAdAsync(adId);
            SDKDebug.Log(adInstance);
            AddAdObject(adInstance);
        }));
        buttons.Add(CreateButton("GetRewardedVideoAsync", Color.green, async () =>
        {
            // new string[]{ "instant_games_space","rewarded_video","Rewarded Video","idle","736549963194277_766542550195018"},
            string rvId = "736549963194277_766542550195018";
            AdInstance adInstance = await FBInstant.GetRewardedVideoAsync(rvId);
            SDKDebug.Log(adInstance);
            AddAdObject(adInstance);
        }));
        // buttons.Add(CreateButton("GetRewardedInterstitialAsync", Color.white, async () =>
        // {
        //     // new string[]{ "instant_games_space","rewarded_interstitial","Rewarded Video","idle","736549963194277_1759091644273432"},
        //     string riId = "736549963194277_1759091644273432";
        //     AdInstance adInstance = await FBInstant.GetRewardedInterstitialAsync(riId);
        //     SDKDebug.Log(adInstance);
        //     AddAdObject(adInstance);
        // }));
        #endregion

        #region Matchmaking Functions
        buttons.Add(CreateButton("CheckCanPlayerMatchAsync", Color.green, async () =>
        {
            await FBInstant.CheckCanPlayerMatchAsync();
        }));
        buttons.Add(CreateButton("MatchPlayerAsync", Color.green, async () =>
        {
            string matchTag = "ABCDEF";
            bool switchContextWhenMatched = true;
            bool offlineMatch = true;

            await FBInstant.MatchPlayerAsync(matchTag, switchContextWhenMatched, offlineMatch);
        }));

        // callbacks
        buttons.Add(CreateButton("OnContextChange", Color.green, () =>
        {
            // pass two functions
            FBInstant.OnContextChange((string message) =>
            {
                SDKDebug.Log("unity game onContextChange success callback called " + message);
            },
            (string message) =>
            {
                SDKDebug.Log("unity game onContextChange error callback called " + message);
            });
        }));

        #endregion
        buttons.Add(CreateButton("OnPause", Color.green, () =>
        {
            // pass a function
            FBInstant.OnPause((string message) =>
            {
                SDKDebug.Log("unity game onPause callback called " + message);
            });
        }));
    }
    #endregion
    #region Player Functions
    void AddPlayerFunctions()
    {
        buttons.Add(CreateButton("PlayerFns", Color.white, null));

        buttons.Add(CreateButton("GetID", Color.green, async () =>
        {
            string id = await FBInstant.Player.GetID();
            SDKDebug.Log("player id ; " + id);
        }));
        buttons.Add(CreateButton("GetASIDAsync", Color.green, async () =>
        {
            string asid = await FBInstant.Player.GetASIDAsync();
            SDKDebug.Log("player asid ; " + asid);
        }));
        buttons.Add(CreateButton("GetSignedASIDAsync", Color.green, async () =>
        {
            SignedASID signedAsid = await FBInstant.Player.GetSignedASIDAsync();
            SDKDebug.Log(
                signedAsid == null
                    ? "SignedASID ; null"
                    : "SignedASID ; " + signedAsid.ToString());
        }));
        buttons.Add(CreateButton("GetSignedPlayerInfoAsync", Color.green, async () =>
        {
            SignedPlayerInfo signedPlayerInfo = await FBInstant.Player.GetSignedPlayerInfoAsync();
            SDKDebug.Log("SignedPlayerInfo :" + signedPlayerInfo.ToString());
        }));
        buttons.Add(CreateButton("CanSubscribeBotAsync", Color.green, async () =>
        {
            string result = await FBInstant.Player.CanSubscribeBotAsync();
            SDKDebug.Log("result : " + result);
        }));
        buttons.Add(CreateButton("IsSubscribedBotAsync", Color.green, async () =>
        {
            string result = await FBInstant.Player.IsSubscribedBotAsync();
            SDKDebug.Log("result : " + result);
        }));
        buttons.Add(CreateButton("SubscribeBotAsync", Color.green, async () =>
        {
            string result = await FBInstant.Player.SubscribeBotAsync();
            SDKDebug.Log("result : " + result);
        }));
        buttons.Add(CreateButton("GetDataAsync", Color.green, async () =>
        {
            string[] getData = new string[] { "name", "id", "random_value", "isVIP", "player_data" };
            await FBInstant.Player.GetDataAsync(getData);
        }));

        buttons.Add(CreateButton("SetDataAsync", Color.green, async () =>
        {
            string setData = Json.Object()
                        .Prop("name", "PLAYER_NAM_" + UnityEngine.Random.Range(0, 1000))
                        .Prop("id", "123451240981254999")
                        .Prop("random_value", UnityEngine.Random.Range(0, 1000))
                        .Prop("isVIP", false)
                        .Obj("player_data", data => data
                            .Prop("head", "empty_head")
                            .Prop("body", "empty_body")
                            .Prop("legs", "empty_legs")
                        )
                        .Prop("data", "initial_data")
                        .ToString();
            await FBInstant.Player.SetDataAsync(setData);
        }));

        buttons.Add(CreateButton("FlushDataAsync", Color.green, async () =>
        {
            await FBInstant.Player.FlushDataAsync();
        }));
        buttons.Add(CreateButton("GetConnectedPlayersAsync", Color.green, async () =>
        {
            ConnectedPlayer[] connectedPlayers = await FBInstant.Player.GetConnectedPlayersAsync();
            foreach (ConnectedPlayer player in connectedPlayers)
            {
                SDKDebug.Log(player.ToString());
            }
        }));
    }
    #endregion
    #region Community Functions
    void AddCommunityFunctions()
    {
        buttons.Add(CreateButton("Community", Color.white, null));

        buttons.Add(CreateButton("CanFollowOfficialPageAsync", Color.green, async () =>
        {
            string result = await FBInstant.Community.CanFollowOfficialPageAsync();
            SDKDebug.Log(result);
        }));
        buttons.Add(CreateButton("CanJoinOfficialGroupAsync", Color.green, async () =>
        {
            string result = await FBInstant.Community.CanJoinOfficialGroupAsync();
            SDKDebug.Log(result);
        }));
        buttons.Add(CreateButton("FollowOfficialPageAsync", Color.green, async () =>
        {
            await FBInstant.Community.FollowOfficialPageAsync();
        }));
        buttons.Add(CreateButton("JoinOfficialGroupAsync", Color.green, async () =>
        {
            await FBInstant.Community.JoinOfficialGroupAsync();
        }));
    }
    #endregion
    #region TournamentFunctions
    void AddTournamentFunctions()
    {
        buttons.Add(CreateButton("Tournament", Color.white, null));

        buttons.Add(CreateButton("Tournament.CreateAsync", Color.green, async () =>
        {
            int tournamentInitialScore = 6767;
            CreateTournamentConfig tournamentConfig = new CreateTournamentConfig()
            {
                title = "My Test Weekly Challenge",
                image = FBInstant.testBase64Image,
                sortOrder = TournamentSortOrder.HIGHER_IS_BETTER,
                scoreFormat = TournamentScoreFormat.NUMERIC,
                endTime = System.DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                tournamentType = TournamentType.DEEP,
                goal = 19999
            };
            Json inputData = Json.Object();

            await FBInstant.Tournament.CreateAsync(tournamentInitialScore, tournamentConfig, inputData);
        }));
        buttons.Add(CreateButton("GetTournamentsAsync", Color.green, async () =>
        {
            TournamentInstance[] tournaments = await FBInstant.Tournament.GetTournamentsAsync();
        }));
        buttons.Add(CreateButton("JoinAsync", Color.green, async () =>
        {
            // Fetch tournaments, take the first one, and pass its id to joinAsync
            TournamentInstance[] tournaments = await FBInstant.Tournament.GetTournamentsAsync();
            if (tournaments == null || tournaments.Length == 0)
            {
                SDKDebug.LogWarning("No tournaments available to join.");
                return;
            }

            string tournamentId = tournaments[0].getID(); // assumes TournamentInstance exposes Id
            SDKDebug.Log($"Joining tournament with id: {tournamentId}");
            await FBInstant.Tournament.JoinAsync(tournamentId);
        }));

        buttons.Add(CreateButton("PostScoreAsync", Color.green, async () =>
        {
            int score = UnityEngine.Random.Range(0, 1000);
            SDKDebug.Log("Posting score to tournament: " + score);
            await FBInstant.Tournament.PostScoreAsync(score);
        }));

        buttons.Add(CreateButton("ShareAsync", Color.green, async () =>
        {
            ShareTournamentPayload sharePayload = new ShareTournamentPayload()
            {
                score = 12345,
                data = Json.Object()
                    .Prop("player_name", "PLAYER_NAME")
                    .Prop("id", 12345)
                    .Prop("isVIP", false)
            };
            await FBInstant.Tournament.ShareAsync(sharePayload);
        }));
    }
    #endregion
    #region Context Functions
    void AddContextFunctions()
    {
        buttons.Add(CreateButton("Context", Color.white, null));

        buttons.Add(CreateButton("GetID", Color.green, async () =>
        {
            string result = await FBInstant.Context.GetID();
            SDKDebug.Log("context.getID: " + result);
        }));
        buttons.Add(CreateButton("SwitchAsync", Color.white, async () =>
        {
            // get context number from source
            await FBInstant.Context.SwitchAsync("12371957", true);
        }));

        buttons.Add(CreateButton("CreateAsync-single", Color.white, async () =>
        {
            await FBInstant.Context.CreateAsync(new string[] { "1233455678" });
        }));
        buttons.Add(CreateButton("CreateAsync-group", Color.white, async () =>
        {
            await FBInstant.Context.CreateAsync(new string[]{
                "1234567",
                "1234546",
                "12480912735"
            });
        }));
        buttons.Add(CreateButton("CreateAsync-select", Color.white, async () =>
        {
            await FBInstant.Context.CreateAsync(new string[] { });
        }));

        buttons.Add(CreateButton("GetPlayersAsync", Color.green, async () =>
        {
            ContextPlayer[] contextPlayers = await FBInstant.Context.GetPlayersAsync();
            foreach (ContextPlayer player in contextPlayers)
            {
                SDKDebug.Log(player.GetID());
            }

        }));

        buttons.Add(CreateButton("ChooseAsync", Color.green, async () =>
        {
            await FBInstant.Context.ChooseAsync();
        }));
    }
    #endregion
    #region Payment Functions
    void AddPaymentFunctions()
    {
        buttons.Add(CreateButton("Payments", Color.white, null));

        buttons.Add(CreateButton("GetCatalogAsync", Color.green, async () =>
        {
            Product[] products = await FBInstant.Payment.GetCatalogAsync();
            foreach (Product product in products)
            {
                SDKDebug.Log(product.ToString());
                AddIAPObj(product);
            }
        }));
        buttons.Add(CreateButton("PurchaseAsync", Color.green, async () =>
        {
            Product[] products = await FBInstant.Payment.GetCatalogAsync();

            //take product 1.
            if (products.Length == 0) return;
            SDKDebug.Log(products[0].productID);
            PurchaseConfig purchaseConfig = new PurchaseConfig()
            {
                productID = products[0].productID,
                developerPayload = "foobar"
            };

            await FBInstant.Payment.PurchaseAsync(purchaseConfig);
        }));
        buttons.Add(CreateButton("GetPurchasesAsync", Color.white, async () =>
        {
            await FBInstant.Payment.GetPurchasesAsync();
        }));
        buttons.Add(CreateButton("ConsumePurchaseAsync", Color.white, async () =>
        {
            string productId = "123467";
            await FBInstant.Payment.ConsumePurchaseAsync(productId);
        }));
        buttons.Add(CreateButton("OnReady", Color.green, () =>
        {
            //register a function to payment.onready.
            FBInstant.Payment.OnReady((string message) =>
             {
                 SDKDebug.Log($"FBInstant.payments.onReady default callback: {message ?? "NIL"}");
             });

        }));
    }
    #endregion
    #region Room Functions
    void AddRoomFunctions()
    {
        buttons.Add(CreateButton("Room", Color.white, null));

        buttons.Add(CreateButton("GetCurrentMatchAsync", Color.white, async () =>
        {
            await FBInstant.Room.GetCurrentMatchAsync();
        }));

    }
    #endregion
    public GameObject OverlayFollowerPrefab;
    #region OverlayView Functions
    void AddOverlayViewFunctions()
    {
        buttons.Add(CreateButton("OverlayViews", Color.white, null));

        buttons.Add(CreateButton("Spawn Overlay Follower", Color.green, () =>
        {
            //add overlay follower
            GameObject newOVFollow = Instantiate(OverlayFollowerPrefab, OverlayFollowerPrefab.transform.parent);
            newOVFollow.transform.position = new Vector3(0, 0, 0);

        }));

        // buttons.Add(CreateButton("CreateOverlayView", Color.white, async () =>
        // {
        //     string pathToXML = "ig_views/example_overlay.xml";
        //     string pathToCSS = "TemplateData/style.css";
        //     string initialData = Json.Object()
        //                 .Prop("level", "createOverlayViewAsync")
        //                 .Prop("score", 2134)
        //                 .Prop("tournament", true)
        //                 .Prop("data", "initial_data")
        //                 .ToString();
        //     Action<int, string> onLoadCallback = (code, message) => SDKDebug.Log("CreateOverlayView onLoad" + code + " " + message);
        //     Action<int, string> onErrorCallback = (code, message) => SDKDebug.Log("CreateOverlayView onError" + code + " " + message);

        //     OverlayView overlayView = await FBInstant.OverlayViews.CreateOverlayView(
        //          pathToXML,
        //           pathToCSS,
        //            initialData,
        //             onLoadCallback,
        //              onErrorCallback);
        //     AddOverlayViewInstanceFunction(overlayView);

        // }));

        buttons.Add(CreateButton("CreateOverlayViewAsync", Color.green, async () =>
        {
            string pathToXML = "ig_views/example_overlay.xml";
            string domElementId = "unity-container";
            string iFrameStyle = "opacity:0; border:2px solid red; position:absolute; top:10%; left:10%; z-index: 100; width:300px; height: 200px; overflow:hidden; background-color:#fff;";
            string pathToCSS = "TemplateData/style.css";
            string initialData = Json.Object()
                        .Prop("player_name", "PLAYER_NAME_12345")
                        .Prop("id", 1234569696969)
                        .Prop("isVIP", false)
                        .Prop("isVIPStr", "false")
                        .Obj("player_data", data => data
                            .Prop("head", "empty_head")
                            .Prop("body", "empty_body")
                            .Prop("legs", "empty_legs")).ToString();

            OverlayView overlayView = await FBInstant.OverlayViews.CreateOverlayViewAsync(
                   pathToXML,
                    domElementId,
                     iFrameStyle,
                      pathToCSS,
                       initialData);
            AddOverlayViewInstanceFunction(overlayView);

        }));

        // buttons.Add(CreateButton("CreateOverlayViewWithXMLString", Color.white, async () =>
        // {

        //     string overlayViewContentOverride = @"<View>
        //                                 <View>
        //                                     <Text content = ""Player Data"" />
        //                                     <Text content = ""player_name? {{player_name}}"" />
        //                                     <Text content = ""id? {{id}}"" />
        //                                     <Text content = ""isvip? {{isVIP}}"" />
        //                                 </View>
        //                         </View>";
        //     string pathToCSS = "TemplateData/style.css";
        //     string initialData = Json.Object()
        //               .Prop("player_name", "PLAYER_NAME_12345")
        //               .Prop("id", 1234569696969)
        //               .Prop("isVIP", false)
        //               .Prop("isVIPStr", "false")
        //               .Obj("player_data", data => data
        //                   .Prop("head", "empty_head")
        //                   .Prop("body", "empty_body")
        //                   .Prop("legs", "empty_legs")
        //               ).ToString();
        //     string pathToOverlayFiles = "ig_views";
        //     Action<int, string> onLoadCallback = (code, message) => { SDKDebug.Log($"Overlay loaded: {code} - {message}"); };
        //     Action<int, string> onErrorCallback = (code, message) => { SDKDebug.Log($"Overlay error: {code} - {message}"); };

        //     OverlayView overlayView = await FBInstant.OverlayViews.CreateOverlayViewWithXMLString(
        //          overlayViewContentOverride,
        //           pathToCSS,
        //            initialData,
        //            onLoadCallback,
        //            onErrorCallback,
        //             pathToOverlayFiles);
        //     AddOverlayViewInstanceFunction(overlayView);
        // }));

        buttons.Add(CreateButton("CreateOverlayViewWithXMLStringAsync", Color.green, async () =>
        {
            string overlayViewContentOverride = @"<View>
                                        <View>
                                            <Text content = ""Player Data"" />
                                            <Text content = ""player_name? {{player_name}}"" />
                                            <Text content = ""id? {{id}}"" />
                                            <Text content = ""isvip? {{isVIP}}"" />
                                            <Text content = ""isvipSTr? {{isVIPStr}}"" />
                                            <Text content = ""pd? {{player_data}}"" />
                                            <Text content = ""pd? {{player_data.head}}"" />
                                            <Text content = ""pd? {{player_data.body}}"" />
                                            <Text content = ""pd? {{player_data.legs}}"" />
                                        </View>
                                </View>";
            string domElementId = "unity-container";
            string iFrameStyle = "border:2px solid red; z-index: 100; position:absolute; top:50%; left:50%;  width:250px; height: 250px; overflow:hidden; background-color:#fff; opacity:0; transition-duration:0.5s; transform:translate(-50%,-50%);";
            string pathToCSS = "TemplateData/style.css";
            string initialData = Json.Object()
                        .Prop("player_name", "PLAYER_NAME_12345")
                        .Prop("id", 1234569696969)
                        .Prop("isVIP", false)
                        .Prop("isVIPStr", "false")
                        .Obj("player_data", data => data
                            .Prop("head", "empty_head")
                            .Prop("body", "empty_body")
                            .Prop("legs", "empty_legs")
                        ).ToString();
            string pathToOverlayFiles = "ig_views";

            OverlayView overlayView = await FBInstant.OverlayViews.CreateOverlayViewWithXMLStringAsync(
                 overlayViewContentOverride,
                 domElementId,
                 iFrameStyle,
                  pathToCSS,
                   initialData,
                    pathToOverlayFiles);
            AddOverlayViewInstanceFunction(overlayView);

        }));

        buttons.Add(CreateButton("CreateProfilePictureOverlayViewAsync", Color.green, async () =>
        {
            string domElementId = "unity-container";
            string imageStyle = "border:none; z-index: 100; width:128px; height:128px; overflow:hidden; background:transparent;";
            string iFrameStyle = "opacity:0 ; transition-duation:0.5s; position:absolute; top: 20%; right:20%; z-index: 100; width:300px; height: 200px; overflow:hidden; background-color:#fff, border: none; background-color: transparent;";

            OverlayView overlayView = await FBInstant.OverlayViews.CreateProfilePictureOverlayViewAsync(
                domElementId,
             imageStyle,
              iFrameStyle);
            AddOverlayViewInstanceFunction(overlayView);

        }));
        buttons.Add(CreateButton("CreateProfileNameOverlayViewAsync", Color.green, async () =>
        {
            string domElementId = "unity-container";
            string textStyle = "border:none; z-index: 100; width:240px; height:60px; overflow:hidden; background:transparent;";
            string iFrameStyle = "opacity:0 ; transition-duation:0.5s; position:absolute; top: 10%; right:10%; z-index: 100; width:300px; height: 200px; overflow:hidden; background-color:#fff, border: none; background-color: transparent;";
            string pathToCSS = "TemplateData/style.css";

            OverlayView overlayView = await FBInstant.OverlayViews.CreateProfileNameOverlayViewAsync(
                domElementId,
                 textStyle,
                  iFrameStyle,
                   pathToCSS);
            AddOverlayViewInstanceFunction(overlayView);
        }));

        buttons.Add(CreateButton("SetCustomEventHandler", Color.green, () =>
        {
            FBInstant.OverlayViews.SetCustomEventHandler((string eventStr, string overlayID) =>
            {
                SDKDebug.Log("CUSTOM OVERLAYVIEW EVENT : " + eventStr + " triggered by " + overlayID);
                switch (eventStr)
                {
                    case "command-a": SDKDebug.Log("command-a called by " + overlayID); break;
                    case "command-b": SDKDebug.Log("command-b called by " + overlayID); break;
                    case "command-c": SDKDebug.Log("command-c called by " + overlayID); break;
                }
            });
        }));
        buttons.Add(CreateButton("GetOverlayViews", Color.green, async () =>
        {
            SDKDebug.Log(await FBInstant.OverlayViews.GetOverlayViews());
        }));

        buttons.Add(CreateButton("ShowAsyncAll", Color.green, async () =>
        {
            await FBInstant.OverlayViews.ShowAsyncAll();
        }));
        buttons.Add(CreateButton("UpdateAsyncAll", Color.green, async () =>
        {
            string data = Json.Object()
                    .Prop("player_name", "TEST_PLAYER")
                    .Prop("id", UnityEngine.Random.Range(0, 99999))
                    .Prop("isVIP", true)
                    .Obj("player_data", data => data
                        .Prop("head", "empty_head")
                        .Prop("body", "empty_body")
                        .Prop("legs", "empty_legs")
                    ).ToString();
            await FBInstant.OverlayViews.UpdateAsyncAll(data);
        }));
        buttons.Add(CreateButton("DismissAsyncAll", Color.green, async () =>
        {
            await FBInstant.OverlayViews.DismissAsyncAll();
        }));
        buttons.Add(CreateButton("SetStyleAll", Color.green, async () =>
        {
            await FBInstant.OverlayViews.SetStyleAll();
        }));
        buttons.Add(CreateButton("MoveRandomAll", Color.green, () =>
        {
            FBInstant.OverlayViews.MoveRandomAll();
        }));
        buttons.Add(CreateButton("DestroyAllOverlays", Color.green, async () =>
        {
            await FBInstant.OverlayViews.DestroyAllAsync();
        }));

    }

    #endregion

    #region  Add Button Sections
    public void AddOverlayViewInstanceFunction(OverlayView overlayView)
    {
        buttons.Add(CreateButton(overlayView.id, Color.white, null));
        buttons.Add(CreateButton("ShowAsync", Color.green, async () =>
       {
           await overlayView.ShowAsync();
       }));
        buttons.Add(CreateButton("UpdateAsync", Color.green, async () =>
       {
           string data = Json.Object()
                .Prop("player_name", "TEST_PLAYER")
                .Prop("id", UnityEngine.Random.Range(0, 99999))
                .Prop("isVIP", true)
                .Obj("player_data", data => data
                    .Prop("head", "empty_head")
                    .Prop("body", "empty_body")
                    .Prop("legs", "empty_legs")
                ).ToString();
           await overlayView.UpdateAsync(data);
       }));
        buttons.Add(CreateButton("DismissAsync", Color.green, async () =>
       {
           await overlayView.DismissAsync();
       }));
        buttons.Add(CreateButton("DestroyOverlayView", Color.green, async () =>
       {
           await overlayView.DestroyOverlayView();
       }));
        buttons.Add(CreateButton("SetStyle", Color.green, async () =>
       {
           string randomHexColor = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV(0f, 1f, 0f, 1f, 1f, 1f));
           await overlayView.SetStyle("background-color", randomHexColor);
           await overlayView.SetStyle("opacity", "1");
           await overlayView.SetStyle("height", UnityEngine.Random.Range(100, 300) + "px");
           await overlayView.SetStyle("width", UnityEngine.Random.Range(100, 300) + "px");
       }));
        buttons.Add(CreateButton("MoveRandom", Color.green, () =>
        {
            overlayView.MoveRandom();
        }));
        buttons.Add(CreateButton("GetStatus", Color.green, async () =>
             {
                 string result = await overlayView.GetStatus();
                 SDKDebug.Log(result);
             }));
        buttons.Add(CreateButton("GetInitialData", Color.green, async () =>
             {
                 string result = await overlayView.GetInitialData();
                 SDKDebug.Log(result);
             }));
        buttons.Add(CreateButton("GetErrors", Color.green, async () =>
             {
                 string result = await overlayView.GetErrors();
                 SDKDebug.Log(result);
             }));
    }


    public void AddAdObject(AdInstance adInstance)
    {
        buttons.Add(CreateButton(adInstance.data.adInstanceID, Color.white, null));
        buttons.Add(CreateButton("LoadAsync", Color.green, async () =>
       {
           await adInstance.LoadAsync();
       }));

        buttons.Add(CreateButton("ShowAsync", Color.green, async () =>
       {
           await adInstance.ShowAsync();
       }));
    }
    public void AddIAPObj(Product product)
    {
        buttons.Add(CreateButton(product.title + " " + product.productID, Color.white, null));
        buttons.Add(CreateButton("Purchase Item", Color.green, async () =>
       {
           await FBInstant.Payment.PurchaseAsync(new PurchaseConfig()
           {
               productID = product.productID,
               developerPayload = "item-" + product.title
           });
       }));
    }
    #endregion
    #region Button Helper Function
    public GameObject CreateButton(string label, Color color, System.Action callback)
    {
        // Create a new Button GameObject
        GameObject buttonGO = new GameObject(label + "_button", typeof(RectTransform));
        buttonGO.transform.SetParent(parentTransform, false);

        // Add Image component for the button’s visuals
        Image image = buttonGO.AddComponent<Image>();
        if (callback == null) { image.color = Color.blue; }
        else { image.color = color; }

        // Add Button component and wire up onClick
        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = image;
        if (callback != null)
        {
            button.onClick.AddListener(() =>
            {
                SDKDebug.Log($"{label} clicked!");
                callback.Invoke();
            });
        }

        // Set up RectTransform position and size
        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200, 60);

        // Add a Text child for label
        GameObject textGO = new GameObject(label + "_label", typeof(RectTransform));
        textGO.transform.SetParent(buttonGO.transform, false);
        Text textComponent = textGO.AddComponent<Text>();
        textComponent.text = label;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.raycastTarget = false;
        textComponent.resizeTextForBestFit = true;
        textComponent.resizeTextMinSize = 28;
        textComponent.resizeTextMaxSize = 36;
        if (callback != null) { textComponent.color = Color.black; }
        else { textComponent.color = Color.white; }

        // Assign a default font to the label to ensure text renders
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont != null)
        {
            textComponent.font = defaultFont;
        }
        else
        {
            SDKDebug.LogWarning("Default font (Arial.ttf) not found. Please ensure a font is assigned.");
        }

        // Stretch label to fill button
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // Track the created button
        return buttonGO;
    }
    #endregion

    #region Editor Test

    [ContextMenu("Add Test Overlay")]
    public void TestOverlayAdder()
    {
        AddOverlayViewInstanceFunction(new OverlayView() { id = "123-thumbs" });
    }

    [ContextMenu("Add Payment Object")]
    public void TestAdObj()
    {
        AddAdObject(new AdInstance()
        {
            data = new AdInstanceData()
            {
                adInstanceID = "1234",
                placementID = "12345-ads"
            },
            type = AdType.INTERSTITIAL
        });
    }

    [ContextMenu("Add Payment Object")]
    public void TestIAPObj()
    {
        AddIAPObj(new Product()
        {
            productID = "12345",
            title = "what item"
        });
    }

    #endregion
}
