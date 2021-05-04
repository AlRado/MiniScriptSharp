namespace Miniscript.sources.parser {

    // BackPatch: represents a place where we need to patch the code to fill
    // in a jump destination (once we figure out where that destination is).
    internal class BackPatch {

        public int lineNum; // which code line to patch
        public string waitingFor; // what keyword we're waiting for (e.g., "end if")

    }

}