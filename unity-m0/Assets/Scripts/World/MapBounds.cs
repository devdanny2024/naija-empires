using UnityEngine;

namespace NaijaEmpires
{
    /// Single source of truth for the playable map size. Everything that needs to know
    /// "how big is the world" (Bootstrap ground/water sizing, base spawn corners,
    /// TerritoryManager grid extent, FogOfWar grid extent, the HUD minimap) reads from
    /// here so the map can be resized in one place.
    ///
    /// Half = the half-extent of the square playable area in world units. The ground spans
    /// [-Half, +Half] on both X and Z (a 2*Half square centred at the origin).
    public static class MapBounds
    {
        /// Playable half-extent in world units. Ground is (2*Half) x (2*Half) centred at origin.
        public const float Half = 60f;

        /// Full side length of the playable square (world units).
        public const float Size = Half * 2f;

        /// How far in from the edge the corner bases sit (keeps them off the shoreline).
        public const float BaseInset = 14f;

        /// True if a world position is inside the playable square.
        public static bool Contains(Vector3 world) =>
            world.x >= -Half && world.x <= Half && world.z >= -Half && world.z <= Half;

        /// Clamp a world position to the playable square (Y preserved).
        public static Vector3 Clamp(Vector3 world)
        {
            world.x = Mathf.Clamp(world.x, -Half, Half);
            world.z = Mathf.Clamp(world.z, -Half, Half);
            return world;
        }
    }
}
