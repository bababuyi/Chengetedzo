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


public class WebGLPersistentCallbacks
{
    // Delegate signature: void(int callbackId, int statusCode, IntPtr resultPtr)
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PersistentCallbackDelegate(int callbackId, int statusCode, IntPtr resultPtr);

    // Dictionary storing persistent callbacks by ID
    private static Dictionary<int, Action<int, string>> _persistentCallbacks = new Dictionary<int, Action<int, string>>();

    // Counter for generating unique callback IDs
    private static int _nextCallbackId = 1;

    // Shared delegate instance to prevent garbage collection
    private static PersistentCallbackDelegate _sharedCallback;

    static WebGLPersistentCallbacks()
    {
        _sharedCallback = OnPersistentCallback;
    }

    /// <summary>
    /// Registers a persistent callback and returns its unique ID and function pointer.
    /// The callback will remain active until explicitly unregistered.
    /// </summary>
    /// <param name="callback">Action that receives (statusCode, resultString)</param>
    /// <returns>Tuple of (callbackId, functionPointer) for use with jslib dyncall</returns>
    public static (int callbackId, IntPtr callbackPtr) RegisterCallback(Action<int, string> callback)
    {
        int callbackId = _nextCallbackId++;
        _persistentCallbacks[callbackId] = callback;

        IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(_sharedCallback);
        return (callbackId, callbackPtr);
    }

    /// <summary>
    /// Unregisters a persistent callback by its ID.
    /// Call this when the callback is no longer needed to free memory.
    /// </summary>
    /// <param name="callbackId">The callback ID returned from RegisterCallback</param>
    /// <returns>True if the callback was found and removed, false otherwise</returns>
    public static bool UnregisterCallback(int callbackId)
    {
        return _persistentCallbacks.Remove(callbackId);
    }

    /// <summary>
    /// Checks if a callback with the given ID is currently registered.
    /// </summary>
    /// <param name="callbackId">The callback ID to check</param>
    /// <returns>True if the callback exists, false otherwise</returns>
    public static bool HasCallback(int callbackId)
    {
        return _persistentCallbacks.ContainsKey(callbackId);
    }

    /// <summary>
    /// Gets the current count of registered persistent callbacks.
    /// Useful for debugging and monitoring memory usage.
    /// </summary>
    public static int CallbackCount => _persistentCallbacks.Count;

    /// <summary>
    /// Clears all registered persistent callbacks.
    /// Use with caution - this will invalidate all active callback references.
    /// </summary>
    public static void ClearAllCallbacks()
    {
        _persistentCallbacks.Clear();
        SDKDebug.Log("[WebGLPersistentCallbacks] All callbacks cleared");
    }

    /// <summary>
    /// Gets the shared function pointer for use with jslib dyncall.
    /// Use this when you need just the pointer without registering a new callback.
    /// </summary>
    public static IntPtr GetSharedCallbackPtr()
    {
        return Marshal.GetFunctionPointerForDelegate(_sharedCallback);
    }

    /// <summary>
    /// Logs all currently registered callbacks for debugging purposes.
    /// </summary>
    public static void LogRegisteredCallbacks()
    {
        SDKDebug.Log($"[WebGLPersistentCallbacks] Total registered: {_persistentCallbacks.Count}");
        foreach (var kvp in _persistentCallbacks)
        {
            SDKDebug.Log($"  Callback ID: {kvp.Key}");
        }
    }

    /// <summary>
    /// The shared callback handler invoked by JavaScript via dyncall.
    /// Routes the call to the appropriate registered callback.
    /// </summary>
    [MonoPInvokeCallback(typeof(PersistentCallbackDelegate))]
    private static void OnPersistentCallback(int callbackId, int statusCode, IntPtr resultPtr)
    {
        if (!_persistentCallbacks.TryGetValue(callbackId, out var callback))
        {
            SDKDebug.LogWarning($"[WebGLPersistentCallbacks] Received callback for unknown ID: {callbackId}");
            return;
        }

        string result = resultPtr != IntPtr.Zero
            ? Marshal.PtrToStringUTF8(resultPtr)
            : string.Empty;

        try
        {
            callback.Invoke(statusCode, result);
        }
        catch (Exception ex)
        {
            SDKDebug.LogError($"[WebGLPersistentCallbacks] Error in callback {callbackId}: {ex.Message}");
        }
    }
}
