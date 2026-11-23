using System;
using System.IO;
using System.Linq;
using System.Security;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OutsideTheBox.Editor
{
    public sealed class FirebaseAndroidConfigBuilder : IPreprocessBuildWithReport
    {
        private const string OutputRoot = "Assets/Plugins/Android/OutsideTheBoxFirebase.androidlib";
        private const string OutputXml = OutputRoot + "/res/values/google-services.xml";
        public int callbackOrder => -500;

        [Serializable] private sealed class Root
        {
            public ProjectInfo project_info;
            public Client[] client;
        }

        [Serializable] private sealed class ProjectInfo
        {
            public string project_number;
            public string project_id;
            public string firebase_url;
            public string storage_bucket;
        }

        [Serializable] private sealed class Client
        {
            public ClientInfo client_info;
            public ApiKey[] api_key;
            public OAuthClient[] oauth_client;
        }

        [Serializable] private sealed class ClientInfo
        {
            public string mobilesdk_app_id;
            public AndroidClientInfo android_client_info;
        }

        [Serializable] private sealed class AndroidClientInfo
        {
            public string package_name;
        }

        [Serializable] private sealed class ApiKey
        {
            public string current_key;
        }

        [Serializable] private sealed class OAuthClient
        {
            public string client_id;
            public int client_type;
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            if (!TryGenerate(out var error)) throw new BuildFailedException(error);
        }

        [MenuItem("Tools/Outside The Box/Generate Firebase Android Resources")]
        private static void GenerateFromMenu()
        {
            if (!TryGenerate(out var error))
            {
                Debug.LogError(error);
                return;
            }
            Debug.Log("Firebase Android resources generated for this game's package ID.");
        }

        public static bool TryGenerate(out string error)
        {
            error = string.Empty;
            var jsonPath = Directory.GetFiles(Application.dataPath, "google-services.json",
                SearchOption.AllDirectories).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                error = "Firebase google-services.json is missing. Download the file for com.outsidethebox.puzzle and place it in Assets.";
                return false;
            }

            Root root;
            try { root = JsonUtility.FromJson<Root>(File.ReadAllText(jsonPath)); }
            catch (Exception exception)
            {
                error = $"Firebase google-services.json is invalid: {exception.Message}";
                return false;
            }

            var packageName = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            var client = root?.client?.FirstOrDefault(candidate =>
                string.Equals(candidate?.client_info?.android_client_info?.package_name, packageName,
                    StringComparison.Ordinal));
            if (client == null)
            {
                error = $"google-services.json has no Android client matching package '{packageName}'. Do not reuse another game's Firebase file.";
                return false;
            }

            var appId = client.client_info?.mobilesdk_app_id;
            var apiKey = client.api_key?.FirstOrDefault()?.current_key;
            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(root.project_info?.project_number) ||
                string.IsNullOrWhiteSpace(root.project_info?.project_id))
            {
                error = "google-services.json is missing required Firebase Android fields.";
                return false;
            }

            var webClient = client.oauth_client?.FirstOrDefault(item => item.client_type == 3)?.client_id;
            Directory.CreateDirectory(Path.GetFullPath(OutputRoot + "/res/values"));
            var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n" +
                      Value("google_app_id", appId) +
                      Value("google_api_key", apiKey) +
                      Value("google_crash_reporting_api_key", apiKey) +
                      Value("gcm_defaultSenderId", root.project_info.project_number) +
                      Value("project_id", root.project_info.project_id) +
                      Value("google_storage_bucket", root.project_info.storage_bucket) +
                      Value("firebase_database_url", root.project_info.firebase_url) +
                      Value("default_web_client_id", webClient) +
                      "</resources>\n";
            File.WriteAllText(Path.GetFullPath(OutputXml), xml);
            AssetDatabase.ImportAsset(OutputXml, ImportAssetOptions.ForceUpdate);
            return true;
        }

        private static string Value(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return $"  <string name=\"{name}\" translatable=\"false\">{SecurityElement.Escape(value)}</string>\n";
        }
    }
}
