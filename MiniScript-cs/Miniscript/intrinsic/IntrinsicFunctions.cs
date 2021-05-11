using System;
using Miniscript.types;

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

        // bitOr
        //	Treats its arguments as integers, and computes the bitwise
        //	`or`: each bit in the result is set if the corresponding
        //	bit is set in either (or both) of the arguments.
        // i (number, default 0): first integer argument
        // j (number, default 0): second integer argument
        // Returns: bitwise `or` of i and j
        // Example: bitOr(14, 7)		returns 15
        // See also: bitAnd; bitXor
        public int BitOr(int i, int j) {
            return i | j;
        }
        
        // bitXor
        //	Treats its arguments as integers, and computes the bitwise
        //	`xor`: each bit in the result is set only if the corresponding
        //	bit is set in exactly one (not zero or both) of the arguments.
        // i (number, default 0): first integer argument
        // j (number, default 0): second integer argument
        // Returns: bitwise `and` of i and j
        // Example: bitAnd(14, 7)		returns 9
        // See also: bitAnd; bitOr
        public int BitXor(int i, int j) {
            return i ^ j;
        }
        
        // char
        //	Gets a character from its Unicode code point.
        // codePoint (number, default 65): Unicode code point of a character
        // Returns: string containing the specified character
        // Example: char(42)		returns "*"
        // See also: code
        public string Char(int codePoint = 65) {
            return char.ConvertFromUtf32(codePoint);
        }
        
        // ceil
        //	Returns the "ceiling", i.e. closest whole number 
        //	greater than or equal to the given number.
        // x (number, default 0): number to get the ceiling of
        // Returns: closest whole number not less than x
        // Example: ceil(41.2)		returns 42
        // See also: floor
        public double Ceil(double x = 0) {
            return Math.Ceiling(x);
        }
        
        // code
        //	Return the Unicode code point of the first character of
        //	the given string.  This is the inverse of `char`.
        //	May be called with function syntax or dot syntax.
        // self (string): string to get the code point of
        // Returns: Unicode code point of the first character of self
        // Example: "*".code		returns 42
        // Example: code("*")		returns 42
        
        
        // cos
        //	Returns the cosine of the given angle (in radians).
        // radians (number): angle, in radians, to get the cosine of
        // Returns: cosine of the given angle
        // Example: cos(0)		returns 1
        public double Cos(double radians = 0) {
            return Math.Cos(radians);
        }
        
        // floor
        //	Returns the "floor", i.e. closest whole number 
        //	less than or equal to the given number.
        // x (number, default 0): number to get the floor of
        // Returns: closest whole number not more than x
        // Example: floor(42.9)		returns 42
        // See also: floor
        public double Floor(double x = 0) {
            return Math.Floor(x);
        }
        
        // funcRef
        //	Returns a map that represents a function reference in
        //	Minisript's core type system.  This can be used with `isa`
        //	to check whether a variable refers to a function (but be
        //	sure to use @ to avoid invoking the function and testing
        //	the result).
        // Example: @floor isa funcRef		returns 1
        // See also: number, string, list, map
        
        
        // hash
        //	Returns an integer that is "relatively unique" to the given value.
        //	In the case of strings, the hash is case-sensitive.  In the case
        //	of a list or map, the hash combines the hash values of all elements.
        //	Note that the value returned is platform-dependent, and may vary
        //	across different Minisript implementations.
        // obj (any type): value to hash
        // Returns: integer hash of the given value
        public int Hash(Value obj) {
            return obj.Hash();
        }
        
    }

}