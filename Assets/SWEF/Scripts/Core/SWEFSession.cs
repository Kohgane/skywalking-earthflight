namespace SWEF.Core
{
    /// <summary>
    /// Minimal static session: passes location data from Boot scene to World scene.
    /// </summary>
    public static class SWEFSession
    {
        public static double Lat;
        public static double Lon;
        public static double Alt;
        public static bool HasFix;

        public static void Set(double lat, double lon, double alt)
        {
            Lat = lat;
            Lon = lon;
            Alt = alt;
            HasFix = true;
        }

        public static void Clear()
        {
            Lat = 0;
            Lon = 0;
            Alt = 0;
            HasFix = false;
        }
    }
}
