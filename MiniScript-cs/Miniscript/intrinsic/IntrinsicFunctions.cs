using System;

namespace Miniscript.intrinsic {

    public class IntrinsicFunctions {
        
        // abs
        //	Returns the absolute value of the given number.
        // x (number, default 0): number to take the absolute value of.
        // Example: abs(-42)		returns 42
        public double Abs(double x = 0) {
            return Math.Abs(x);
        }
        
        // acos
        //	Returns the inverse cosine, that is, the angle 
        //	(in radians) whose cosine is the given value.
        // x (number, default 0): cosine of the angle to find.
        // Returns: angle, in radians, whose cosine is x.
        // Example: acos(0) 		returns 1.570796
        public double Acos(double x = 0) {
            return Math.Acos(x);
        }
        
        // asin
        //	Returns the inverse sine, that is, the angle
        //	(in radians) whose sine is the given value.
        // x (number, default 0): cosine of the angle to find.
        // Returns: angle, in radians, whose cosine is x.
        // Example: asin(1) return 1.570796
        public double Asin(double x = 0) {
            return Math.Asin(x);
        }
        
        // atan
        //	Returns the arctangent of a value or ratio, that is, the
        //	angle (in radians) whose tangent is y/x.  This will return
        //	an angle in the correct quadrant, taking into account the
        //	sign of both arguments.  The second argument is optional,
        //	and if omitted, this function is equivalent to the traditional
        //	one-parameter atan function.  Note that the parameters are
        //	in y,x order.
        // y (number, default 0): height of the side opposite the angle
        // x (number, default 1): length of the side adjacent the angle
        // Returns: angle, in radians, whose tangent is y/x
        // Example: atan(1, -1)		returns 2.356194
        public double Atan(double y = 0, double x = 1) {
            return x == 1.0 ? Math.Atan(y) : Math.Atan2(y, x);
        }
        
        // bitAnd
        //	Treats its arguments as integers, and computes the bitwise
        //	`and`: each bit in the result is set only if the corresponding
        //	bit is set in both arguments.
        // i (number, default 0): first integer argument
        // j (number, default 0): second integer argument
        // Returns: bitwise `and` of i and j
        // Example: bitAnd(14, 7)		returns 6
        // See also: bitOr; bitXor
        public int BitAnd(int i, int j) {
            return i & j;
        }

    }

}