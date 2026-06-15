#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace NaijaEmpires
{
    /// One-shot iOS configuration for the App Store / TestFlight build.
    /// Run once via the menu (settings persist in ProjectSettings.asset, which is committed).
    /// Sets bundle id, landscape orientation, and product/company names.
    /// (Set the app icon manually — see the log message.)
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

            AssetDatabase.SaveAssets();
            Debug.Log($"[IOSBuildSetup] iOS configured: {BundleId}, landscape. " +
                      "Now set the icon: Edit > Project Settings > Player > iOS > Icon — drag in Assets/Art/AppIcon.png.");
        }
    }
}
#endif
