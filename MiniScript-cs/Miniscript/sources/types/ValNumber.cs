using System.Globalization;

namespace Miniscript.sources.types {

    /// <summary>
    /// ValNumber represents a numeric (double-precision floating point) value in Minisript.
    /// Since we also use numbers to represent boolean values, ValNumber does that job too.
    /// </summary>
    public class ValNumber : Value {

        public double value;

        public ValNumber(double value) {
            this.value = value;
        }

        public override string ToString(TAC.Machine vm) {
            // Convert to a string in the standard Minisript way.
            if (value % 1.0 == 0.0) {
                // integer values as integers
                return value.ToString("0", CultureInfo.InvariantCulture);
            }
            else if (value > 1E10 || value < -1E10 || (value < 1E-6 && value > -1E-6)) {
                // very large/small numbers in exponential form
                string s = value.ToString("E6", CultureInfo.InvariantCulture);
                s = s.Replace("E-00", "E-0");
                return s;
            }
            else {
                // all others in decimal form, with 1-6 digits past the decimal point
                return value.ToString("0.0#####", CultureInfo.InvariantCulture);
            }
        }

        public override int IntValue() {
            return (int) value;
        }

        public override double DoubleValue() {
            return value;
        }

        public override bool BoolValue() {
            // Any nonzero value is considered true, when treated as a bool.
            return value != 0;
        }

        public override bool IsA(Value type, TAC.Machine vm) {
            return type == vm.numberType;
        }

        public override int Hash(int recursionDepth = 16) {
            return value.GetHashCode();
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            return rhs is ValNumber && ((ValNumber) rhs).value == value ? 1 : 0;
        }

        static ValNumber _zero = new ValNumber(0), _one = new ValNumber(1);

        /// <summary>
        /// Handy accessor to a shared "zero" (0) value.
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValNumber zero
        {
            get { return _zero; }
        }

        /// <summary>
        /// Handy accessor to a shared "one" (1) value.
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValNumber one
        {
            get { return _one; }
        }

        /// <summary>
        /// Convenience method to get a reference to zero or one, according
        /// to the given boolean.  (Note that this only covers Boolean
        /// truth values; Minisript also allows fuzzy truth values, like
        /// 0.483, but obviously this method won't help with that.)
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        /// <param name="truthValue">whether to return 1 (true) or 0 (false)</param>
        /// <returns>ValNumber.one or ValNumber.zero</returns>
        public static ValNumber Truth(bool truthValue) {
            return truthValue ? one : zero;
        }

        /// <summary>
        /// Basically this just makes a ValNumber out of a double,
        /// BUT it is optimized for the case where the given value
        ///	is either 0 or 1 (as is usually the case with truth tests).
        /// </summary>
        public static ValNumber Truth(double truthValue) {
            if (truthValue == 0.0) return zero;
            if (truthValue == 1.0) return one;
            return new ValNumber(truthValue);
        }

    }

}