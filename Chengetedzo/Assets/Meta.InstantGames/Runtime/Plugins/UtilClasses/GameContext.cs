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
    public class GameContext
    {
        public ContextData contextData; // ContextData The data for this context
        public ContextSizeResponse contextSizeResponse; // ContextSizeResponse The response from a context size check
        public string getID() => contextData.id; //Returns the unique identifier for this context. Returns (string | null) The context ID, or null if not available
        public ContextType getType() => contextData.type; // Returns the type of this context. Returns ContextType The context type (e.g., SOLO, THREAD, GROUP)
        public int getSize() => contextData.size; // Returns the number of participants in this context. Returns (number | null) The context size, or null if not available
        public ContextSizeResponse GetContextSizeResponse() => contextSizeResponse; // Returns the response from the last context size check. Returns (ContextSizeResponse | null) The context size response, or null if no check has been performed
        public void SetContextSizeResponse(ContextSizeResponse _csr) => contextSizeResponse = _csr; // Sets the response from the last context size check. Returns void
    }
}
