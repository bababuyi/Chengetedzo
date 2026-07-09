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
    /// <summary>
    /// Represents a product available for purchase in the game.
    /// </summary>
    public class Product
    {
        /// <summary>The title of the product.</summary>
        public string title;

        /// <summary>The product's game-specified identifier.</summary>
        public string productID;

        /// <summary>The product description.</summary>
        public string description;

        /// <summary>A link to the product's associated image.</summary>
        public string imageURI;

        /// <summary>The price of the product as a formatted string.</summary>
        public string price;

        /// <summary>The currency code for the product (e.g., "USD").</summary>
        public string priceCurrencyCode;

        /// <summary>The numeric price of the product.</summary>
        public float priceAmount;

        /// <summary>
        /// Creates a new Product instance from a JSON string.
        /// </summary>
        /// <param name="jsonString">The JSON string containing product data.</param>
        public Product(string jsonString)
        {
            JsonValue json = JsonParser.Parse(jsonString);
            title = json["title"]?.ToStringTrimmed() ?? string.Empty;
            productID = json["productID"]?.ToStringTrimmed() ?? string.Empty;
            description = json["description"]?.ToStringTrimmed() ?? string.Empty;
            imageURI = json["imageURI"]?.ToStringTrimmed() ?? string.Empty;
            price = json["price"]?.ToStringTrimmed() ?? string.Empty;
            priceCurrencyCode = json["priceCurrencyCode"]?.ToStringTrimmed() ?? string.Empty;

            string priceAmountStr = json["priceAmount"]?.ToStringTrimmed();
            if (!string.IsNullOrEmpty(priceAmountStr) &&
                float.TryParse(priceAmountStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float parsedAmount))
            {
                priceAmount = parsedAmount;
            }
            else
            {
                priceAmount = 0f;
            }
        }

        /// <summary>
        /// Creates a new empty Product instance.
        /// </summary>
        public Product() { }

        /// <summary>
        /// Returns a string representation of the product.
        /// </summary>
        public override string ToString()
        {
            return $"Product(title='{title}', productID='{productID}', description='{description}', imageURI='{imageURI}', price='{price}', priceCurrencyCode='{priceCurrencyCode}', priceAmount={priceAmount})";
        }
    }
}
