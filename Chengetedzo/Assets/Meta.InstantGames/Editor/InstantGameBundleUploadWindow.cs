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
using UnityEditor;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class InstantGameBundleUploadWindow : EditorWindow
{
    private string field1 = "";
    private string field2 = "";
    private string field3 = "";
    private string field4 = "";
    private string field5 = "";
    private string responseMessage = "";
    private bool isLoading = false;
    private float labelWidth = 80;
    private bool showAppSecret = false;
    private bool showAppAccessToken = false;
    private Vector2 scrollPosition;
    private int sdkVersionIndex = 0;
    public static readonly string[] sdkVersionOptions = new string[] { "8.0", "restricted.latest", "7.1" };


    private const string PREF_FIELD1 = "BundleUpload_Field1";
    private const string PREF_FIELD2 = "BundleUpload_Field2";
    private const string PREF_FIELD3 = "BundleUpload_Field3";
    private const string PREF_FIELD4 = "BundleUpload_Field4";
    private const string PREF_FIELD5 = "BundleUpload_Field5";
    public const string PREF_SDK_VERSION = "BundleUpload_SDKVersion";

    public class CurlResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public int ExitCode { get; set; }
    }

    [Serializable]
    class TokenResponse { public string access_token; public string token_type; }


    [MenuItem("Window/Instant Games/Project Optimiser", priority = 1)]
    public static void SetupProject()
    {
        // Show confirmation dialog with all steps
        string confirmationMessage = "" +
            "This tool serves to set up Unity for faster developement in WebGL. In cases of existing projects, this may overwrite some of your values, please proceed at your own risk!\n\n" +
            "The Project Optimiser will apply the following settings:\n\n" +
            "1. Switch Build Target to WebGL\n" +
            "2. Set Compression Format to Disabled\n" +
            "3. Enable Development Build\n" +
            "4. Set WebGL Template to FB\n" +
            "5. Enable Run In Background\n" +
            "6. Set Active Input Handling to Both\n\n" +
            "Note: Step 6 may require a Unity restart to take effect.\n\n" +
            "Do you want to proceed?";

        if (!EditorUtility.DisplayDialog("Instant Games Project Optimiser", confirmationMessage, "Yes, Optimise", "Cancel"))
        {
            UnityEngine.Debug.Log("[Instant Games] Project optimisation cancelled by user.");
            return;
        }

        bool requiresRestart = false;

        // Step 1: Switch build target to WebGL if not already
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            UnityEngine.Debug.Log("[Instant Games] Switching build target to WebGL...");
            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            if (!switched)
            {
                EditorUtility.DisplayDialog("Setup Error", "Failed to switch build target to WebGL. Please ensure WebGL module is installed.", "OK");
                return;
            }
            UnityEngine.Debug.Log("[Instant Games] Build target switched to WebGL.");
        }

        // Step 2: Set Publishing > Compression Format to Disabled
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        UnityEngine.Debug.Log("[Instant Games] WebGL compression format set to Disabled.");

        // Step 3: Set Development Build to true
        EditorUserBuildSettings.development = true;
        UnityEngine.Debug.Log("[Instant Games] Development build enabled.");

        // Step 4: Set WebGL Template to FB (if available)
        string fbTemplateName = "PROJECT:FB";
        PlayerSettings.WebGL.template = fbTemplateName;
        UnityEngine.Debug.Log($"[Instant Games] WebGL template set to: {fbTemplateName}");

        // Step 5: Set Run In Background to true
        PlayerSettings.runInBackground = true;
        UnityEngine.Debug.Log("[Instant Games] Run in background enabled.");

        // Step 6: Set Active Input Handling to Both (requires restart)
        try
        {
            var serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var activeInputHandlerProp = serializedObject.FindProperty("activeInputHandler");
            if (activeInputHandlerProp != null && activeInputHandlerProp.intValue != 2)
            {
                activeInputHandlerProp.intValue = 2; // 0 = Input Manager, 1 = Input System, 2 = Both
                serializedObject.ApplyModifiedProperties();
                requiresRestart = true;
                UnityEngine.Debug.Log("[Instant Games] Active Input Handling set to Both.");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[Instant Games] Could not set Active Input Handling: {ex.Message}. Please set it manually to 'Both' in Player Settings > Other Settings.");
        }

        // Save all changes
        AssetDatabase.SaveAssets();

        string message = "Instant Games project setup complete!\n\n" +
            "Settings applied:\n" +
            "• Build Target: WebGL\n" +
            "• Compression Format: Disabled\n" +
            "• Development Build: Enabled\n" +
            "• WebGL Template: FB\n" +
            "• Run In Background: Enabled\n" +
            "• Active Input Handling: Both";

        if (requiresRestart)
        {
            message += "\n\n⚠️ Unity restart required for Input Handling changes to take effect.";
            if (EditorUtility.DisplayDialog("Setup Complete", message, "Restart Now", "Restart Later"))
            {
                EditorApplication.OpenProject(System.IO.Directory.GetCurrentDirectory());
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Setup Complete", message, "OK");
        }

        UnityEngine.Debug.Log("[Instant Games] Project setup complete.");
    }

    [MenuItem("Window/Instant Games/Open Documentation", priority = 4)]
    public static void OpenSDKDocs()
    {
        // Open SDK documentation in the default browser
        string docsUrl = "https://developers.facebook.com/docs/games/instant-games/sdk/fbinstant8.0";
        Application.OpenURL(docsUrl);
    }

    [MenuItem("Window/Instant Games/Bundle Uploader", priority = 0)]
    public static void ShowWindow()
    {
        GetWindow<InstantGameBundleUploadWindow>("Bundle Uploader");
    }

    void OnEnable()
    {
        LoadPreferences();
    }

    void OnDisable()
    {
        SavePreferences();
    }

    private void LoadPreferences()
    {
        field1 = EditorPrefs.GetString(PREF_FIELD1, "");
        field2 = EditorPrefs.GetString(PREF_FIELD2, "");
        field3 = EditorPrefs.GetString(PREF_FIELD3, "");
        field4 = EditorPrefs.GetString(PREF_FIELD4, "");
        field5 = EditorPrefs.GetString(PREF_FIELD5, "");
        sdkVersionIndex = EditorPrefs.GetInt(PREF_SDK_VERSION, 0);
    }

    private void SavePreferences()
    {
        EditorPrefs.SetString(PREF_FIELD1, field1);
        EditorPrefs.SetString(PREF_FIELD2, field2);
        EditorPrefs.SetString(PREF_FIELD3, field3);
        EditorPrefs.SetString(PREF_FIELD4, field4);
        EditorPrefs.SetString(PREF_FIELD5, field5);
        EditorPrefs.SetInt(PREF_SDK_VERSION, sdkVersionIndex);
    }

    private void OutputMessage(string message, bool clear = false)
    {
        if (clear)
        {
            responseMessage = "";
        }
        responseMessage += message + "\n";
    }

    void TestLocally()
    {
        throw new NotImplementedException("Test Locally");
    }

    void TestOnFacebook()
    {
        throw new NotImplementedException("Test On Facebook");
    }

    async void GetToken()
    {

        string app_id = this.field1.Trim();
        string app_secret = this.field2.Trim();

        var curlCommand =
            $"-X GET \"https://graph.facebook.com/oauth/access_token" +
            $"?client_id={app_id}" +
            $"&client_secret={app_secret}" +
            $"&grant_type=client_credentials\"";

        UnityEngine.Debug.Log(curlCommand);
        var result = await RunCUrl(curlCommand);
        if (result.Success)
        {
            UnityEngine.Debug.Log("Get Token successful: " + result.Output);

            try
            {
                // Facebook returns JSON like: {"access_token":"APP|TOKEN","token_type":"bearer"}
                // Wrap into a serializable container if needed and parse with JsonUtility
                // Since JsonUtility requires fields, define a local helper class

                // Trim output to reduce chances of stray whitespace/newlines causing issues
                var output = (result.Output ?? string.Empty).Trim();

                // Some curl outputs may include HTTP headers; try to find the JSON body
                int jsonStart = output.IndexOf('{');
                int jsonEnd = output.LastIndexOf('}');
                string json = (jsonStart >= 0 && jsonEnd >= jsonStart)
                    ? output.Substring(jsonStart, (jsonEnd - jsonStart) + 1)
                    : output;

                var tokenResponse = JsonUtility.FromJson<TokenResponse>(json);
                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.access_token))
                {
                    field5 = tokenResponse.access_token; // store in "App Access Token" field
                    SavePreferences();
                    OutputMessage("Access token parsed and saved.");
                }
                else
                {
                    OutputMessage("Failed to parse access_token from response JSON.");
                }
            }
            catch (Exception ex)
            {
                OutputMessage("Exception parsing token JSON: " + ex.Message);
            }
        }
        else
        {
            OutputMessage("Get Token failed: " + result.Error);
        }
    }

    async void UploadBundle()
    {

        OutputMessage("Uploading Bundle now...", true);
        string app_id = this.field1.Trim();
        // string app_secret = this.field2.Trim(); //not needed for upload using this method.
        string path = this.field4.Trim();
        string notes = this.field3.Trim();
        string app_access_token = this.field5.Trim();

        var safePath = path.Replace("\\", "/"); // normalize for curl
        var curlCommand =
            $"-X POST \"https://graph-video.facebook.com/{app_id}/assets\" " +
            $"-F \"access_token={app_access_token}\" " +
            $"-F \"type=BUNDLE\" " +
            $"-F \"asset=@{safePath}\" " +
            $"-F \"comment={notes}\"";
        UnityEngine.Debug.Log(curlCommand);
        // Indicate loading in the response box before starting the upload
        isLoading = true;
        OutputMessage("Uploading bundle...\nPlease wait.");
        Repaint();

        var result = await RunCUrl(curlCommand);

        // Indicate loading is complete
        isLoading = false;
        if (result.Success)
        {
            OutputMessage("Upload Bundle successful: " + result.Output);
        }
        else
        {
            OutputMessage("Upload Bundle failed!: " + result.Error);
        }
        // responseMessage = result.Output;
        Repaint();
    }

    async void UploadProdBundle()
    {

        OutputMessage("Uploading Prod Bundle now...", true);
        string APP_ID = this.field1.Trim();
        // string app_secret = this.field2.Trim(); //not needed for upload using this method.
        string RAW_PATH = this.field4.Trim();
        string notes = this.field3.Trim();
        string USER_ACCESS_TOKEN = this.field5.Trim();
        string APP_ACCESS_TOKEN = this.field5.Trim();
        string FILE_NAME = RAW_PATH.Replace("\\", "/"); // normalize for curl

        int FILE_LENGTH_IN_BYTES = 0;
        string SESSION_ID = "";
        string BUNDLE_INSTANCE_ID = "";
        string BUNDLE_ID = "";
        string BUNDLE_VERSION = "";
        string YOUR_GAME = "";
        // Get file length (in bytes) of the selected bundle file
        try
        {
            if (string.IsNullOrWhiteSpace(FILE_NAME))
            {
                OutputMessage("File path is empty. Please select a bundle file.", true);
                return;
            }

            if (!System.IO.File.Exists(FILE_NAME))
            {
                OutputMessage($"File not found: {FILE_NAME}", true);
                return;
            }

            FILE_LENGTH_IN_BYTES = (int)new System.IO.FileInfo(FILE_NAME).Length;
            OutputMessage($"Bundle file size: {FILE_LENGTH_IN_BYTES} bytes");
        }
        catch (Exception ex)
        {
            OutputMessage($"Failed to get file length: {ex.Message}", true);
            return;
        }

        // const string FILE_LENGTH_IN_BYTES = "{FILE_LENGTH_IN_BYTES}";
        // const string USER_ACCESS_TOKEN = "{USER_ACCESS_TOKEN}";


        // // step 1 : get upload session id :
        // curl -i -X POST "https://graph.facebook.com/v24.0/{APP_ID}/uploads
        //   ?file_name={FILE_NAME}
        //   &file_length={FILE_LENGTH_IN_BYTES}
        //   &file_type=application/zip
        //   &access_token={USER_ACCESS_TOKEN}"
        //   //response : {"id":"upload:{SESSION_ID"}

        /*
          var curlCommand =
                    $"-X POST \"https://graph-video.facebook.com/{app_id}/assets\" " +
                    $"-F \"access_token={app_access_token}\" " +
                    $"-F \"type=BUNDLE\" " +
                    $"-F \"asset=@{safePath}\" " +
                    $"-F \"comment={notes}\"";
                    */

        string getSessionIdCommand =
            $"-X POST \"https://graph.facebook.com/v24.0/{APP_ID}/uploads\" " +
            $"-F ?file_name={FILE_NAME}" +
            $"-F &file_length={FILE_LENGTH_IN_BYTES}" +
            $"-F &file_type=application/zip" +
            $"-F &access_token={APP_ACCESS_TOKEN}";
        // $"-F &access_token={USER_ACCESS_TOKEN}";

        var uploadCurlCommandResult = await RunCUrl(getSessionIdCommand);
        if (uploadCurlCommandResult.Success)
        {
            OutputMessage("Get Session Id Curl successful: " + uploadCurlCommandResult.Output);
        }
        else
        {
            OutputMessage("Get Session Id Curl failed!: " + uploadCurlCommandResult.Error);
            return;
        }

        // // step 2 : upload bundle:
        // curl -i -X POST "https://rupload.facebook.com/gg_graph_api/upload:{SESSION_ID}" \
        //    -H "Authorization: OAuth {USER_ACCESS_TOKEN}" \
        //    -H "Offset: 0" \
        //    -H "X-Entity-Length: {FILE_LENGTH_IN_BYTES}" \
        //    -H "content-length: {FILE_LENGTH_IN_BYTES}" \
        //    -H "type: BUNDLE" \
        //    -H "comment: Optional Bundle Upload Comment" \
        //    -H "name: {YOUR_GAME.ZIP}" \
        //    --data-binary @./{YOUR_GAME}.zip

        string uploadBundleCurlCommand = $"-X POST \"https://upload.facebook.com/gg_graph_api/upload:{SESSION_ID}\"  " +
          $"-H \"Authorization: OAuth {USER_ACCESS_TOKEN}\"  " +
          $"-H \"Offset: 0\"  " +
          $"-H \"X - Entity - Length: {FILE_LENGTH_IN_BYTES}\"  " +
          $"-H \"content - length: {FILE_LENGTH_IN_BYTES}\"  " +
          $"-H \"type: BUNDLE\"  " +
          $"-H \"comment: Optional Bundle Upload Comment\"  " +
          $"-H \"name: {FILE_NAME}\" " +
          $"--data-binary @./{YOUR_GAME}.zip";

        var uploadBundleCurlCommandResult = await RunCUrl(getSessionIdCommand);
        if (uploadBundleCurlCommandResult.Success)
        {
            OutputMessage("Upload Bundle successful: " + uploadBundleCurlCommandResult.Output);
        }
        else
        {
            OutputMessage("Upload Bundle failed!: " + uploadBundleCurlCommandResult.Error);
            return;
        }
        //    //return BUNDLE_INSTANCE_ID

        // // step 3 set bundle to prod:
        // curl -i -X POST "https://api.facebook.com/instant-games/assets/{APP_ID}/push-to-production" \
        //      -H "Content-Type: application/json" \
        //      -H "Authorization: OAuth {APP_ID}|{APP_ACCESS_TOKEN}" \
        //      -H "X-API-Version: 1.0.0" \
        //      -d '{"version_id": "{BUNDLE_INSTANCE_ID"}'


        // old code
        var curlCommand =
            $"-X POST \"https://graph-video.facebook.com/{APP_ID}/assets\" " +
            $"-F \"access_token={USER_ACCESS_TOKEN}\" " +
            $"-F \"type=BUNDLE\" " +
            $"-F \"asset=@{FILE_NAME}\" " +
            $"-F \"comment={notes}\"";
        UnityEngine.Debug.Log(curlCommand);
        // Indicate loading in the response box before starting the upload
        isLoading = true;
        OutputMessage("Uploading bundle...\nPlease wait.");
        Repaint();

        var result = await RunCUrl(curlCommand);

        // Indicate loading is complete
        isLoading = false;
        if (result.Success)
        {
            OutputMessage("Upload Bundle successful: " + result.Output);
        }
        else
        {
            OutputMessage("Upload Bundle failed!: " + result.Error);
        }
        // responseMessage = result.Output;
        Repaint();
    }

    public void OpenURL(string url)
    {
        //https://www.facebook.com/embed/instantgames/1368341408188711/player?game_url=https://localhost:8080

#if UNITY_EDITOR
        // Try Unity's URL opener first
        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                OutputMessage("OpenURL called with empty URL.");
                return;
            }

            OutputMessage($"Opening URL: {url}");
            Application.OpenURL(url);
        }
        catch (Exception ex)
        {
            OutputMessage($"Application.OpenURL failed, attempting OS-specific open. Error: {ex.Message}");

            // Fallbacks for different platforms
            try
            {
#if UNITY_EDITOR_WIN
                // Use "start" via cmd to open default browser on Windows
                var psi = new System.Diagnostics.ProcessStartInfo("cmd", $"/c start \"\" \"{url}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(psi);
#elif UNITY_EDITOR_OSX
            // Use "open" on macOS
            var psi = new System.Diagnostics.ProcessStartInfo("open", $"\"{url}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            System.Diagnostics.Process.Start(psi);
#elif UNITY_EDITOR_LINUX
            // Use xdg-open on Linux
            var psi = new System.Diagnostics.ProcessStartInfo("xdg-open", $"\"{url}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            System.Diagnostics.Process.Start(psi);
#else
             OutputMessage("Unsupported editor platform for opening URLs.");
#endif
            }
            catch (Exception osEx)
            {
                OutputMessage($"Failed to open URL via OS-specific method: {osEx.Message}");
            }
        }
#else
        // In non-editor contexts, still attempt Application.OpenURL
        Application.OpenURL(url);
#endif
    }

    public async Task<CurlResult> RunCUrl(string curlCommand)
    {
        var result = new CurlResult();

        try
        {
            // Create process start info
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "curl.exe",
                Arguments = curlCommand,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (var process = new System.Diagnostics.Process { StartInfo = processStartInfo })
            {
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                // Set up event handlers for async output reading
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        outputBuilder.AppendLine(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        errorBuilder.AppendLine(args.Data);
                    }
                };

                // Start the process
                process.Start();

                // Begin async reading
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for exit (with timeout)
                await Task.Run(() => process.WaitForExit(60000)); // 60 second timeout

                result.ExitCode = process.ExitCode;
                result.Output = outputBuilder.ToString();
                result.Error = errorBuilder.ToString();
                result.Success = process.ExitCode == 0;

                OutputMessage($"cURL Exit Code: {result.ExitCode}");
                OutputMessage($"cURL Output: {result.Output}");

                if (!string.IsNullOrEmpty(result.Error))
                {
                    OutputMessage($"cURL Error: {result.Error}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Exception running cURL: {ex.Message}";
            OutputMessage(result.Error);
        }

        return result;
    }

    public void ZipFolder()
    {
        // Prompt the user to select a folder to compress
        string folderPath = EditorUtility.OpenFolderPanel("Select Folder to Zip", "", "");
        OutputMessage("Zipping Folder now...", true);
        if (string.IsNullOrEmpty(folderPath))
        {
            OutputMessage("ZipFolder canceled: No folder selected.");
            return;
        }

        // Prompt the user to choose where to save the zip file
        string defaultZipName = System.IO.Path.GetFileName(folderPath) + ".zip";
        string saveZipPath = EditorUtility.SaveFilePanel("Save Zip File", System.IO.Path.GetDirectoryName(folderPath), defaultZipName, "zip");
        if (string.IsNullOrEmpty(saveZipPath))
        {
            OutputMessage("ZipFolder canceled: No save path selected.");
            return;
        }

        try
        {
            // Ensure the target directory exists
            string saveDir = System.IO.Path.GetDirectoryName(saveZipPath);
            if (!string.IsNullOrEmpty(saveDir) && !System.IO.Directory.Exists(saveDir))
            {
                System.IO.Directory.CreateDirectory(saveDir);
            }

            // If a file already exists at the destination, delete it to avoid exceptions
            if (System.IO.File.Exists(saveZipPath))
            {
                System.IO.File.Delete(saveZipPath);
            }

            // Use System.IO.Compression to zip the selected folder
            System.IO.Compression.ZipFile.CreateFromDirectory(folderPath, saveZipPath, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);

            OutputMessage($"Folder zipped successfully:\nSource: {folderPath}\nZip: {saveZipPath}");
            EditorUtility.RevealInFinder(saveZipPath);
        }
        catch (Exception ex)
        {
            OutputMessage($"Failed to zip folder: {ex.Message}");
            EditorUtility.DisplayDialog("Zip Error", $"Failed to zip folder:\n{ex.Message}", "OK");
        }

    }

    void OnGUI()
    {
        GUILayout.Label("Form Submission Window", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Input Field 1
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("App ID", GUILayout.Width(labelWidth));
        field1 = EditorGUILayout.TextField(field1);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        // Input Field 2
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("App Secret", GUILayout.Width(labelWidth));
        if (showAppSecret)
            field2 = EditorGUILayout.TextField(field2);
        else
            field2 = EditorGUILayout.PasswordField(field2);
        if (GUILayout.Button(showAppSecret ? "Hide" : "Show", GUILayout.Width(50)))
        {
            showAppSecret = !showAppSecret;
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        // Input Field 5
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("App Access Token", GUILayout.Width(labelWidth));
        if (showAppAccessToken)
            field5 = EditorGUILayout.TextField(field5);
        else
            field5 = EditorGUILayout.PasswordField(field5);
        if (GUILayout.Button(showAppAccessToken ? "Hide" : "Show", GUILayout.Width(50)))
        {
            showAppAccessToken = !showAppAccessToken;
        }
        if (GUILayout.Button("Get Token", GUILayout.Width(labelWidth)))
        {
            GetToken();
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        // Input Field 3
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Notes", GUILayout.Width(labelWidth));
        field3 = EditorGUILayout.TextField(field3);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        // Input Field 4
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Select File:", GUILayout.Width(labelWidth));
        field4 = EditorGUILayout.TextField(field4);
        if (GUILayout.Button("Browse...", GUILayout.Width(labelWidth)))
        {
            string path = EditorUtility.OpenFilePanel("Select Directory", "", "");
            if (!string.IsNullOrEmpty(path))
            {
                field4 = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        // SDK Version Dropdown
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("SDK Version", GUILayout.Width(labelWidth));
        int newSdkVersionIndex = EditorGUILayout.Popup(sdkVersionIndex, sdkVersionOptions);
        if (newSdkVersionIndex != sdkVersionIndex)
        {
            sdkVersionIndex = newSdkVersionIndex;
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        // Submit Button
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !isLoading;
        if (GUILayout.Button("Build Bundle", GUILayout.Height(30)))
        {
            //build game in webgl
            BuildGame();
        }
        if (GUILayout.Button("Zip Folder", GUILayout.Height(30)))
        {
            ZipFolder();
        }
        bool canUpload = !isLoading &&
            !string.IsNullOrEmpty(field1.Trim()) &&
            !string.IsNullOrEmpty(field2.Trim()) &&
            !string.IsNullOrEmpty(field4.Trim()) &&
            !string.IsNullOrEmpty(field5.Trim());
        GUI.enabled = canUpload;
        if (GUILayout.Button("Upload Bundle", GUILayout.Height(30)))
        {
            UploadBundle();
        }
        // if (GUILayout.Button("Upload Prod Bundle", GUILayout.Height(30)))
        // {
        //     UploadProdBundle(); //TODO: incomplete
        // }
        GUI.enabled = !isLoading;
        if (GUILayout.Button("Open Test URL", GUILayout.Height(30)))
        {
            RunTestServer();
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUI.enabled = true;
        // Loading indicator
        if (isLoading)
        {
            GUILayout.Space(10);
            GUILayout.Label("Loading...", EditorStyles.helpBox);
        }
        // Response Message
        if (!string.IsNullOrEmpty(responseMessage))
        {
            GUILayout.Space(10);
            GUILayout.Label("Response:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            EditorGUILayout.TextArea(responseMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }

    void RunTestServer()
    {
        var appId = field1 != null ? field1.Trim() : string.Empty;
        string testUrl = $"https://www.facebook.com/embed/instantgames/{appId}/player?game_url=https://localhost:8080";

        // Prompt user to select the build folder
        string defaultFolder = System.IO.Path.Combine(Application.dataPath, "../Builds/WebGL");
        string buildFolder = EditorUtility.OpenFolderPanel("Select Build Folder", defaultFolder, "");

        if (string.IsNullOrEmpty(buildFolder))
        {
            OutputMessage("Test server canceled: No build folder selected.", true);
            return;
        }

        OutputMessage($"Starting test server in: {buildFolder}", true);

        // check if key.pem and cert.pem are in the folder
        // if not, create them

        try
        {
#if UNITY_EDITOR_WIN
            // Windows: Open browser and start http-server in the selected build folder
            // Using cmd /k to keep window open, and adding pause on error
            var browserProcess = new ProcessStartInfo("cmd", $"/c start \"\" \"{testUrl}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(browserProcess);

            var serverProcess = new ProcessStartInfo("cmd", $"/k cd /d \"{buildFolder}\" && http-server --cors --ssl -c-1 -p 8080 -a 127.0.0.1 || pause")
            {
                CreateNoWindow = false,
                UseShellExecute = true
            };
            Process.Start(serverProcess);

            OutputMessage("Windows: Browser opened and http-server started on https://127.0.0.1:8080");
#elif UNITY_EDITOR_OSX
            // macOS: Open browser and start http-server in the selected build folder
            // Using bash with read to keep terminal open on error
            var browserProcess = new ProcessStartInfo("open", testUrl)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(browserProcess);

            var serverProcess = new ProcessStartInfo("open", $"-a Terminal \"{buildFolder}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(serverProcess);

            // Run the http-server command in a new Terminal window
            var httpServerProcess = new ProcessStartInfo("/bin/bash", $"-c \"cd '{buildFolder}' && http-server --cors --ssl -c-1 -p 8080 -a 127.0.0.1; echo ''; echo 'Press Enter to close...'; read\"")
            {
                CreateNoWindow = false,
                UseShellExecute = true
            };
            Process.Start(httpServerProcess);

            OutputMessage("macOS: Browser opened and http-server started on https://127.0.0.1:8080");
#elif UNITY_EDITOR_LINUX
            // Linux: Open browser and start http-server in the selected build folder
            // Using bash with read to keep terminal open on error
            var browserProcess = new ProcessStartInfo("xdg-open", testUrl)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(browserProcess);

            var serverProcess = new ProcessStartInfo("/bin/bash", $"-c \"cd '{buildFolder}' && http-server --cors --ssl -c-1 -p 8080 -a 127.0.0.1; echo ''; echo 'Press Enter to close...'; read\"")
            {
                CreateNoWindow = false,
                UseShellExecute = true
            };
            Process.Start(serverProcess);

            OutputMessage("Linux: Browser opened and http-server started on https://127.0.0.1:8080");
#else
            OutputMessage("Unsupported platform for running test server.");
#endif
            OutputMessage($"Test URL: {testUrl}");
        }
        catch (Exception ex)
        {
            OutputMessage($"Failed to start test server: {ex.Message}");
            EditorUtility.DisplayDialog("Test Server Error", $"Failed to start test server:\n{ex.Message}\n\nMake sure http-server is installed (npm install -g http-server)", "OK");
        }
    }
    void BuildGame()
    {

        try
        {
            OutputMessage("Starting WebGL build...", true);

            // Ensure WebGL module is available
#if UNITY_2020_1_OR_NEWER
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog("Build Error", "WebGL Build Target not supported in this Unity installation.", "OK");
                OutputMessage("WebGL Build Target not supported.");
                return;
            }
#endif

            // Collect enabled scenes from Build Settings
            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Build Error", "No scenes found in Build Settings.", "OK");
                OutputMessage("No scenes found in Build Settings.");
                return;
            }

            var scenePaths = new System.Collections.Generic.List<string>();
            foreach (var s in scenes)
            {
                if (s.enabled) scenePaths.Add(s.path);
            }

            if (scenePaths.Count == 0)
            {
                EditorUtility.DisplayDialog("Build Error", "No enabled scenes in Build Settings.", "OK");
                OutputMessage("No enabled scenes in Build Settings.");
                return;
            }

            // Choose output folder
            string defaultFolder = System.IO.Path.Combine(Application.dataPath, "../Builds/WebGL");
            string targetFolder = EditorUtility.SaveFolderPanel("Choose WebGL Build Folder", defaultFolder, "");

            if (string.IsNullOrEmpty(targetFolder))
            {
                OutputMessage("Build canceled by user.");
                return;
            }

            // Build options
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenePaths.ToArray(),
                locationPathName = targetFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            // Switch active build target if needed
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                OutputMessage("Switching Active Build Target to WebGL...");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                {
                    EditorUtility.DisplayDialog("Build Error", "Failed to switch Active Build Target to WebGL.", "OK");
                    OutputMessage("Failed to switch Active Build Target to WebGL.");
                    return;
                }
            }

            // Perform the build
#if UNITY_2021_2_OR_NEWER
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                OutputMessage($"Build Succeeded. Output: {targetFolder}");
                EditorUtility.RevealInFinder(targetFolder);
            }
            else
            {
                OutputMessage($"Build Failed: {report.summary.result} - Errors: {report.summary.totalErrors}, Warnings: {report.summary.totalWarnings}");
                EditorUtility.DisplayDialog("Build Failed", $"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}", "OK");
            }
#else
            string buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (string.IsNullOrEmpty(buildResult))
            {
                UpdateIndexHtmlSdkVersion(targetFolder);
                OutputMessage($"Build Succeeded. Output: {targetFolder}");
                EditorUtility.RevealInFinder(targetFolder);
            }
            else
            {
                OutputMessage($"Build Failed: {buildResult}");
                EditorUtility.DisplayDialog("Build Failed", buildResult, "OK");
            }
#endif
        }
        catch (Exception ex)
        {
            OutputMessage($"Build Exception: {ex.Message}");
            EditorUtility.DisplayDialog("Build Exception", ex.ToString(), "OK");
        }
    }



}


public class HelloWorldPostBuild : IPostprocessBuildWithReport
{
    // Priority for execution order if multiple postprocess scripts exist.
    public int callbackOrder => 0;

    // This method is automatically called by Unity after a build completes.
    public void OnPostprocessBuild(BuildReport report)
    {
        try
        {
            // Determine chosen SDK version from EditorPrefs index
            int sdkIndex = EditorPrefs.GetInt(InstantGameBundleUploadWindow.PREF_SDK_VERSION, 0);
            string[] sdkOptions = InstantGameBundleUploadWindow.sdkVersionOptions; // new string[] { "8.0", "restricted.latest", "7.1" };
            string sdkVersion = (sdkIndex >= 0 && sdkIndex < sdkOptions.Length) ? sdkOptions[sdkIndex] : sdkOptions[0];
            UnityEngine.Debug.Log("Setting SDK_VERSION : " + sdkVersion);

            // Locate index.html at the root of the build output
            string buildRoot = report.summary.outputPath;
            // For WebGL builds, outputPath may point to a folder. Ensure we use the folder path.
            if (System.IO.File.Exists(buildRoot))
            {
                buildRoot = System.IO.Path.GetDirectoryName(buildRoot);
            }
            string indexPath = System.IO.Path.Combine(buildRoot, "index.html");

            if (!System.IO.File.Exists(indexPath))
            {
                UnityEngine.Debug.LogWarning($"[PostBuild] index.html not found at: {indexPath}");
            }
            else
            {
                UnityEngine.Debug.Log("file @ " + indexPath);

                string html = System.IO.File.ReadAllText(indexPath, Encoding.UTF8);
                if (html.Contains("___SDK_VERSION___"))
                {
                    html = html.Replace("___SDK_VERSION___", sdkVersion);
                    System.IO.File.WriteAllText(indexPath, html, Encoding.UTF8);
                    UnityEngine.Debug.Log($"[PostBuild] Replaced ___SDK_VERSION___ with \"{sdkVersion}\" in index.html");
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[PostBuild] Placeholder ___SDK_VERSION___ not found in index.html");
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[PostBuild] Failed to update ___SDK_VERSION___ in index.html: {ex.Message}");
        }

    }
}
