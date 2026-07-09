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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugLogRedirect : MonoBehaviour
{
    private const int MAX_LOGS = 100;
    private List<string> logs = new List<string>();
    private Queue<string> pendingLogs = new Queue<string>();
    private bool isDirty = false;
    private readonly object lockObj = new object();

    public Text text;
    public CanvasGroup cg;

    void Start()
    {
        if (text == null) InstantiateTextbox();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        Application.logMessageReceivedThreaded += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLog;
    }

    void LateUpdate()
    {
        if (!isDirty) return;

        lock (lockObj)
        {
            while (pendingLogs.Count > 0)
            {
                string log = pendingLogs.Dequeue();
                logs.Add(log);

                if (logs.Count > MAX_LOGS)
                {
                    logs.RemoveAt(0);
                }
            }
            isDirty = false;
        }

        if (text != null)
        {
            text.text = string.Join("\n", logs);
        }
    }

    public void ToggleCG()
    {
        if (cg.alpha == 0) cg.alpha = 1;
        else cg.alpha = 0;
    }

    private void InstantiateTextbox()
    {
        GameObject textGO = gameObject;
        text = textGO.AddComponent<Text>();

        RectTransform rt = textGO.GetComponent<RectTransform>();
        const float margin = 10f;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);

        text = textGO.GetComponent<Text>();
        text.text = "";
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        text.alignment = TextAnchor.LowerLeft;
        text.color = Color.green;
        text.fontSize = 24;
        text.raycastTarget = false;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string formattedLog = $"[{type}] {logString}";

        lock (lockObj)
        {
            pendingLogs.Enqueue(formattedLog);
            isDirty = true;
        }
    }

    public List<string> GetLogs()
    {
        return new List<string>(logs);
    }

    public void ClearLogs()
    {
        lock (lockObj)
        {
            logs.Clear();
            pendingLogs.Clear();
            isDirty = false;
        }
        if (text != null)
        {
            text.text = "";
        }
    }
}
