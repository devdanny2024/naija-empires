using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// A fast, unarmed explorer trained at the Town Centre. Move commands QUEUE as waypoints: clicking a
    /// new spot doesn't redirect the scout — it finishes its current leg, then continues to each queued
    /// spot in the order you clicked. Lets the player line up several places to explore in one go.
    /// (Fog of war reveals around it as it travels, so it scouts the map automatically.)
    public class Scout : Unit
    {
        readonly Queue<Vector3> _waypoints = new();

        /// Append a waypoint instead of replacing the destination — the scout visits every clicked spot.
        public override void MoveTo(Vector3 pos) => _waypoints.Enqueue(pos);

        protected override void Update()
        {
            // MoveStep returns true when it arrives (or is idle); then head to the next queued waypoint.
            if (MoveStep() && _waypoints.Count > 0) SetTarget(_waypoints.Dequeue());
        }
    }
}
