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
    public class Purchase
    {
        public string developerPayload; // string? A developer-specified string, provided during the purchase of the product
        public bool isConsumed; // boolean Whether or not the purchase has been consumed
        public string paymentActionType; // string The current status of the purchase, such as 'charge' or 'refund'
        public string paymentID; // string The identifier for the purchase transaction
        public string productID; // string The product's game-specified identifier
        public string purchasePlatform; // PurchasePlatform The platform associated with the purchase, such as "FB" for Facebook and "GOOGLE" for Google.
        public string purchasePrice; // Object Contains the local amount and currency associated with the purchased item
        public string purchaseTime; // string Unix timestamp of when the purchase occurred
        public string purchaseToken; // string A token representing the purchase that may be used to consume the purchase
        public string signedRequest; // SignedPurchaseRequest Server-signed encoding of the purchase request
    }
}
