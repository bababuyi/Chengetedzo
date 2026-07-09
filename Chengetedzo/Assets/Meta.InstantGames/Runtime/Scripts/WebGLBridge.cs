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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using AOT;

/// <summary>
/// Provides a bridge between Unity C# code and JavaScript for WebGL builds.
/// Handles asynchronous callbacks and function invocations to the FBInstant API.
/// </summary>
public class WebGLBridge : MonoBehaviour
{
    // Delegate signature: void(int statusCode, IntPtr resultPtr)
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void CallbackDelegate(int callbackId, int statusCode, IntPtr resultPtr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void PersistentCallbackDelegate(IntPtr callbackId, int statusCode, IntPtr resultPtr);

    [DllImport("__Internal")] private static extern void FBInstant_init_callbacks(int callback, IntPtr callbackPtr, int successCode);
    [DllImport("__Internal")] private static extern void FBInstant_function(int callbackId, IntPtr callbackPtr, string functionName, string args, int successCode, int errorCode);
    [DllImport("__Internal")] private static extern void FBInstant_overlayView_function(string functionName, string overlayViewId, string dataPtr, int callbackId, IntPtr callbackPtr, int successCode, int errorCode);
    [DllImport("__Internal")] private static extern void FBInstant_ad_function(string functionName, string overlayViewId, string dataPtr, int callbackId, IntPtr callbackPtr, int successCode, int errorCode);
    [DllImport("__Internal")] private static extern void FBInstant_getScreenshot(int callbackId, IntPtr callbackPtr, int successCode, int errorCode);
    [DllImport("__Internal")] private static extern void FBInstant_getCanvasRect(int callbackId, IntPtr callbackPtr, int successCode, int errorCode);
    [DllImport("__Internal")] private static extern void FBInstant_testCallback();

    // Status codes for JavaScript interop
    private const int STATUS_CODE_SUCCESS = 200;
    private const int STATUS_CODE_ERROR = 400;

    // Registry to track multiple concurrent calls
    private static int _nextCallbackId = 1;
    private static Dictionary<int, TaskCompletionSource<string>> _pendingCallbacks = new Dictionary<int, TaskCompletionSource<string>>();
    private static Dictionary<int, DateTime> _callbackTimestamps = new Dictionary<int, DateTime>();
    private static CallbackDelegate _sharedCallback;
    private static PersistentCallbackDelegate _sharedPersistentCallback;

    // Timeout configuration
    private const int CALLBACK_TIMEOUT_SECONDS = 60;
    private const int CLEANUP_INTERVAL_SECONDS = 30;
    private static DateTime _lastCleanupTime = DateTime.UtcNow;

    /// <summary>
    /// Gets the number of pending callbacks waiting for responses.
    /// </summary>
    public static int PendingCallbackCount => _pendingCallbacks.Count;

    // Persistent callbacks
    private static Dictionary<string, Action<string>> _persistentCallbackDictionary = new Dictionary<string, Action<string>>();

    static WebGLBridge()
    {
        // Initialize the shared callback once
        _sharedCallback = OnJavaScriptCallback;
        _sharedPersistentCallback = OnPersistentJavaScriptCallback;
    }

    /// <summary>
    /// Logs information about all pending callbacks for debugging.
    /// </summary>
    public static void LogCache()
    {
        SDKDebug.Log("Cache total: " + _pendingCallbacks.Count);
        foreach (var kvp in _pendingCallbacks)
        {
            var age = _callbackTimestamps.ContainsKey(kvp.Key)
                ? (DateTime.UtcNow - _callbackTimestamps[kvp.Key]).TotalSeconds
                : -1;
            SDKDebug.Log($"C: {kvp.Key} Age: {age:F1}s");
        }
    }

    private static void CleanupStaleCallbacks()
    {
        if ((DateTime.UtcNow - _lastCleanupTime).TotalSeconds < CLEANUP_INTERVAL_SECONDS)
            return;

        _lastCleanupTime = DateTime.UtcNow;
        var staleIds = new List<int>();

        foreach (var kvp in _callbackTimestamps)
        {
            if ((DateTime.UtcNow - kvp.Value).TotalSeconds > CALLBACK_TIMEOUT_SECONDS)
            {
                staleIds.Add(kvp.Key);
            }
        }

        foreach (var id in staleIds)
        {
            if (_pendingCallbacks.TryGetValue(id, out var tcs))
            {
                SDKDebug.LogWarning($"[WebGLBridge] Callback {id} timed out after {CALLBACK_TIMEOUT_SECONDS}s");
                tcs.TrySetException(new TimeoutException($"Callback {id} timed out"));
                _pendingCallbacks.Remove(id);
            }
            _callbackTimestamps.Remove(id);
        }

        if (staleIds.Count > 0)
        {
            SDKDebug.Log($"[WebGLBridge] Cleaned up {staleIds.Count} stale callbacks");
        }
    }

    /// <summary>
    /// Clears all pending callbacks.
    /// </summary>
    public static void ClearAllPendingCallbacks()
    {
        foreach (var kvp in _pendingCallbacks)
        {
            kvp.Value.TrySetCanceled();
        }
        _pendingCallbacks.Clear();
        _callbackTimestamps.Clear();
        SDKDebug.Log("[WebGLBridge] All pending callbacks cleared");
    }


    [MonoPInvokeCallback(typeof(CallbackDelegate))]
    private static void OnJavaScriptCallback(int callbackId, int statusCode, IntPtr resultPtr)
    {
        SDKDebug.Log($"[WebGLBridge] OnJavaScriptCallback: id={callbackId} status={statusCode} pending=[{string.Join(",", _pendingCallbacks.Keys)}]");

        if (!_pendingCallbacks.TryGetValue(callbackId, out var tcs))
        {
            SDKDebug.LogWarning($"Received callback for unknown ID: {callbackId}. Pending IDs: [{string.Join(",", _pendingCallbacks.Keys)}]");
            return;
        }

        _pendingCallbacks.Remove(callbackId);
        _callbackTimestamps.Remove(callbackId);

        string result = Marshal.PtrToStringUTF8(resultPtr);
        SDKDebug.Log($"[WebGLBridge] id={callbackId} result length={result?.Length ?? -1} status={(statusCode == STATUS_CODE_SUCCESS ? "SUCCESS" : "ERROR")}");

        if (statusCode == STATUS_CODE_SUCCESS) tcs.SetResult(result);
        else tcs.SetException(new Exception(result));
    }

    public static void RegisterPersistentCallback(string callbackId, Action<string> action)
    {
        if (!_persistentCallbackDictionary.TryAdd(callbackId, action))
        {
            _persistentCallbackDictionary[callbackId] = action;
            SDKDebug.Log($"callback action id {callbackId} overridden");
        }
        else
        {
            SDKDebug.Log($"callback action id {callbackId} added");
        }
    }


    [MonoPInvokeCallback(typeof(PersistentCallbackDelegate))]
    private static void OnPersistentJavaScriptCallback(IntPtr callbackIdPtr, int statusCode, IntPtr resultPtr)
    {
        string callbackId = Marshal.PtrToStringUTF8(callbackIdPtr);
        string buffer = Marshal.PtrToStringUTF8(resultPtr);

        if (!_persistentCallbackDictionary.TryGetValue(callbackId, out var tcs))
        {
            SDKDebug.LogWarning($"Received persistent callback for unknown ID: {callbackId}");
            return;
        }

        tcs.Invoke(buffer);
    }

    /// <summary>
    /// Calls a JavaScript function asynchronously and returns the result.
    /// </summary>
    /// <param name="functionName">The name of the JavaScript function to call.</param>
    /// <param name="args">Optional JSON-formatted arguments to pass to the function.</param>
    /// <returns>A task that resolves to the result string from JavaScript.</returns>
    public static async Task<string> CallAsyncJavaScript(string functionName, string args = "")
    {
        CleanupStaleCallbacks();

        int callbackId = _nextCallbackId++;
        var tcs = new TaskCompletionSource<string>();

        _pendingCallbacks[callbackId] = tcs;
        _callbackTimestamps[callbackId] = DateTime.UtcNow;
        SDKDebug.Log($"[WebGLBridge] CallAsyncJavaScript: id={callbackId} fn={functionName} registered. Pending=[{string.Join(",", _pendingCallbacks.Keys)}]");

#if UNITY_WEBGL && !UNITY_EDITOR
        IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(_sharedCallback);
        FBInstant_function(callbackId, callbackPtr, functionName, args, STATUS_CODE_SUCCESS, STATUS_CODE_ERROR);
#else
        // Editor simulation
        await Task.Delay(100);
        tcs.SetResult("Editor mock result");
#endif

        SDKDebug.Log($"[WebGLBridge] CallAsyncJavaScript: id={callbackId} about to await tcs.Task, IsCompleted={tcs.Task.IsCompleted}");
        var result = await tcs.Task;
        SDKDebug.Log($"[WebGLBridge] CallAsyncJavaScript: id={callbackId} await RESUMED, result length={result?.Length ?? -1}");
        return result;
    }


    #region Unity DOM Helpers
    /// <summary>
    /// Calls a custom JavaScript function. Currently routes to screenshot functionality.
    /// </summary>
    /// <param name="fnName">The function name (currently unused).</param>
    /// <returns>A task that resolves to the result string.</returns>
    public static async Task<string> CustomFunction(string fnName)
    {
#if UNITY_EDITOR
        await Task.Delay(100);
        return "Editor mock result";
#endif
        int callbackId = _nextCallbackId++;
        var tcs = new TaskCompletionSource<string>();
        _pendingCallbacks[callbackId] = tcs;

        IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(_sharedCallback);
        FBInstant_getScreenshot(callbackId, callbackPtr, STATUS_CODE_SUCCESS, STATUS_CODE_ERROR);

        return await tcs.Task;
    }

    /// <summary>
    /// Captures a screenshot of the Unity canvas asynchronously.
    /// </summary>
    /// <returns>A task that resolves to the base64 encoded screenshot.</returns>
    public static async Task<string> GetScreenshotAsync()
    {
#if UNITY_EDITOR
        await Task.Delay(100);
        return "Editor mock result";
#endif
        int callbackId = _nextCallbackId++;
        var tcs = new TaskCompletionSource<string>();
        _pendingCallbacks[callbackId] = tcs;

        IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(_sharedCallback);
        FBInstant_getScreenshot(callbackId, callbackPtr, STATUS_CODE_SUCCESS, STATUS_CODE_ERROR);

        return await tcs.Task;
    }

    /// <summary>
    /// Gets the current canvas rectangle dimensions from the DOM.
    /// </summary>
    /// <returns>A task that resolves to JSON containing canvas dimensions.</returns>
    public static async Task<string> GetCanvasRect()
    {
#if UNITY_EDITOR
        await Task.Delay(100);
        return "Editor mock result";
#endif
        int callbackId = _nextCallbackId++;
        var tcs = new TaskCompletionSource<string>();
        _pendingCallbacks[callbackId] = tcs;

        IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(_sharedCallback);
        FBInstant_getCanvasRect(callbackId, callbackPtr, STATUS_CODE_SUCCESS, STATUS_CODE_ERROR);

        return await tcs.Task;
    }

    #endregion

    #region Ad functions
    /// <summary>
    /// Calls an ad-related function on a specific ad instance.
    /// </summary>
    /// <param name="functionName">The name of the ad function to call.</param>
    /// <param name="adInstanceId">The ID of the ad instance.</param>
    /// <param name="args">Optional arguments to pass to the function.</param>
    /// <returns>A task that resolves to the result string.</returns>
    public static async Task<string> AdFunction(string functionName, string adInstanceId, string args = "")
    {
#if UNITY_EDITOR
        await Task.Delay(100);
        return "Editor mock result";
#endif
        int callbackId = _nextCallbackId++;
        var tcs = new TaskCompletionSource<string>();
        _pendingCallbacks[callbackId] = tcs;
        SDKDebug.Log($"[WebGLBridge] AdFunction: id={callbackId} fn={functionName} adId={adInstanceId} registered. Pending=[{string.Join(",", _pendingCallbacks.Keys)}]");

        IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(_sharedCallback);
        FBInstant_ad_function(functionName, adInstanceId, args, callbackId, callbackPtr, STATUS_CODE_SUCCESS, STATUS_CODE_ERROR);
        return await tcs.Task;
    }
    #endregion

    #region OverlayView functions
    /// <summary>
    /// Calls a function on a specific overlay view.
    /// </summary>
    /// <param name="functionName">The name of the overlay function to call.</param>
    /// <param name="overlayViewId">The ID of the overlay view.</param>
    /// <param name="args">Optional arguments to pass to the function.</param>
    /// <returns>A task that resolves to the result string.</returns>
    public static async Task<string> OverlayFunction(string functionName, string overlayViewId, string args = "")
    {
#if UNITY_EDITOR
        await Task.Delay(100);
        return "Editor mock result";
#endif
        int callbackId = _nextCallbackId++;
        var tcs = new TaskCompletionSource<string>();
        _pendingCallbacks[callbackId] = tcs;
        IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(_sharedCallback);

        FBInstant_overlayView_function(functionName, overlayViewId, args, callbackId, callbackPtr, STATUS_CODE_SUCCESS, STATUS_CODE_ERROR);
        return await tcs.Task;
    }

    #endregion



    #region TEST Functions
    /// <summary>
    /// Initializes persistent callbacks for FBInstant events.
    /// Must be called once at application startup.
    /// </summary>
    public static void InitCallbacks()
    {
        RegisterPersistentCallback("initCallbacks", (string message) =>
                   {
                       SDKDebug.Log($"Persistent Callback Registration Complete: {message ?? "NIL"}");
                   });

        IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(_sharedPersistentCallback);
#if UNITY_WEBGL && !UNITY_EDITOR
        FBInstant_init_callbacks(0, callbackPtr, STATUS_CODE_SUCCESS);
#else
        SDKDebug.Log("Editor mock initCallbacks");
#endif
    }
    public static void TestCallback()
    {
        FBInstant_testCallback();
    }

    #endregion

}
