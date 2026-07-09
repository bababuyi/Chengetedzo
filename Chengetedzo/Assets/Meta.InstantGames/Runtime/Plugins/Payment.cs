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
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Meta.InstantGames
{
    /// <summary>
    /// Provides access to payment and in-app purchase functionality in the FBInstant API.
    /// </summary>
    public class Payment
    {
        /// <summary>
        /// Gets the product catalog for the current game.
        /// </summary>
        /// <returns>A task that resolves to an array of available products.</returns>
        /// <exception cref="InstantGamesParseException">Thrown when the response JSON cannot be parsed.</exception>
        public async Task<Product[]> GetCatalogAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("payments.getCatalogAsync");

            try
            {
                JsonValue ids = JsonParser.Parse(result);
                if (ids != null)
                {
                    List<Product> newList = new List<Product>(ids.Count);
                    foreach (JsonValue id in ids.ArrayElements)
                    {
                        Product newProduct = new Product(id.ToString());
                        newList.Add(newProduct);
                    }
                    return newList.ToArray();
                }
                return Array.Empty<Product>();
            }
            catch (System.Exception ex)
            {
                throw new InstantGamesParseException("Failed to parse product catalog JSON: " + ex.Message, ex);
            }
        }
        /// <summary>
        /// Purchases a product.
        /// </summary>
        /// <param name="purchaseConfig">Configuration for the purchase.</param>
        /// <returns>A task that resolves to the purchase result.</returns>
        public async Task<string> PurchaseAsync(PurchaseConfig purchaseConfig)
        {
            string result = await WebGLBridge.CallAsyncJavaScript("payments.purchaseAsync", purchaseConfig.ToJson().ToString());
            return result;
        }

        /// <summary>
        /// Gets a list of unconsumed purchases.
        /// </summary>
        /// <returns>A task that resolves to the list of purchases.</returns>
        public async Task<string> GetPurchasesAsync()
        {
            string result = await WebGLBridge.CallAsyncJavaScript("payments.getPurchasesAsync");
            return result;
        }

        /// <summary>
        /// Consumes a specific purchase.
        /// </summary>
        /// <param name="productId">The ID of the product to consume.</param>
        /// <returns>A task that resolves when the purchase is consumed.</returns>
        public async Task<string> ConsumePurchaseAsync(string productId)
        {
            string payload = Json.Object()
    .ArrayOfObjects("args", args => args
        .Add(item => item
            .Prop("type", "string")
            .Prop("value", productId)
        )
     ).ToString();

            string result = await WebGLBridge.CallAsyncJavaScript("payments.consumePurchaseAsync", payload);
            return result;
        }

        /// <summary>
        /// Registers a callback to be invoked when the payment system is ready.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        public void OnReady(Action<string> callback)
        {
            WebGLBridge.RegisterPersistentCallback("payments.onReady", callback);
        }
    }
}
