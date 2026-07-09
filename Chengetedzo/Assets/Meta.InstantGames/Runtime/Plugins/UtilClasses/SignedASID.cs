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
    public class SignedASID
    {
        public SignedASIDData data;
        public string GetASID() => data.asid; //Get the app-scoped user id of the player.
        public string GetSignature() => data.signed_request; //A signature to verify this object indeed comes from Facebook. The string is base64url encoded and signed with an HMAC version of your App Secret, based on the OAuth 2.0 spec.

        public SignedASID(string jsonString)
        {
            JsonValue json = JsonParser.Parse(jsonString);
            JsonValue payload = json?["$1"];
            data = new SignedASIDData(payload == null || payload.IsNull ? "{}" : payload.ToString());
        }

        public override string ToString()
        {
            return $"ASID: {data.asid}, Signature: {data.signed_request}";
        }
    }
}
