using MiniScriptSharp.Constants;
using MiniScriptSharp.Tac;

namespace MiniScriptSharp.Types {

    /// <summary>
    /// ValNull is an object to represent null in places where we can't use
    /// an actual null (such as a dictionary key or value).
    /// </summary>
    public class ValNull : Value {
        
        /// <summary>
        /// Handy accessor to a shared "instance".
        /// </summary>
        public static readonly ValNull Instance = new ValNull();

        private ValNull() {
        }

        public override string ToString(Machine machine) {
            return Consts.NULL;
        }

        public override bool IsA(Value type, Machine vm) {
            return false;
        }

        public override int Hash(int recursionDepth = 16) {
            return -1;
        }

        public override Value Val(Context context) {
            return null;
        }

        public override Value Val(Context context, out ValMap valueFoundIn) {
            valueFoundIn = null;
            return null;
        }

        public override Value FullEval(Context context) {
            return null;
        }

        public override int IntValue() {
            return 0;
        }

        public override double DoubleValue() {
            return 0.0;
        }

        public override bool BoolValue() {
            return false;
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            return (rhs == null || rhs is ValNull ? 1 : 0);
        }
        
    }

}