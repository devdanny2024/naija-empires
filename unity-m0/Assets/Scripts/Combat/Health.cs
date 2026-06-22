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

            const float w = 36f, h = 5f;
            float x = sp.x - w / 2f;
            float y = Screen.height - sp.y;
            float frac = Mathf.Clamp01(Current / Max);

            var prev = GUI.color;
            GUI.color = Color.black; GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, h + 2), Texture2D.whiteTexture);
            GUI.color = new Color(0.6f, 0.1f, 0.1f); GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.85f, 0.3f); GUI.DrawTexture(new Rect(x, y, w * frac, h), Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
