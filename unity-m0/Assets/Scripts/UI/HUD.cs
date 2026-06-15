using UnityEngine;

namespace NaijaEmpires
{
    /// Zero-setup IMGUI HUD: resources, population, age + age-up, build menu, the selected
    /// building's train menu, and the victory/defeat banner.
    public class HUD : MonoBehaviour
    {
        void OnGUI()
        {
            GUI.skin.label.richText = true;
            var e = Match.Econ(FactionId.Player);

            // Resource / age panel
            GUILayout.BeginArea(new Rect(10, 10, 340, 150), GUI.skin.box);
            GUILayout.Label("<b>Naija Empires — M1</b>   Player: Benin");
            if (e != null)
            {
                GUILayout.Label($"Yam <b>{e.Yam}</b>    Timber <b>{e.Timber}</b>    Iron <b>{e.Iron}</b>");
                GUILayout.Label($"Pop {e.PopUsed}/{e.PopCap}     Age {e.Age}");
                if (e.Age < Ages.Max)
                {
                    Cost c = Ages.CostFor(e.Age + 1);
                    GUI.enabled = e.CanAfford(c);
                    if (GUILayout.Button($"Advance to Age {e.Age + 1}   ({Fmt(c)})")) Ages.TryAdvance(FactionId.Player);
                    GUI.enabled = true;
                }
                else GUILayout.Label("Max Age reached");
            }
            GUILayout.EndArea();

            // Build menu
            GUILayout.BeginArea(new Rect(10, 170, 340, 200), GUI.skin.box);
            GUILayout.Label("<b>Build</b>");
            BuildButton(BuildingKind.House, e);
            BuildButton(BuildingKind.Barracks, e);
            BuildButton(BuildingKind.Tower, e);
            BuildButton(BuildingKind.Stable, e);
            if (BuildPlacer.Instance != null && BuildPlacer.Instance.Placing)
                GUILayout.Label("Placing — left-click to build, right-click to cancel");
            GUILayout.EndArea();

            // Train menu for the selected building
            var pb = SelectionManager.Instance != null ? SelectionManager.Instance.SelectedBuilding : null;
            if (pb != null && e != null)
            {
                GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 170), GUI.skin.box);
                GUILayout.Label("<b>Train</b>");
                foreach (var t in pb.Trainable)
                {
                    Cost c = UnitConfig.CostOf(t);
                    GUI.enabled = pb.CanTrain(t) && e.CanAfford(c) && e.HasPop(1);
                    string label = e.Age >= UnitConfig.AgeRequired(t)
                        ? $"Train {t}  ({Fmt(c)})"
                        : $"{t}  (needs Age {UnitConfig.AgeRequired(t)})";
                    if (GUILayout.Button(label)) pb.Train(t);
                    GUI.enabled = true;
                }
                if (pb.QueueCount > 0) GUILayout.Label($"In queue: {pb.QueueCount}");
                GUILayout.EndArea();
            }

            // Controls help
            GUILayout.BeginArea(new Rect(10, Screen.height - 72, 740, 62), GUI.skin.box);
            GUILayout.Label("<b>Units</b>  left-click / drag = select   •   right-click = move / gather / attack enemy");
            GUILayout.Label("<b>Tip</b>  select your Town Centre or Barracks to train   •   WASD pan, scroll zoom");
            GUILayout.EndArea();

            // Win / lose banner
            if (Match.Over)
            {
                string msg = Match.Winner == FactionId.Player ? "VICTORY" : "DEFEAT";
                var style = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 42,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                style.normal.textColor = Match.Winner == FactionId.Player
                    ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.4f, 0.3f);
                GUI.Box(new Rect(Screen.width / 2f - 170, Screen.height / 2f - 60, 340, 120), msg, style);
            }
        }

        void BuildButton(BuildingKind k, Economy e)
        {
            if (e == null) return;
            bool ageOk = e.Age >= BuildingConfig.AgeRequired(k);
            Cost c = BuildingConfig.CostOf(k, e.Civ);
            GUI.enabled = ageOk && e.CanAfford(c);
            string label = ageOk ? $"{k}   ({Fmt(c)})" : $"{k}   (needs Age {BuildingConfig.AgeRequired(k)})";
            if (GUILayout.Button(label) && BuildPlacer.Instance != null) BuildPlacer.Instance.BeginPlace(k);
            GUI.enabled = true;
        }

        static string Fmt(Cost c)
        {
            string s = "";
            if (c.Yam > 0) s += c.Yam + "Y ";
            if (c.Timber > 0) s += c.Timber + "T ";
            if (c.Iron > 0) s += c.Iron + "I";
            return s.Trim();
        }
    }
}
