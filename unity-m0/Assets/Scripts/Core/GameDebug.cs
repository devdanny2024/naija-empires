using UnityEngine;

namespace NaijaEmpires
{
    /// Developer/test toggles. TestMode makes every economy check free (unlimited resources, no pop
    /// cap, free age-advance / build / train / upgrade) so you can exercise the whole tech tree fast.
    public static class GameDebug
    {
        /// When true, Economy.CanAfford/Spend/HasPop all pass for free. Toggle in-game with F9.
        /// Ships OFF — real economy. (F9 is a desktop-only dev convenience; no effect on iOS device.)
        public static bool TestMode = false;
    }

    /// Tiny in-scene controller: F9 toggles TestMode and draws a corner indicator while it's on.
    /// Added by Bootstrap.BuildManagers.
    public class TestModeController : MonoBehaviour
    {
        GUIStyle _style;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9)) GameDebug.TestMode = !GameDebug.TestMode;
        }

        void OnGUI()
        {
            if (!GameDebug.TestMode) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
                _style.normal.textColor = new Color(1f, 0.85f, 0.2f);
            }
            GUI.Label(new Rect(Screen.width * 0.5f - 170f, Screen.height - 52f, 360f, 22f),
                      "● TEST MODE — unlimited resources  ·  F9 to toggle", _style);
        }
    }
}
