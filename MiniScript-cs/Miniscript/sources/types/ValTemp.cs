using System.Globalization;

namespace Miniscript.sources.types {

    public class ValTemp : Value {

        public int tempNum;

        public ValTemp(int tempNum) {
            this.tempNum = tempNum;
        }

        public override Value Val(TAC.Context context) {
            return context.GetTemp(tempNum);
        }

        public override Value Val(TAC.Context context, out ValMap valueFoundIn) {
            valueFoundIn = null;
            return context.GetTemp(tempNum);
        }

        public override string ToString(TAC.Machine vm) {
            return "_" + tempNum.ToString(CultureInfo.InvariantCulture);
        }

        public override int Hash(int recursionDepth = 16) {
            return tempNum.GetHashCode();
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            return rhs is ValTemp && ((ValTemp) rhs).tempNum == tempNum ? 1 : 0;
        }

    }

}