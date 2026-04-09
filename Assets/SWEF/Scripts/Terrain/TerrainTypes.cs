namespace SWEF.Terrain
{
    /// <summary>
    /// LOD levels for terrain chunks. Values increase with distance/coarseness.
    /// Defined here (in SWEF.Terrain) so that both SWEF.Terrain.TerrainChunk and
    /// SWEF.LOD.LODManager can share the enum without creating a circular assembly
    /// dependency. SWEF.LOD references SWEF.Terrain; SWEF.Terrain does not reference SWEF.LOD.
    /// </summary>
    public enum TerrainLODLevel
    {
        /// <summary>Highest resolution — full vertex count.</summary>
        Full    = 0,
        /// <summary>Half resolution.</summary>
        Half    = 1,
        /// <summary>Quarter resolution.</summary>
        Quarter = 2,
        /// <summary>Minimal (1/8) resolution.</summary>
        Minimal = 3,
        /// <summary>Completely hidden / frustum-culled.</summary>
        Culled  = 4
    }
}
