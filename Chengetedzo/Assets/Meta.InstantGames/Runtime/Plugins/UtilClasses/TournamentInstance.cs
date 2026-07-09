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
    public class TournamentInstance
    {
        public string tournamentID = ""; // string The unique identifier for the tournament.
        public string contextID = ""; // string The context ID associated with the tournament.
        public double endTime = 0; // number Timestamp when the tournament ends.
        public string tournamentType = ""; // InstantGameTimedLeaderboardTournamentExternalType The type of tournament.
        public string title = ""; // string? Optional title for the tournament.
        public string payload = "{}"; // string? Optional data payload for the tournament.

        public string getID() => tournamentID;
        public string getContextID() => contextID;
        public double getEndTime() => endTime;
        public string getTournamentType() => tournamentType;
        public string getTitle() => title;
        public string getPayload() => payload;

        public TournamentInstance(string jsonString)
        {
            JsonValue json = JsonParser.Parse(jsonString);
            tournamentID = json["$1"].ToStringTrimmed();
            contextID = json["$2"].ToStringTrimmed();
            endTime = double.Parse(json["$3"].ToStringTrimmed());
            tournamentType = json["$4"] != null ? json["$4"].ToStringTrimmed() : "DEFAULT";
            title = json["$5"] != null ? json["$5"].ToString() : "Title";
            payload = json["$6"] != null ? json["$6"].ToString() : "{}";

        }

        public override string ToString()
        {
            return $"tournamentID {tournamentID}\ncontextID {contextID}\nendTime {endTime}\ntournamentType {tournamentType}\ntitle {title}\npayload {payload}";
        }
    }
}
