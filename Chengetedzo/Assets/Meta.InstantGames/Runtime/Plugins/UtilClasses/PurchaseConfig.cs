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
    public class PurchaseConfig
    {
        public string productID; // string The identifier of the product to purchase
        public string developerPayload; // string? An optional developer-specified payload, to be included in the returned purchase's signed request.

        public Json ToJson()
        {
            Json payload = Json.Object()
           .ArrayOfObjects("args", args => args
               .Add(item => item
                   .Prop("type", "obj")
                   .Obj("value", obj => obj
                   .Prop("productID", productID)
                   .Prop("developerPayload", developerPayload)
                   )
               )
            );

            return payload;

        }
    }
}
