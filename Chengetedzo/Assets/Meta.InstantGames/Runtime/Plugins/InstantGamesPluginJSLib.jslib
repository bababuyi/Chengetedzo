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

mergeInto(LibraryManager.library, {
    // typeof never throws, even on an undefined global — safe to call on any WebGL host
    // (Facebook injects window.FBInstant via its template's script tag; plain WebGL hosts
    // like itch.io do not). Callers must check this before touching FBInstant directly,
    // since every other function below assumes it already exists and will throw otherwise.
    FBInstant_isAvailable: function() {
        return (typeof FBInstant !== 'undefined' && FBInstant !== null) ? 1 : 0;
    },

    FBInstant_init_callbacks: async function(callbackId,callbackPtr,successCode){

        FBInstant.onPause(()=>{
            var message = JSON.stringify({message:"FBInstant.onPause default callback"});
            var cmd = IGPlugin.StringBuffer("onPause");
            var buffer = IGPlugin.StringBuffer(message);
            {{{ makeDynCall('viii', 'callbackPtr') }}} (cmd, successCode, buffer);
            try { _free(buffer); } catch(e) {}
        });

        FBInstant.onContextChange(
            ()=>{
            var message = JSON.stringify({message:"FBInstant.onContextChange default success callback"});
            var cmd = IGPlugin.StringBuffer("onContextChangeSuccess");
            var buffer = IGPlugin.StringBuffer(message);
            {{{ makeDynCall('viii', 'callbackPtr') }}} (cmd, successCode, buffer);
            _free(buffer);
        },
            ()=>{
            var message = JSON.stringify({message:"FBInstant.onContextChange default error callback"});
            var cmd = IGPlugin.StringBuffer("onContextChangeError");
            var buffer = IGPlugin.StringBuffer(message);
            {{{ makeDynCall('viii', 'callbackPtr') }}} (cmd, successCode, buffer);
            try { _free(buffer); } catch(e) {}
        });

        FBInstant.payments.onReady(()=>{
            var message = JSON.stringify({message:"FBInstant.payments.onReady default callback"});
            var cmd = IGPlugin.StringBuffer("payments.onReady");
            var buffer = IGPlugin.StringBuffer(message);

            {{{ makeDynCall('viii', 'callbackPtr') }}} (cmd, successCode, buffer);
            try { _free(buffer); } catch(e) {}
        });

        FBInstant.overlayViews.setCustomEventHandler((eventStr, overlayViewId)=>{
            var message = JSON.stringify({
                message:"FBInstant.overlayViews.setCustomEventHandler default callback",
                eventStr:eventStr,
                overlayViewId:overlayViewId
            });
            var cmd = IGPlugin.StringBuffer("overlayViews.setCustomEventHandler");
            var buffer = IGPlugin.StringBuffer(message);

            {{{ makeDynCall('viii', 'callbackPtr') }}} (cmd, successCode, buffer);
            try { _free(buffer); } catch(e) {}
        });

        window.testCallback = function(msg) {
            var message = JSON.stringify({message:"TEST CALLBACK: " + msg});
            var cmd = IGPlugin.StringBuffer("testCallback");
            var buffer = IGPlugin.StringBuffer(message);

            {{{ makeDynCall('viii', 'callbackPtr') }}} (cmd, successCode, buffer);
            try { _free(buffer); } catch(e) {}
        };

        var message = JSON.stringify({message:"WebGLBridge Initialised!"});
        var cmd = IGPlugin.StringBuffer("initCallbacks");
        var buffer = IGPlugin.StringBuffer(message);

        {{{ makeDynCall('viii', 'callbackPtr') }}} (cmd, successCode, buffer);
        try { _free(buffer); } catch(e) {}
    },

    FBInstant_function: async function (callbackId, callbackPtr, functionNamePtr, argsPtr, successCode, errorCode) {
        let functionName = UTF8ToString(functionNamePtr);

        //convert json to array of args
        let finalArgs = [];
        try {
            const raw = UTF8ToString(argsPtr);
            if (raw && raw.length > 0) {
                const parsed = JSON.parse(raw);
                // Expecting { args: [ {type: "...", value: ...}, ... ] }
                const items = (parsed && Array.isArray(parsed.args)) ? parsed.args : [];
                finalArgs = items.map(item => {
                    // Normalize and coerce by type if provided
                    switch (((item && item.type) || '').toLowerCase()) {
                        case 'int':
                        case 'integer':
                        case 'float':
                        case 'double':
                        case 'number':
                            return typeof item.value === 'number' ? item.value : parseFloat(item.value);
                        case 'string':
                            return String(item.value);
                        case 'bool':
                        case 'boolean':
                            return typeof item.value === 'boolean'
                                ? item.value
                                : item.value === 'true' || item.value === true || item.value === 1;
                        case 'object':
                        case 'obj':
                            // Ensure item.value is a valid object; if it's a JSON string, try to parse it
                            try {
                                item.value = JSON.parse(item.value);
                            } catch (e) {
                                // Keep original value on parse failure
                            }
                            return item.value;
                        case 'array':
                            return item.value; // Array.isArray(item.value) ? item.value : [];
                        case 'domelement':
                        case 'domelementid':
                            return document.getElementById(item.value);
                        case 'null':
                            return null;
                        case 'undefined':
                            return undefined;
                        case 'callback':
                            return item.value;
                        default:
                            // If no type or unknown type, pass through value as-is
                            return item ? item.value : undefined;
                    }
                });
            }
        } catch (e) {
            finalArgs = [];
        }


        // Resolve member and function
        const path = UTF8ToString(functionNamePtr);
        const segments = path.split('.');
        if (segments.length < 1) {
            throw new Error("functionName must include at least 1 member name");
        }
        //iterate through the functionName to find the target
        let target = FBInstant;
        for (let i = 0; i < segments.length - 1; i++) {
            target = target[segments[i]];
            if (target === undefined || target === null) {
                throw new Error("Member path not found: " + segments.slice(0, i + 1).join('.'));
            }
        }
        const fnName = segments[segments.length - 1];
        const fn = target[fnName];
        if (typeof fn !== 'function') {throw new Error("Target is not a function: " + path);}
        const fnCall = fn.apply(target, finalArgs);

        //special case functions :

        var dynCall = {{{ makeDynCall('viii', 'callbackPtr') }}};

        try {
            let reply = {};
            if (fnCall && typeof fnCall.then === 'function') { reply = await fnCall; }
            else { reply = fnCall; }

            let data = reply;

            if(reply == undefined){
                IGPlugin.deferCallback(dynCall, callbackId, successCode, "{}");
            }else{

                switch(functionName){
                    case "overlayViews.getOverlayViews":
                        data = (reply && typeof reply.keys === 'function')
                            ? Array.from(reply.keys())
                            : [];
                    break;

                    case "getInterstitialAdAsync":
                    case "getRewardedVideoAsync":
                    case "getRewardedInterstitialAsync":
                        IGPlugin.addToCache(reply["$1"].adInstanceID, reply);
                        data = {"$1": reply["$1"], "$2": reply["$2"], "$3": reply["$3"]};
                    break;
                }

                var message = JSON.stringify(data);
                IGPlugin.deferCallback(dynCall, callbackId, successCode, message);
            }

        } catch (error) {
            var errDetail = (error && error.message) ? error.message : (error && error.stack) ? error.stack : String(error);
            IGPlugin.deferCallback(dynCall, callbackId, errorCode, errDetail || "Unknown error");
        }
    },

    FBInstant_overlayView_function:async function (functionNamePtr, overlayViewIdPtr, argsPtr, callbackId, callbackPtr, successCode, errorCode) {
        let functionName = UTF8ToString(functionNamePtr);
        let overlayViewId = UTF8ToString(overlayViewIdPtr);
        const rawArgsStr = UTF8ToString(argsPtr);
        let finalArgs= rawArgsStr ? JSON.parse(rawArgsStr) : [];

        //v1, get overlayView from getOverlay list.
        let targetOV = FBInstant.overlayViews.getOverlayViews().get(overlayViewId);
        let data = {}
        switch(functionName){
            case "setStyle":
                let style = finalArgs["styleName"];
                value = finalArgs["value"];
                targetOV.iframeElement.style[style]=value;
                break;
            case "setAttribute":
                let attribute = finalArgs["attributeName"];
                value = finalArgs["value"];
                targetOV.iframeElement.setAttribute(attribute,value);
            break;
            case "destroyOverlayView":
                targetOV.iframeElement["display"]= "none";
                targetOV.iframeElement.parentNode.removeChild(targetOV.iframeElement);
            break;
        default:
                //fn call
                if(typeof targetOV[functionName] === 'function' && typeof targetOV[functionName].then === 'function'){
                    data = await targetOV[functionName](finalArgs);
                }else if(typeof targetOV[functionName] === 'function'){
                    data = targetOV[functionName](finalArgs);
                }
            break;
        }


        var message = JSON.stringify({message:"Overlay Function called!", data:data});
        var buffer = IGPlugin.StringBuffer(message);
         {{{ makeDynCall('viii', 'callbackPtr') }}} (callbackId, successCode, buffer);
        try { _free(buffer); } catch(e) {}
    },

    FBInstant_ad_function:async function (functionNamePtr, adIdPtr, argsPtr, callbackId, callbackPtr, successCode, errorCode) {
        let functionName = UTF8ToString(functionNamePtr);
        let adId = UTF8ToString(adIdPtr);
        const rawArgsStr = UTF8ToString(argsPtr);
        let finalArgs= rawArgsStr ? JSON.parse(rawArgsStr) : [];

        let callbackSent = false;
        let targetAd = IGPlugin.getFromCache(adId);

        var dynCall = {{{ makeDynCall('viii', 'callbackPtr') }}};

        try {
            let data = {}
            if(targetAd && typeof targetAd[functionName] === 'function'){
                let fnResult = targetAd[functionName](finalArgs);
                if (fnResult && typeof fnResult.then === 'function') {
                    data = await fnResult;
                } else {
                    data = fnResult;
                }
            } else {
            }

            var message = JSON.stringify({message:"Ad function called!", fn: functionName, data:data});
            IGPlugin.deferCallback(dynCall, callbackId, successCode, message);
        } catch (error) {
            var errDetail = (error && error.message) ? error.message : String(error);
            IGPlugin.deferCallback(dynCall, callbackId, errorCode, errDetail || "Unknown error");
        }
    },

    FBInstant_getCanvasRect: function(callbackId, callbackPtr, successCode, errorCode)
    {
        var canvas = document.getElementById('unity-container');
        var rect = canvas.getBoundingClientRect();
        var result = {
            left: rect.left,
            top: rect.top,
            width: rect.width,
            height: rect.height
        };
        var buffer = IGPlugin.StringBuffer(JSON.stringify(result));
        {{{ makeDynCall('viii', 'callbackPtr') }}} (callbackId, successCode, buffer);
        try { _free(buffer); } catch(e) {}
    },

    FBInstant_getScreenshot:function(callbackId, callbackPtr, successCode, errorCode)
    {
        let quality = 0.7;
        const gameContainer = document.getElementById("unity-container");
        const gameCanvas = gameContainer ? gameContainer.querySelector('canvas') : null;
        let base64Image = gameCanvas.toDataURL("image/jpeg", quality);
        var buffer = IGPlugin.StringBuffer(base64Image);
        {{{ makeDynCall('viii', 'callbackPtr') }}} (callbackId, successCode, buffer);
        try { _free(buffer); } catch(e) {}
    },

    FBInstant_testCallback: async function() //successCallbackPtr,errorCallbackPtr)
    {
        // pass in successCallbackPtr and errorCallbackPtr
        // dyncall when they are done
        // use this to test if callbacks actually work! ok thanks see you later.

        let ov = FBInstant.overlayViews.createOverlayView(
            "ig_views/example_overlay.xml",
            "TemplateData/style.css",
            {data: 30},
            (overlayView) =>{
                var message = {message:"ok done"};
                var buffer = IGPlugin.StringBuffer(JSON.stringify(message));
                // {{{ makeDynCall('viii', 'successCallbackPtr') }}} (callbackId, successCode, buffer);
            },
            (overlayView, error) =>{
                var message = {message:"ok done"};
                var buffer = IGPlugin.StringBuffer(JSON.stringify(message));
                // {{{ makeDynCall('viii', 'errorCallbackPtr') }}} (callbackId, successCode, buffer);
            }
        );

        let iframe = ov.getIFrameElement();
        let domElement = document.getElementById('unity-container');
        domElement.appendChild(iframe);
        iframe.zIndex = 9999;

        await new Promise(resolve => setTimeout(resolve, 5000));

        await ov.showAsync(); //the callback happens here.

        iframe.style["position"] = "absolute";
        iframe.style["border"] = "2px solid red";
        iframe.style["top"] = "50px";
        iframe.style["left"] = "50px";
        iframe.style["z-index"] = "999";

    }
})
