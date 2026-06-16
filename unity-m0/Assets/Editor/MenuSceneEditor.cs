#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NaijaEmpires
{
    /// Editor convenience: creates the front-end Menu scene (one GameObject with GameFlow), wires the
    /// AppIcon logo, saves it to Assets/Scenes/Menu.unity, and makes it build-scene 0 (the app entry)
    /// with Skirmish right after. Mirrors NaijaEmpiresEditor. Menu: "Naija Empires".
    public static class MenuSceneEditor
    {
        const string MenuPath = "Assets/Scenes/Menu.unity";
        const string SkirmishPath = "Assets/Scenes/Skirmish.unity";
        const string LogoPath = "Assets/Art/AppIcon.png";

        [MenuItem("Naija Empires/Create & Open Menu Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var go = new GameObject("_GameFlow");
            var flow = go.AddComponent<GameFlow>();

            // The splash logo lives outside Resources/, so assign it here at author-time.
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            if (logo != null) flow.Logo = logo;
            else Debug.LogWarning($"Naija Empires: logo not found at {LogoPath}; splash will show wordmark only.");

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, MenuPath);

            RegisterBuildScenes();

            AssetDatabase.Refresh();
            Debug.Log("Naija Empires: Menu scene created at " + MenuPath +
                      " and set as build-scene 0 (Skirmish follows). Press Play.");
        }

        /// Make Menu the entry scene (index 0) with Skirmish right after it. The Skirmish entry is
        /// added by path even if its .unity isn't built yet (the "Create Skirmish Scene" item makes
        /// the file); GameFlow loads it by name at runtime.
        static void RegisterBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(SkirmishPath, true),
            };
        }
    }
}
#endif
