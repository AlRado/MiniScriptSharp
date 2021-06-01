namespace MiniScriptSharp.Parse {

    /// <summary>
    /// BackPatch: represents a place where we need to patch the code to fill
    /// in a jump destination (once we figure out where that destination is).
    /// </summary>
    internal class BackPatch {

        public int LineNum; // which code line to patch
        public string WaitingFor; // what keyword we're waiting for (e.g., "end if")

    }

}