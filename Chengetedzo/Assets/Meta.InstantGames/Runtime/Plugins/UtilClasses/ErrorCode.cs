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
    public class ErrorCode
    {
        public string ADS_FREQUENT_LOAD = "ADS_FREQUENT_LOAD";// string Ads are being loaded too frequently.
        public string ADS_NO_FILL = "ADS_NO_FILL";// string We were not able to serve ads to the current user. This can happen if the user has opted out of interest-based ads on their device, or if we do not have ad inventory to show for that user.
        public string ADS_NOT_LOADED = "ADS_NOT_LOADED";// string Attempted to show an ad that has not been loaded successfully.
        public string ADS_TOO_MANY_INSTANCES = "ADS_TOO_MANY_INSTANCES";// string There are too many concurrent ad instances. Load and show existing ad instances before creating new ones.
        public string ANALYTICS_POST_EXCEPTION = "ANALYTICS_POST_EXCEPTION";// string The analytics API experienced a problem while attempting to post an event.
        public string CLIENT_REQUIRES_UPDATE = "CLIENT_REQUIRES_UPDATE";// string [Deprecated] - The client requires an update to access the feature that returned this result. If this result is returned on web, it means the feature is not supported by the web client yet. Deprecated in favor of CLIENT_UNSUPPORTED_OPERATION in v5.0 and above
        public string CLIENT_UNSUPPORTED_OPERATION = "CLIENT_UNSUPPORTED_OPERATION";// string The client does not support the current operation. This may be due to lack of support on the client version or platform, or because the operation is not allowed for the game or player.
        public string OPERATION_SUPPRESSED = "OPERATION_SUPPRESSED";// string The operation was suppressed by the platform. This may be due to user-level rate limiting, play style restrictions, or other reasons.
        public string GLOBAL_LEADERBOARD_NOT_FOUND = "GLOBAL_LEADERBOARD_NOT_FOUND";// string No global leaderboard with the requested ID was found. Either the leaderboard does not exist yet, or the ID did not match any registered leaderboard, you can verify the leaderboard ID in the Global Leaderboard section of the Instant Games Dashboard.
        public string IARC_CERT_NOT_FOUND = "IARC_CERT_NOT_FOUND";// string The requested IARC (International Age Rating Coalition) certificate was not found.
        public string IARC_CERT_TEST_ONLY = "IARC_CERT_TEST_ONLY";// string This is a test IARC (International Age Rating Coalition) operation.
        public string IARC_SUBMIT_CERT_FAILED = "IARC_SUBMIT_CERT_FAILED";// string Thw IARC (International Age Rating Coalition) certificate failed to be submitted.
        public string IARC_SUBMIT_EMAIL_FAILED = "IARC_SUBMIT_EMAIL_FAILED";// string The developer's IARC (International Age Rating Coalition) contact email failed to be submitted.
        public string INVALID_OPERATION = "INVALID_OPERATION";// string The requested operation is invalid or the current game state. This may include requests that violate limitations, such as exceeding storage thresholds, or are not available in a certain state, such as making a context-specific request in a solo context.
        public string INVALID_PARAM = "INVALID_PARAM";// string The parameter(s) passed to the API are invalid. Could indicate an incorrect type, invalid number of arguments, or a semantic issue (for example, passing an unserializable object to a serializing function).
        public string LEADERBOARD_NOT_FOUND = "LEADERBOARD_NOT_FOUND";// string No leaderboard with the requested name was found. Either the leaderboard does not exist yet, or the name did not match any registered leaderboard configuration for the game.
        public string LEADERBOARD_WRONG_CONTEXT = "LEADERBOARD_WRONG_CONTEXT";// string Attempted to write to a leaderboard that's associated with a context other than the one the game is currently being played in.
        public string MOCK_IAP = "MOCK_IAP";// string User is temporarily turning off Mock IAP and switching to production flow for current purchase
        public string NETWORK_FAILURE = "NETWORK_FAILURE";// string The client experienced an issue with a network request. This is likely due to a transient issue, such as the player's internet connection dropping.
        public string PAYMENTS_NOT_INITIALIZED = "PAYMENTS_NOT_INITIALIZED";// string The client has not completed setting up payments or is not accepting payments API calls.
        public string PENDING_REQUEST = "PENDING_REQUEST";// string Represents a rejection due an existing request that conflicts with this one. For example, we will reject any calls that would surface a Facebook UI when another request that depends on a Facebook UI is pending.
        public string RATE_LIMITED = "RATE_LIMITED";// string Some APIs or operations are being called too often. This is likely due to the game calling a particular API an excessive amount of times in a very short period. Reducing the rate of requests should cause this error to go away.
        public string SAME_CONTEXT = "SAME_CONTEXT";// string The game attempted to perform a context switch into the current context.
        public string TOURNAMENT_NOT_SHAREABLE = "TOURNAMENT_NOT_SHAREABLE";// string The game attempted to share a private tournament. This is only possible for non-private tournaments. If a score was submitted with the share call, then the score was still submitted.
        public string UNKNOWN = "UNKNOWN";// string An unknown or unspecified issue occurred. This is the default error code returned when the client does not specify a code.
        public string USER_INPUT = "USER_INPUT";// string The user made a choice that resulted in a rejection. For example, if the game calls up the Context Switch dialog and the player closes it, this error code will be included in the promise rejection.
    }
}
