#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace NaijaEmpires
{
    /// One-shot iOS configuration for the App Store / TestFlight build.
    /// Run once via the menu (settings persist in ProjectSettings.asset, which is committed),
    /// or CI can call Configure() before building. Sets bundle id, landscape orientation,
    /// product/company names, and the app icon from Assets/Art/AppIcon.png.
    public static class IOSBuildSetup
    {
        const string BundleId = "com.buildafr.naijaempires";

        [MenuItem("Naija Empires/Configure iOS Build Settings")]
        public static void Configure()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.productName = "Naija Empires";
            PlayerSettings.companyName = "BuildAfr";

            // Landscape-only RTS.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.iOS.targetOSVersionString = "13.0";

            // App icon from the logo.
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/AppIcon.png");
            if (icon != null)
            {
                var sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.iOS, IconKind.Application);
                var arr = new Texture2D[sizes.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = icon;
                PlayerSettings.SetIcons(NamedBuildTarget.iOS, arr, IconKind.Application);
            }
            else Debug.LogWarning("[IOSBuildSetup] Assets/Art/AppIcon.png not found — icon not set.");

            AssetDatabase.SaveAssets();
            Debug.Log($"[IOSBuildSetup] iOS configured: {BundleId}, landscape, icon. Commit ProjectSettings.asset.");
        }
    }
}
#endif
