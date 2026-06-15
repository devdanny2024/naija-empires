#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NaijaEmpires
{
    /// Editor convenience: creates the playable Skirmish scene (an empty scene with the Bootstrap
    /// component) so you can just press Play. Menu: "Naija Empires".
    public static class NaijaEmpiresEditor
    {
        [MenuItem("Naija Empires/Create & Open Skirmish Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("_Bootstrap");
            go.AddComponent<Bootstrap>();

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Skirmish.unity");
            AssetDatabase.Refresh();
            Debug.Log("Naija Empires: Skirmish scene created at Assets/Scenes/Skirmish.unity — press Play.");
        }
    }
}
#endif
