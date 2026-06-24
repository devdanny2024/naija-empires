using System;
using UnityEngine;

namespace NaijaEmpires
{
    /// HP for any unit or building. Draws a small health bar when damaged. Destroys on death.
    public class Health : MonoBehaviour
    {
        public float Max = 50f;
        public float Current { get; private set; }
        public bool Dead { get; private set; }

        public event Action<Health> Died;

        void Awake() => Current = Max;

        public void Init(float max) { Max = max; Current = max; }

        public void TakeDamage(float dmg)
        {
            if (Dead) return;
            Current -= dmg;
            if (Current <= 0f) { Die(); return; }

            var anim = GetComponent<ModelAnimator>();
            if (anim != null) anim.Hit();
        }

        /// Restore HP up to Max (used by villager repair). No-op once dead.
        public void Heal(float amount)
        {
            if (Dead || amount <= 0f) return;
            Current = Mathf.Min(Max, Current + amount);
        }

        void Die()
        {
            Dead = true;
            Died?.Invoke(this); // pop bookkeeping etc. happens immediately

            // Play the death clip if the rig has one; otherwise (buildings/primitives) remove at once.
            var anim = GetComponent<ModelAnimator>();
            bool animatedDeath = anim != null && anim.Die();

            // Become an inert corpse: stop moving/fighting and stop blocking clicks/pathing.
            var unit = GetComponent<Unit>(); if (unit != null) unit.enabled = false;
            var sel = GetComponent<Selectable>(); if (sel != null) sel.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

            Destroy(gameObject, animatedDeath ? 2f : 0f);
        }

        void OnGUI()
        {
            if (Dead || Current >= Max) return;

            // Don't draw enemy health bars through the fog — an IMGUI bar isn't a Renderer, so the fog
            // doesn't hide it, and a floating bar in the dark would give away an enemy's position.
            var faction = GetComponent<Faction>();
            if (faction != null && faction.Id != FactionId.Player)
            {
                var fog = FogOfWar.Instance;
                if (fog != null)
                {
                    bool isUnit = GetComponent<Unit>() != null;
                    bool seen = isUnit ? fog.IsVisible(transform.position) : fog.IsExplored(transform.position);
                    if (!seen) return;
                }
            }

            var cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.4f);
            if (sp.z <= 0) return;

            const float w = 42f, h = 7f;
            float x = sp.x - w / 2f;
            float y = Screen.height - sp.y;
            float frac = Mathf.Clamp01(Current / Max);
            float fw = w * frac;

            // Health colour shifts green (full) → yellow → red (low) for instant readability.
            Color full = frac > 0.5f
                ? Color.Lerp(new Color(0.90f, 0.78f, 0.18f), new Color(0.28f, 0.82f, 0.34f), (frac - 0.5f) * 2f)
                : Color.Lerp(new Color(0.82f, 0.16f, 0.14f), new Color(0.90f, 0.78f, 0.18f), frac * 2f);

            var prev = GUI.color;
            void Quad(float qx, float qy, float qw, float qh, Color c)
            { GUI.color = c; GUI.DrawTexture(new Rect(qx, qy, qw, qh), Texture2D.whiteTexture); }

            Quad(x - 1f, y + 2f, w + 2f, h + 1f, new Color(0f, 0f, 0f, 0.35f));          // drop shadow → "floating" 3D feel
            Quad(x - 1.6f, y - 1.6f, w + 3.2f, h + 3.2f, new Color(0.04f, 0.04f, 0.06f)); // dark frame
            Quad(x, y, w, h, new Color(0.16f, 0.05f, 0.05f));                            // empty track
            Quad(x, y, fw, h, full * 0.62f);                                            // shaded base (bottom)
            Quad(x, y, fw, h * 0.5f, full);                                             // brighter top half → gradient
            Quad(x, y, fw, 1.5f, new Color(1f, 1f, 1f, 0.45f));                          // glossy top sheen
            GUI.color = prev;
        }
    }
}
