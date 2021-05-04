namespace Miniscript.sources.intrinsic {

    /// <summary>
    /// Information about the app hosting Minisript.  Set this in your main program.
    /// This is provided to the user via the `version` intrinsic.
    /// </summary>
    public static class HostInfo {

        public static string name; // name of the host program
        public static string info; // URL or other short info about the host
        public static double version; // host program version number

    }

}