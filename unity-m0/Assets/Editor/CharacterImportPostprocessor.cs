#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace NaijaEmpires
{
    /// Forces correct import settings on the animated Quaternius character FBX so they come in
    /// rigged + animated, with their natural per-part colours — no manual clicking in the Inspector.
    /// Scoped to the four character files only; building/nature FBX are left untouched.
    public class CharacterImportPostprocessor : AssetPostprocessor
    {
        static readonly HashSet<string> Characters = new()
        { "Worker_Male", "Soldier_Male", "BlueSoldier_Male", "Knight_Male" };

        void OnPreprocessModel()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("Resources/NE/Models/")) return;
            if (!Characters.Contains(Path.GetFileNameWithoutExtension(path))) return;

            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.optimizeGameObjects = false;
            // Pull the Quaternius per-part base colours (Skin/Shirt/Pants/...) through as materials,
            // so units look human instead of importing flat white.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }
    }
}
#endif
