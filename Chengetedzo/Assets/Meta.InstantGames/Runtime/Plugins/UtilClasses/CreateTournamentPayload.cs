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
    public class CreateTournamentPayload
    {
        public int initialScore; // number An integer value representing the player's score which will be the first score in the tournament.
        public CreateTournamentConfig config; // CreateTournamentConfig An object holding optional configurations for the tournament
        public string data; // GamePayload? A blob of data to attach to the update. All game sessions launched from the update will be able to access this blob through FBInstant.getEntryPointData(). Must be less than or equal to 1000 characters when stringified.
    }
}
