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
using UnityEngine.UI;
public class FPSCounter : MonoBehaviour
{
    private const int SampleCount = 64;
    private const float UpdateInterval = 0.25f;

    private readonly float[] _frameTimes = new float[SampleCount];
    private int _frameIndex;
    private int _frameCount;
    private float _timeSinceUpdate;


    public Text text;
    public float CurrentFPS { get; private set; }
    public float AverageFPS { get; private set; }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        _frameTimes[_frameIndex] = deltaTime;
        _frameIndex = (_frameIndex + 1) & (SampleCount - 1);

        if (_frameCount < SampleCount)
            _frameCount++;

        _timeSinceUpdate += deltaTime;

        if (_timeSinceUpdate >= UpdateInterval)
        {
            _timeSinceUpdate = 0f;

            CurrentFPS = RoundToOneDecimal(1f / deltaTime);
            AverageFPS = RoundToOneDecimal(CalculateAverageFPS());

            text.text = string.Format("FPS: {0}\nAvg: {1}", CurrentFPS, AverageFPS);
        }
    }

    private float CalculateAverageFPS()
    {
        if (_frameCount == 0) return 0f;

        float totalTime = 0f;
        for (int i = 0; i < _frameCount; i++)
            totalTime += _frameTimes[i];

        if (totalTime <= 0f) return 0f;

        return _frameCount / totalTime;
    }

    private static float RoundToOneDecimal(float value)
    {
        return Mathf.Round(value * 10f) * 0.1f;
    }

    public void Reset()
    {
        _frameIndex = 0;
        _frameCount = 0;
        _timeSinceUpdate = 0f;
        CurrentFPS = 0f;
        AverageFPS = 0f;
    }
}
