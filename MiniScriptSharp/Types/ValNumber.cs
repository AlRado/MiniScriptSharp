using System.Globalization;
using MiniScriptSharp.Tac;

namespace MiniScriptSharp.Types {

    /// <summary>
    /// ValNumber represents a numeric (double-precision floating point) value in MiniScript.
    /// Since we also use numbers to represent boolean values, ValNumber does that job too.
    /// </summary>
    public class ValNumber : Value {
        
        public double Value;

        public ValNumber(double value) {
            Value = value;
        }

        public override string ToString(Machine vm) {
            // Convert to a string in the standard MiniScript way.
            if (Value % 1.0 == 0.0) {
                // integer values as integers
                return Value.ToString("0", CultureInfo.InvariantCulture);
            }
            else if (Value > 1E10 || Value < -1E10 || (Value < 1E-6 && Value > -1E-6)) {
                // very large/small numbers in exponential form
                var s = Value.ToString("E6", CultureInfo.InvariantCulture);
                s = s.Replace("E-00", "E-0");
                return s;
            }
            else {
                // all others in decimal form, with 1-6 digits past the decimal point
                return Value.ToString("0.0#####", CultureInfo.InvariantCulture);
            }
        }

        public override int IntValue() {
            return (int) Value;
        }

        public override double DoubleValue() {
            return Value;
        }

        public override bool BoolValue() {
            // Any nonzero value is considered true, when treated as a bool.
            return Value != 0;
        }

        public override bool IsA(Value type, Machine vm) {
            return type == vm.NumberType;
        }

        public override int Hash(int recursionDepth = 16) {
            return Value.GetHashCode();
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            return rhs is ValNumber number && number.Value == Value ? 1 : 0;
        }
        
        /// <summary>
        /// Handy accessor to a shared "zero" (0) value.
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static readonly ValNumber Zero = new ValNumber(0);

        /// <summary>
        /// Handy accessor to a shared "one" (1) value.
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static readonly ValNumber One = new ValNumber(1);


        /// <summary>
        /// Convenience method to get a reference to zero or one, according
        /// to the given boolean.  (Note that this only covers Boolean
        /// truth values; MiniScript also allows fuzzy truth values, like
        /// 0.483, but obviously this method won't help with that.)
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        /// <param name="truthValue">whether to return 1 (true) or 0 (false)</param>
        /// <returns>ValNumber.one or ValNumber.zero</returns>
        public static ValNumber Truth(bool truthValue) {
            return truthValue ? One : Zero;
        }

        /// <summary>
        /// Basically this just makes a ValNumber out of a double,
        /// BUT it is optimized for the case where the given value
        ///	is either 0 or 1 (as is usually the case with truth tests).
        /// </summary>
        public static ValNumber Truth(double truthValue) {
            return truthValue switch {
                0.0 => Zero,
                1.0 => One,
                _ => new ValNumber(truthValue)
            };
        }

    }

}