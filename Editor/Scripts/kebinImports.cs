using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SimpleJSON;
using kebinClient;
using semver;

/* 
    This file contains the general functionality of kebinImports
*/

[InitializeOnLoad]
public partial class kebinImports : MonoBehaviour
{
    // Private accessor for JSON nodes
    private static JSONNode jsonNode = null;

    // Private accessor for modified kebinClient, an implementation of HttpClient, which is responsible for downloads
    private static ModHttpClient client = null;

    // kebinImports Settings
    private static bool hideWarnings, showSplash;

    // Get the registry key of import options
    private static string IOA = string.Empty, IOW = string.Empty;

    // Clears the UnityEditor Console
    private static void kebin_ClearLog()
    {
        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }

    // Method to import a unitypackage by file or by URL
    private static void ImportAsset(string fileOrURL, bool zippedAssets = false, bool zippedPackage = false, string name = "")
    {
        Selection.activeGameObject = null;
        string path = string.Empty;
        bool file = false;
        if (!fileOrURL.StartsWith("http") && File.Exists(fileOrURL))
        {
            path = fileOrURL;
            file = true;
        }
        else
        {
            path = Application.persistentDataPath + @"/" + fileOrURL.Substring(fileOrURL.LastIndexOf('/') + 1);
            Task.Run(() => ModHttpClient.DownloadFile(client, fileOrURL, @path)).Wait();
        }
        if (!zippedAssets && !zippedPackage)
            AssetDatabase.ImportPackage(@path, false);
        else if (zippedAssets && !zippedPackage)
            ZipFile.ExtractToDirectory(@path, Application.dataPath + @"/" + name);
        else if (zippedPackage && !zippedAssets)
        {
            ZipFile.ExtractToDirectory(@path, Application.persistentDataPath + @"/Extracted");
            // Look for a unitypackage the folder and import it
            foreach (string filePath in Directory.GetFiles(Application.persistentDataPath + @"/Extracted", "*.unitypackage", SearchOption.AllDirectories))
                AssetDatabase.ImportPackage(filePath, false);
        }

        if (file == false && File.Exists(@path))
        {
            File.Delete(@path);
            if (Directory.Exists(Application.persistentDataPath + @"/Extracted"))
                Directory.Delete(Application.persistentDataPath + @"/Extracted", true);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // Update MenuItem Checked States
    private static void UpdateSettings()
    {

        // Update EditorPrefs from last Update
        try
        {
            if (EditorPrefs.GetString("kebinImports.IOA", "1100000").Length != 7 && EditorPrefs.GetString("kebinImports.IOW", "110000").Length != 6)
            {
                IOA = "1100000";
                IOW = "110000";
                EditorPrefs.SetString("kebinImports.IOA", IOA);
                EditorPrefs.SetString("kebinImports.IOW", IOW);
            }

            IOA = EditorPrefs.GetString("kebinImports.IOA", "1100000");
            IOW = EditorPrefs.GetString("kebinImports.IOW", "110000");

            hideWarnings = EditorPrefs.GetBool("kebinImports.hideWarnings", false);
            showSplash = EditorPrefs.GetBool("kebinImports.showSplash", true);

            if (IOA[0] == '1')
                Menu.SetChecked("kebinImports/Customize/Avatar Essentials/VRChat SDK 3", true);
            if (IOA[1] == '1')
                Menu.SetChecked("kebinImports/Customize/Avatar Essentials/Poiyomi Toon Shader", true);
            if (IOA[2] == '1')
                Menu.SetChecked("kebinImports/Customize/Avatar Essentials/Unity-Chan Toon Shader 2.0", true);
            if (IOA[3] == '1')
                Menu.SetChecked("kebinImports/Customize/Avatar Essentials/lilToon", true);
            if (IOA[4] == '1')
                Menu.SetChecked("kebinImports/Customize/Avatar Essentials/Dynamic Bone", true);
            if (IOA[5] == '1')
                Menu.SetChecked("kebinImports/Customize/Avatar Essentials/Pumkin's Avatar Tools", true);
            if (IOA[6] == '1')
                Menu.SetChecked("kebinImports/Customize/Avatar Essentials/Muscle Animation Editor", true);

            if (IOW[0] == '1')
                Menu.SetChecked("kebinImports/Customize/World Essentials/VRChat SDK 3", true);
            if (IOW[1] == '1')
                Menu.SetChecked("kebinImports/Customize/World Essentials/Poiyomi Toon Shader", true);
            if (IOW[2] == '1')
                Menu.SetChecked("kebinImports/Customize/World Essentials/Unity-Chan Toon Shader 2.0", true);
            if (IOW[3] == '1')
                Menu.SetChecked("kebinImports/Customize/World Essentials/lilToon", true);
            if (IOW[4] == '1')
                Menu.SetChecked("kebinImports/Customize/World Essentials/VRWorld Toolkit", true);
            if (IOW[5] == '1')
                Menu.SetChecked("kebinImports/Customize/World Essentials/UdonSharp", true);
        }
        catch (System.Exception)
        {
            Debug.LogError("[kebinImports] Error: Attempted to Update Settings, but failed miserably. Seek help and advice from the Discord Server.");
        }
    }

    private static void updateSelf()
    {
        EditorApplication.update -= updateSelf;
        client = new ModHttpClient(true);
        jsonNode = JSON.Parse(Task.Run(() => ModHttpClient.DownloadString(client, "https://api.github.com/repos/EEkebin/kebinImports/releases/latest")).Result);
        string currVersion = JSON.Parse(File.ReadAllText(Application.dataPath + @"/kebinImports/Editor/Scripts/info.json"))["version"];
        if (CompareVersions.compareVersions(currVersion, jsonNode["tag_name"].ToString().Substring(1)) == 1)
        {
            if (Directory.Exists(Application.dataPath + @"/kebinImports"))
                Directory.Delete(Application.dataPath + @"/kebinImports", true);
            if (File.Exists(Application.dataPath + @"/kebinImports.meta"))
                File.Delete(Application.dataPath + @"/kebinImports.meta");
            FSDSHandler(true);
            ImportAsset("https://github.com/EEkebin/kebinImports/releases/latest/download/kebinImports.unitypackage");
        }
    }

    // Entry point
    static kebinImports()
    {
        EditorApplication.update += updateSelf;
        UpdateSettings();
        kebin_ClearLog();
        if (showSplash == true)
            EditorApplication.update += settingsWindow.showWindow;
    }
}
