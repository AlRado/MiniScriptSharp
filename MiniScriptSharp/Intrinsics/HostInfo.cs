namespace MiniScriptSharp.Intrinsics {

    /// <summary>
    /// Information about the app hosting MiniScript.  Set this in your main program.
    /// This is provided to the user via the `version` intrinsic.
    /// </summary>
    public static class HostInfo {

        public static string Name; // name of the host program
        public static string Info; // URL or other short info about the host
        public static double Version; // host program version number

    }

}