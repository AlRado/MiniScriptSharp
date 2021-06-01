using System.Globalization;
using MiniScriptSharp.Tac;

namespace MiniScriptSharp.Types {

    public class ValTemp : Value {

        public int TempNum;

        public ValTemp(int tempNum) {
            TempNum = tempNum;
        }

        public override Value Val(Context context) {
            return context.GetTemp(TempNum);
        }

        public override Value Val(Context context, out ValMap valueFoundIn) {
            valueFoundIn = null;
            return context.GetTemp(TempNum);
        }

        public override string ToString(Machine vm) {
            return $"_{TempNum.ToString(CultureInfo.InvariantCulture)}";
        }

        public override int Hash(int recursionDepth = 16) {
            return TempNum.GetHashCode();
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            return rhs is ValTemp temp && temp.TempNum == TempNum ? 1 : 0;
        }

    }

}