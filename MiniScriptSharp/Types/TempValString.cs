namespace MiniScriptSharp.Types {

    /// <summary>
    /// We frequently need to generate a ValString out of a string for fleeting purposes,
    /// like looking up an identifier in a map (which we do ALL THE TIME).  So, here's
    /// a little recycling pool of reusable ValStrings, for this purpose only.
    /// </summary>
    class TempValString : ValString {

        private TempValString next;

        private TempValString(string s) : base(s) {
            this.next = null;
        }

        private static TempValString tempPoolHead;
        private static object lockObj = new object();

        public static TempValString Get(string s) {
            lock (lockObj) {
                if (tempPoolHead == null) {
                    return new TempValString(s);
                }
                else {
                    var result = tempPoolHead;
                    tempPoolHead = tempPoolHead.next;
                    result.Value = s;
                    return result;
                }
            }
        }

        public static void Release(TempValString temp) {
            lock (lockObj) {
                temp.next = tempPoolHead;
                tempPoolHead = temp;
            }
        }

    }

}