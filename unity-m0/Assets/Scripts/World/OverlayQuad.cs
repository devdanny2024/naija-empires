using UnityEngine;

namespace NaijaEmpires
{
    /// A flat ground-aligned quad spanning the whole playable square with EXPLICIT UVs:
    /// world (-Half,-Half) -> UV (0,0) and (+Half,+Half) -> UV (1,1). The fog and territory overlays
    /// use this instead of Unity's primitive Plane, whose UV orientation is ambiguous and was mirroring
    /// the overlay so it didn't line up with world/unit positions. Pixel (cx,cz) of a Grid texture maps
    /// to world (origin + cx*cell, origin + cz*cell), matching how both overlays write their grids.
    public static class OverlayQuad
    {
        public static Mesh Mesh()
        {
            float h = MapBounds.Half;
            var m = new Mesh { name = "OverlayQuad" };
            m.vertices = new[]
            {
                new Vector3(-h, 0f, -h), // UV (0,0)
                new Vector3( h, 0f, -h), // UV (1,0)
                new Vector3(-h, 0f,  h), // UV (0,1)
                new Vector3( h, 0f,  h), // UV (1,1)
            };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            m.triangles = new[] { 0, 2, 1, 2, 3, 1 }; // both faces up (+Y) so the camera sees them
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
