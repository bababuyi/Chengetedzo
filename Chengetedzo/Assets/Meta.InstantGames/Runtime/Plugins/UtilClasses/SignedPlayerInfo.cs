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
    public class SignedPlayerInfo
    {
        public SignedPlayerInfoData data;// [SignedPlayerInfoData][40] The player information and signature data.
        public string GetPlayerID() => data.playerID; //Get the id of the player.
        public string GetSignature() => data.signature; //A signature to verify this object indeed comes from Facebook. The string is base64url encoded and signed with an HMAC version of your App Secret, based on the OAuth 2.0 spec.

        public SignedPlayerInfo(string jsonString)
        {
            JsonValue json = JsonParser.Parse(jsonString);
            data = new SignedPlayerInfoData(json["$1"].ToString());
        }
        public override string ToString()
        {
            return $"PlayerID : {GetPlayerID()} , Signature {GetSignature()}";

        }
    }
}
