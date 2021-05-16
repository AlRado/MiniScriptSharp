using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Miniscript.tac;
using Miniscript.types;

namespace Miniscript.intrinsic {

    public class IntrinsicFunctions {
        
        private static Random random = new Random();
        
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
        public int Code(Value self) {
            var codepoint = 0;
            if (self != null) codepoint = char.ConvertToUtf32(self.ToString(), 0);
            return codepoint;
        }
        
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
        
        // self.join
        //	Join the elements of a list together to form a string.
        // self (list): list to join
        // delimiter (string, default " "): string to insert between each pair of elements
        // Returns: string built by joining elements of self with delimiter
        // Example: [2,4,8].join("-")		returns "2-4-8"
        // See also: split
        public string Join(Value self, string delimiter = " ") {
            if (!(self is ValList valList)) return self?.ToString();
            
            var list = new List<string>(valList.Values.Count);
            list.AddRange(valList.Values.Select(t => t?.ToString()));
            return string.Join(delimiter, list.ToArray());
        }
        
        // self.len
        //	Return the number of characters in a string, elements in
        //	a list, or key/value pairs in a map.
        //	May be called with function syntax or dot syntax.
        // self (list, string, or map): object to get the length of
        // Returns: length (number of elements) in self
        // Example: "hello".len		returns 5
        public int Len(Value self) {
            return self switch {
                ValList valList => valList.Values.Count,
                ValString valString => valString.Value.Length,
                ValMap map => map.Count,
                _ => 0
            };
        }
        
        // log(x, base)
        //	Returns the logarithm (with the given) of the given number,
        //	that is, the number y such that base^y = x.
        // x (number): number to take the log of
        // base (number, default 10): logarithm base
        // Returns: a number that, when base is raised to it, produces x
        // Example: log(1000)		returns 3 (because 10^3 == 1000)
        public double Log(double x = 0, double @base = 10) {
            double result;
            if (Math.Abs(@base - 2.718282) < 0.000001) result = Math.Log(x);
            else result = Math.Log(x) / Math.Log(@base);
        
            return result;
        }
        
        // lower
        //	Return a lower-case version of a string.
        //	May be called with function syntax or dot syntax.
        // self (string): string to lower-case
        // Returns: string with all capital letters converted to lowercase
        // Example: "Mo Spam".lower		returns "mo spam"
        // See also: upper
        public string Lower(Value self) {
            if (!(self is ValString valString)) return self?.ToString();
            
            return valString.Value.ToLower();
        }
        
        // pi
        //	Returns the universal constant π, that is, the ratio of
        //	a circle's circumference to its diameter.
        // Example: pi		returns 3.141593
        public double Pi() {
            return Math.PI;
        }
        
        // round
        //	Rounds a number to the specified number of decimal places.  If given
        //	a negative number for decimalPlaces, then rounds to a power of 10:
        //	-1 rounds to the nearest 10, -2 rounds to the nearest 100, etc.
        // x (number): number to round
        // decimalPlaces (number, defaults to 0): how many places past the decimal point to round to
        // Example: round(pi, 2)		returns 3.14
        // Example: round(12345, -3)		returns 12000
        public double Round(double x, int decimalPlaces) {
            if (decimalPlaces >= 0) {
                x = Math.Round(x, decimalPlaces);
            } else {
                var pow10 = Math.Pow(10, -decimalPlaces);
                x = Math.Round(x / pow10) * pow10;
            }

            return x;
        }
        
        // rnd
        //	Generates a pseudorandom number between 0 and 1 (including 0 but
        //	not including 1).  If given a seed, then the generator is reset
        //	with that seed value, allowing you to create repeatable sequences
        //	of random numbers.  If you never specify a seed, then it is
        //	initialized automatically, generating a unique sequence on each run.
        // seed (number, optional): if given, reset the sequence with this value
        // Returns: pseudorandom number in the range [0,1)
        public double Rnd(int? seed) {
            if (seed != null) random = new Random((int)seed);
            return random.NextDouble();
        }
        
        // str
        //	Convert any value to a string.
        // x (any): value to convert
        // Returns: string representation of the given value
        // Example: str(42)		returns "42"
        // See also: val
        public string Str(Value x) {
            return x.ToString();
        }

        // string type
        //	Returns a map that represents the string datatype in
        //	Minisript's core type system.  This can be used with `isa`
        //	to check whether a variable refers to a string.  You can also
        //	assign new methods here to make them available to all strings.
        // Example: "Hello" isa string		returns 1
        // See also: number, list, map, funcRef
        public ValMap String() {
            return FunctionInjector.Context.Vm.StringType ??= 
                Intrinsic.StringType.EvalCopy(FunctionInjector.Context.Vm.GlobalContext);
        }
        
        // shuffle
        //	Randomize the order of elements in a list, or the mappings from
        //	keys to values in a map.  This is Done in place.
        // self (list or map): object to shuffle
        // Returns: null
        public void Shuffle(Value self) {
            switch (self) {
                case ValList valList: {
                    var list = valList.Values;
                    // We'll do a Fisher-Yates shuffle, i.e., swap each element
                    // with a randomly selected one.
                    for (int i=list.Count-1; i >= 1; i--) {
                        var j = random.Next(i+1);
                        var temp = list[j];
                        list[j] = list[i];
                        list[i] = temp;
                    }
                    break;
                }
                case ValMap valMap: {
                    var map = valMap.Map;
                    // Fisher-Yates again, but this time, what we're swapping
                    // is the values associated with the keys, not the keys themselves.
                    var keys = map.Keys.ToList();
                    for (int i=keys.Count-1; i >= 1; i--) {
                        var j = random.Next(i+1);
                        var keyi = keys[i];
                        var keyj = keys[j];
                        var temp = map[keyj];
                        map[keyj] = map[keyi];
                        map[keyi] = temp;
                    }
                    break;
                }
            }
        }

        // sum
        //	Returns the total of all elements in a list, or all values in a map.
        // self (list or map): object to sum
        // Returns: result of adding up all values in self
        // Example: range(3).sum		returns 6 (3 + 2 + 1 + 0)
        public double Sum(Value self) {
            double sum = 0;
            switch (self) {
                case ValList valList: {
                    sum += valList.Values.Sum(v => v.DoubleValue());
                    break;
                }
                case ValMap valMap: {
                    sum += valMap.Map.Values.Sum(v => v.DoubleValue());
                    break;
                }
            }
            return sum;
        }
        
        
        // tan
        //	Returns the tangent of the given angle (in radians).
        // radians (number): angle, in radians, to get the tangent of
        // Returns: tangent of the given angle
        // Example: tan(pi/4)		returns 1
        public double Tan(double radians) {
            return Math.Tan(radians);
        }
        
        // time
        //	Returns the number of seconds since the script started running.
        public double Time() {
            return FunctionInjector.Context.Vm.RunTime;
        }
        
        // upper
        //	Return an upper-case (all capitals) version of a string.
        //	May be called with function syntax or dot syntax.
        // self (string): string to upper-case
        // Returns: string with all lowercase letters converted to capitals
        // Example: "Mo Spam".upper		returns "MO SPAM"
        // See also: lower
        public string Upper(Value self) {
            return self is ValString str ? str.Value.ToUpper() : self?.ToString();
        }
        
        // val
        //	Return the numeric value of a given string.  (If given a number,
        //	returns it as-is; if given a list or map, returns null.)
        //	May be called with function syntax or dot syntax.
        // self (string or number): string to get the value of
        // Returns: numeric value of the given string
        // Example: "1234.56".val		returns 1234.56
        // See also: str
        public double Val(Value self) {
            return double.TryParse(self.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0; 
        }
        
        // values
        //	Returns the values of a dictionary, or the characters of a string.
        //  (Returns any other value as-is.)
        //	May be called with function syntax or dot syntax.
        // self (any): object to get the values of.
        // Example: d={1:"one", 2:"two"}; d.values		returns ["one", "two"]
        // Example: "abc".values		returns ["a", "b", "c"]
        // See also: indexes
        public Value Values(Value self) {
            switch (self) {
                case ValMap valMap: {
                    var values = new List<Value>(valMap.Map.Values);
                    return new ValList(values);
                }
                case ValString valString: {
                    var str = valString.Value;
                    var values = new List<Value>(str.Length);
                    for (int i = 0; i < str.Length; i++) {
                        values.Add(TAC.Str(str[i].ToString()));
                    }

                    return new ValList(values);
                }
                default:
                    return self;
            }
        }
        
        // version
        //	Get a map with information about the version of Minisript and
        //	the host environment that you're currently running.  This will
        //	include at least the following keys:
        //		miniscript: a string such as "1.5"
        //		buildDate: a date in yyyy-mm-dd format, like "2020-05-28"
        //		host: a number for the host major and minor version, like 0.9
        //		hostName: name of the host application, e.g. "Mini Micro"
        //		hostInfo: URL or other short info about the host app
        public ValMap Version() {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (FunctionInjector.Context.Vm.VersionMap != null) return FunctionInjector.Context.Vm.VersionMap;

            var d = new ValMap {["miniscript"] = new ValString("1.5")};

            // Getting the build date is annoyingly hard in C#.
            // This will work if the assembly.cs file uses the version format: 1.0.*
            var buildDate = new DateTime(2000, 1, 1);
            buildDate = buildDate.AddDays(version.Build);
            buildDate = buildDate.AddSeconds(version.Revision * 2);

            d["buildDate"] = new ValString(buildDate.ToString("yyyy-MM-dd"));

            d["host"] = new ValNumber(HostInfo.Version);
            d["hostName"] = new ValString(HostInfo.Name);
            d["hostInfo"] = new ValString(HostInfo.Info);

            FunctionInjector.Context.Vm.VersionMap = d;
            return FunctionInjector.Context.Vm.VersionMap;
        }
        
        // wait
        //	Pause execution of this script for some amount of time.
        // seconds (default 1.0): how many seconds to wait
        // Example: wait 2.5		pauses the script for 2.5 seconds
        // See also: time, yield
        public Result Wait(double seconds = 1) {
            var now = FunctionInjector.Context.Vm.RunTime;
            if (FunctionInjector.PartialResult == null) {
                // Just starting our wait; calculate end time and return as partial result
                return new Result(new ValNumber(now + seconds), false);
            } else {
                // Continue until current time exceeds the time in the partial result
                return now > FunctionInjector.PartialResult.ResultValue.DoubleValue() ? 
                    Result.Null : 
                    FunctionInjector.PartialResult;
            }
        }

        // yield
        //	Pause the execution of the script until the next "tick" of
        //	the host app.  In Mini Micro, for example, this waits until
        //	the next 60Hz frame.  Exact meaning may very, but generally
        //	if you're doing something in a tight loop, calling yield is
        //	polite to the host app or other scripts.
        public void Yield() {
            FunctionInjector.Context.Vm.Yielding = true;
        }

        public string AllIntrinsic() {
            return Intrinsic.GetAllIntrinsicInfo();
        }

    }

}