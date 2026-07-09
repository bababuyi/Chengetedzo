/* // Copyright (c) Facebook, Inc. and its affiliates. All rights reserved.
//
// The examples provided by Facebook are for non-commercial testing and evaluation
// purposes only. Facebook reserves all rights not expressly granted.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// FACEBOOK BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. */

const IGPlugin = {
    objectCache: new Map(),

    deferCallback: function(callbackPtr, callbackId, statusCode, messageStr) {
        setTimeout(function() {
            var buffer = IGPlugin.StringBuffer(messageStr);
            callbackPtr(callbackId, statusCode, buffer);
            _free(buffer);
        }, 0);
    },

    addToCache: function (key, value) {
        // If key already exists, overwrite it (Map#set does overwrite, but log explicitly)
        if (this.objectCache.has(key)) {
            console.log("overwriting existing obj", key, this.objectCache.get(key), "->", value);
        }
        else {
            console.log("setting obj", key, value);
        }
        this.objectCache.set(key, value);
    },
    getFromCache: function (key) {
        console.log("loooking for obj", key);
        if (!this.objectCache.has(key)) {
            return null;
        }
        let value = this.objectCache.get(key);
        console.log("returning cached obj", value);
        return value;
    },

    StringBuffer: function (str) {
        const bufferSize = lengthBytesUTF8(str) + 1;
        const buffer = _malloc(bufferSize);
        stringToUTF8(str, buffer, bufferSize);
        return buffer;
    },



};
