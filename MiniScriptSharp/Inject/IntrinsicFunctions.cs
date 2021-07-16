using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using MiniScriptSharp.Errors;
using MiniScriptSharp.Intrinsics;
using MiniScriptSharp.Tac;
using MiniScriptSharp.Types;
using static MiniScriptSharp.Constants.Consts;

namespace MiniScriptSharp.Inject {
    
    /// <summary>
    /// All standard intrinsics are defined in this class
    /// </summary>
    public class IntrinsicFunctions {
        
        private static Random random = new Random();
        
        [Description(
            "\n   Returns the absolute value of the given number." +
            "\n x (number, default 0): number to take the absolute value of." +
            "\n Example: abs(-42)		returns 42" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Abs(double x = 0) {
            return Math.Abs(x);
        }
        
        [Description(
            "\n   Returns the inverse cosine, that is, the angle" +
            "\n   (in radians) whose cosine is the given value." +
            "\n x (number, default 0): cosine of the angle to find." +
            "\n Returns: angle, in radians, whose cosine is x." +
            "\n Example: acos(0) 		returns 1.570796" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Acos(double x = 0) {
            return Math.Acos(x);
        }
        
        [Description(
            "\n   Returns the inverse sine, that is, the angle" +
            "\n   (in radians) whose sine is the given value." +
            "\n x (number, default 0): cosine of the angle to find." +
            "\n Returns: angle, in radians, whose cosine is x." +
            "\n Example: asin(1) return 1.570796" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Asin(double x = 0) {
            return Math.Asin(x);
        }
        
        [Description(
            "\n   Returns the arctangent of a value or ratio, that is, the" +
            "\n   angle (in radians) whose tangent is y/x.  This will return" +
            "\n   an angle in the correct quadrant, taking into account the" +
            "\n   sign of both arguments.  The second argument is optional," +
            "\n   and if omitted, this function is equivalent to the traditional" +
            "\n   one-parameter atan function.  Note that the parameters are" +
            "\n   in y,x order." +
            "\n y (number, default 0): height of the side opposite the angle" +
            "\n x (number, default 1): length of the side adjacent the angle" +
            "\n Returns: angle, in radians, whose tangent is y/x" +
            "\n Example: atan(1, -1)		returns 2.356194" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Atan(double y = 0, double x = 1) {
            return x == 1.0 ? Math.Atan(y) : Math.Atan2(y, x);
        }
        
        [Description(
            "\n   Treats its arguments as integers, and computes the bitwise" +
            "\n   `and`: each bit in the result is set only if the corresponding" +
            "\n   bit is set in both arguments." +
            "\n i (number, default 0): first integer argument" +
            "\n j (number, default 0): second integer argument" +
            "\n Returns: bitwise `and` of i and j" +
            "\n Example: bitAnd(14, 7)		returns 6" +
            "\n See also: bitOr; bitXor" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public int BitAnd(int i, int j) {
            return i & j;
        }
        
        [Description(
            "\n   Treats its arguments as integers, and computes the bitwise" +
            "\n   `or`: each bit in the result is set if the corresponding" +
            "\n   bit is set in either (or both) of the arguments." +
            "\n i (number, default 0): first integer argument" +
            "\n j (number, default 0): second integer argument" +
            "\n Returns: bitwise `or` of i and j" +
            "\n Example: bitOr(14, 7)		returns 15" +
            "\n See also: bitAnd; bitXor" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public int BitOr(int i, int j) {
            return i | j;
        }
        
        [Description(
            "\n   Treats its arguments as integers, and computes the bitwise" +
            "\n   `xor`: each bit in the result is set only if the corresponding" +
            "\n   bit is set in exactly one (not zero or both) of the arguments." +
            "\n i (number, default 0): first integer argument" +
            "\n j (number, default 0): second integer argument" +
            "\n Returns: bitwise `and` of i and j" +
            "\n Example: bitAnd(14, 7)		returns 9" +
            "\n See also: bitAnd; bitOr" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public int BitXor(int i, int j) {
            return i ^ j;
        }
        
        [Description(
            "\n   Gets a character from its Unicode code point." +
            "\n   codePoint (number, default 65): Unicode code point of a character" +
            "\n Returns: string containing the specified character" +
            "\n Example: char(42)		returns \"*\"" +
            "\n See also: code" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public string Char(int codePoint = 65) {
            return char.ConvertFromUtf32(codePoint);
        }
        
        [Description(
            "\n   Returns the \"ceiling\", i.e. closest whole number" +
            "\n   greater than or equal to the given number." +
            "\n x (number, default 0): number to get the ceiling of" +
            "\n Returns: closest whole number not less than x" +
            "\n Example: ceil(41.2)		returns 42" +
            "\n See also: floor" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Ceil(double x = 0) {
            return Math.Ceiling(x);
        }
        
        [Description(
            "\n   Returns a map that represents a function reference in" +
            "\n   MiniScript's core type system.  This can be used with `isa`" +
            "\n   to check whether a variable refers to a function (but be" +
            "\n   sure to use @ to avoid invoking the function and testing" +
            "\n   the result)." +
            "\n Example: @floor isa funcRef		returns 1" +
            "\n See also: number, string, list, map" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public Result FuncRef() {
            FunctionInjector.Context.Vm.FunctionType ??= Intrinsic.FunctionType.EvalCopy(FunctionInjector.Context.Vm.GlobalContext);
            return new Result(FunctionInjector.Context.Vm.FunctionType);
        }
        
        [Description(
            "\n   Return the Unicode code point of the first character of" +
            "\n   the given string.  This is the inverse of `char`." +
            "\n   May be called with function syntax or dot syntax." +
            "\n self (string): string to get the code point of" +
            "\n Returns: Unicode code point of the first character of self" +
            "\n Example: \"*\".code		returns 42" +
            "\n Example: code(\"*\")		returns 42" +
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [Category(INTRINSIC)]
        public int Code(Value self) {
            var codepoint = 0;
            if (self != null) codepoint = char.ConvertToUtf32(self.ToString(), 0);
            return codepoint;
        }
        
        [Description(
            "\n   Returns the cosine of the given angle (in radians)." +
            "\n radians (number): angle, in radians, to get the cosine of" +
            "\n Returns: cosine of the given angle" +
            "\n Example: cos(0)		returns 1" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Cos(double radians = 0) {
            return Math.Cos(radians);
        }

        [Description(
            "\n   Returns the \"floor\", i.e. closest whole number" +
            "\n   less than or equal to the given number." +
            "\n x (number, default 0): number to get the floor of" +
            "\n Returns: closest whole number not more than x" +
            "\n Example: floor(42.9)		returns 42" +
            "\n See also: floor" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Floor(double x = 0) {
            return Math.Floor(x);
        }

        [Description(
            "\n   Returns an integer that is \"relatively unique\" to the given value." +
            "\n   In the case of strings, the hash is case-sensitive.  In the case" +
            "\n   of a list or map, the hash combines the hash values of all elements." +
            "\n   Note that the value returned is platform-dependent, and may vary" +
            "\n   across different MiniScript implementations." +
            "\n obj (any type): value to hash" +
            "\n Returns: integer hash of the given value" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public int Hash(Value obj) {
            return obj.Hash();
        }

        [Description(
            "\n   Return whether the given index is valid for this object, that is," +
            "\n   whether it could be used with square brackets to get some value" +
            "\n   from self.  When self is a list or string, the result is true for" +
            "\n   integers from -(length of string) to (length of string-1).  When" +
            "\n   self is a map, it is true for any key (index) in the map.  If" +
            "\n   called on a number, this method throws a runtime exception." +
            "\n self (string, list, or map): object to check for an index on" +
            "\n index (any): value to consider as a possible index" +
            "\n Returns: 1 if self[index] would be valid; 0 otherwise" +
            "\n Example: \"foo\".hasIndex(2)		returns 1" +
            "\n Example: \"foo\".hasIndex(3)		returns 0" +
            "\n See also: indexes" +
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        [Category(INTRINSIC)]
        public Result HasIndex(Value self, Value index) {
            switch (self) {
                case ValList _ when !(index is ValNumber):
                    return Result.False;	// #3
                case ValList valList: {
                    var list = valList.Values;
                    var i = index.IntValue();
                    return new Result(ValNumber.Truth(i >= -list.Count && i < list.Count));
                }
                case ValString valString: {
                    var str = valString.Value;
                    var i = index.IntValue();
                    return new Result(ValNumber.Truth(i >= -str.Length && i < str.Length));
                }
                case ValMap valMap: {
                    return new Result(ValNumber.Truth(valMap.ContainsKey(index)));
                }
                default:
                    return Result.Null;
            }
        }
        
        [Description(
            "\n   Returns the keys of a dictionary, or the non-negative indexes" +
            "\n   for a string or list." +
            "\n self (string, list, or map): object to get the indexes of" +
            "\n Returns: a list of valid indexes for self" +
            "\n Example: \"foo\".indexes		returns [0, 1, 2]" +
            "\n See also: hasIndex" +
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        [Category(INTRINSIC)]
        public Result Indexes(Value self) {
            switch (self) {
                case ValMap valMap: {
                    var map = valMap;
                    var keys = new List<Value>(map.Map.Keys);
                    for (int i = 0; i < keys.Count; i++) if (keys[i] is ValNull) keys[i] = null;
                    return new Result(new ValList(keys));
                }
                case ValString valString: {
                    var str = valString.Value;
                    var indexes = new List<Value>(str.Length);
                    for (int i = 0; i < str.Length; i++) {
                        indexes.Add(TAC.Num(i));
                    }
                    return new Result(new ValList(indexes));
                }
                case ValList valList: {
                    var list = valList.Values;
                    var indexes = new List<Value>(list.Count);
                    for (int i = 0; i < list.Count; i++) {
                        indexes.Add(TAC.Num(i));
                    }
                    return new Result(new ValList(indexes));
                }
                default:
                    return Result.Null;
            }
        }
        
        [Description(
            "\n   Returns index or key of the given value, or if not found,		returns null." +
            "\n self (string, list, or map): object to search" +
            "\n value (any): value to search for" +
            "\n after (any, optional): if given, starts the search after this index" +
            "\n Returns: first index (after `after`) such that self[index] == value, or null" +
            "\n Example: \"Hello World\".indexOf(\"o\")		returns 4" +
            "\n Example: \"Hello World\".indexOf(\"o\", 4)		returns 7" +
            "\n Example: \"Hello World\".indexOf(\"o\", 7)		returns null" +
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        [Category(INTRINSIC)]
        public Result IndexOf(Value self, Value value, Value after) {
            switch (self) {
                case ValList valList: {
                    var list = valList.Values;
                    int idx;
                    if (after == null) idx = list.FindIndex(x => 
                        x == null ? value == null : x.Equality(value) == 1);
                    else {
                        var afterIdx = after.IntValue();
                        if (afterIdx < -1) afterIdx += list.Count;
                        if (afterIdx < -1 || afterIdx >= list.Count-1) return Result.Null;
                        idx = list.FindIndex(afterIdx + 1, x => 
                            x == null ? value == null : x.Equality(value) == 1);
                    }
                    if (idx >= 0) return new Result(idx);
                    break;
                }
                case ValString valString: {
                    var str = valString.Value;
                    if (value == null) return Result.Null;
                    var s = value.ToString();
                    int idx;
                    if (after == null) idx = str.IndexOf(s);
                    else {
                        int afterIdx = after.IntValue();
                        if (afterIdx < -1) afterIdx += str.Length;
                        if (afterIdx < -1 || afterIdx >= str.Length-1) return Result.Null;
                        idx = str.IndexOf(s, afterIdx + 1);
                    }
                    if (idx >= 0) return new Result(idx);
                    break;
                }
                case ValMap valMap: {
                    bool sawAfter = (after == null);
                    foreach (Value k in valMap.Map.Keys) {
                        if (!sawAfter) {
                            if (k.Equality(after) == 1) sawAfter = true;
                        } else {
                            if (valMap.Map[k].Equality(value) == 1) return new Result(k);
                        }
                    }

                    break;
                }
            }
            return Result.Null;
        }

        [Description(
            "\n   Insert a new element into a string or list.  In the case of a list," +
            "\n   the list is both modified in place and returned.  Strings are immutable," +
            "\n   so in that case the original string is unchanged, but a new string is" +
            "\n   returned with the value inserted." +
            "\n self (string or list): sequence to insert into" +
            "\n index (number): position at which to insert the new item" +
            "\n value (any): element to insert at the specified index" +
            "\n Returns: modified list, new string" +
            "\n Example: \"Hello\".insert(2, 42)		returns \"He42llo\"" +
            "\n See also: remove" +
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [Category(INTRINSIC)]
        public Result Insert(Value self, Value index, Value value) {
            if (index == null) throw new RuntimeException("insert: index argument required");
            if (!(index is ValNumber)) throw new RuntimeException("insert: number required for index argument");
            var idx = index.IntValue();
            switch (self) {
                case ValList valList: {
                    var list = valList.Values;
                    if (idx < 0) idx += list.Count + 1;	// +1 because we are inserting AND counting from the end.
                    Check.Range(idx, 0, list.Count);	// and allowing all the way up to .Count here, because insert.
                    list.Insert(idx, value);
                    return new Result(valList);
                }
                case ValString _: {
                    var s = self.ToString();
                    if (idx < 0) idx += s.Length + 1;
                    Check.Range(idx, 0, s.Length);
                    s = s.Substring(0, idx) + value.ToString() + s.Substring(idx);
                    return new Result(s);
                }
                default:
                    throw new RuntimeException("insert called on invalid type");
            }
        }

        [Description(
            "\n   Join the elements of a list together to form a string." +
            "\n self (list): list to join" +
            "\n delimiter (string, default \" \"): string to insert between each pair of elements" +
            "\n Returns: string built by joining elements of self with delimiter" +
            "\n Example: [2,4,8].join(\"-\")		returns \"2-4-8\"" +
            "\n See also: split" +
            "\n"
        )]
        [MethodOf(typeof(ValList))]
        [Category(INTRINSIC)]
        public string Join(Value self, string delimiter = " ") {
            if (!(self is ValList valList)) return self?.ToString();
            
            var list = new List<string>(valList.Values.Count);
            list.AddRange(valList.Values.Select(t => t?.ToString()));
            return string.Join(delimiter, list.ToArray());
        }

        [Description(
            "\n   Return the number of characters in a string, elements in" +
            "\n   a list, or key/value pairs in a map." +
            "\n   May be called with function syntax or dot syntax." +
            "\n self (list, string, or map): object to get the length of" +
            "\n Returns: length (number of elements) in self" +
            "\n Example: \"hello\".len		returns 5" +
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        [Category(INTRINSIC)]
        public int Len(Value self) {
            return self switch {
                ValList valList => valList.Values.Count,
                ValString valString => valString.Value.Length,
                ValMap map => map.Count,
                _ => 0
            };
        }
        
        [Description(
            "\n   Returns a map that represents the list datatype in" +
            "\n   MiniScript's core type system.  This can be used with `isa`" +
            "\n   to check whether a variable refers to a list.  You can also" +
            "\n   assign new methods here to make them available to all lists." +
            "\n Example: [1, 2, 3] isa list		returns 1" +
            "\n See also: number, string, map, funcRef" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public Result List() {
            FunctionInjector.Context.Vm.ListType ??= Intrinsic.ListType.EvalCopy(FunctionInjector.Context.Vm.GlobalContext);
            return new Result(FunctionInjector.Context.Vm.ListType);
        }

        [Description(
            "\n   Returns the logarithm (with the given) of the given number," +
            "\n   that is, the number y such that base^y = x." +
            "\n x (number): number to take the log of" +
            "\n base (number, default 10): logarithm base" +
            "\n Returns: a number that, when base is raised to it, produces x" +
            "\n Example: log(1000)		returns 3 (because 10^3 == 1000)" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Log(double x = 0, double @base = 10) {
            double result;
            if (Math.Abs(@base - 2.718282) < 0.000001) result = Math.Log(x);
            else result = Math.Log(x) / Math.Log(@base);
        
            return result;
        }
        
        [Description(
            "\n   Return a lower-case version of a string." +
            "\n   May be called with function syntax or dot syntax." +
            "\n self (string): string to lower-case" +
            "\n Returns: string with all capital letters converted to lowercase" +
            "\n Example: \"Mo Spam\".lower		returns \"mo spam\"" +
            "\n See also: upper" +
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [Category(INTRINSIC)]
        public string Lower(Value self) {
            if (!(self is ValString valString)) return self?.ToString();
            
            return valString.Value.ToLower();
        }
        
        [Description(
            "\n   Returns a map that represents the map datatype in" +
            "\n   MiniScript's core type system.  This can be used with `isa`" +
            "\n   to check whether a variable refers to a map.  You can also" +
            "\n   assign new methods here to make them available to all maps." +
            "\n Example: {1:\"one\"} isa map		returns 1" +
            "\n See also: number, string, list, funcRef" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public Result Map() {
            FunctionInjector.Context.Vm.MapType ??= Intrinsic.MapType.EvalCopy(FunctionInjector.Context.Vm.GlobalContext); 
            return new Result(FunctionInjector.Context.Vm.MapType);
        }

        [Description(
            "\n   Returns a map that represents the number datatype in" +
            "\n   MiniScript's core type system.  This can be used with `isa`" +
            "\n   to check whether a variable refers to a number.  You can also" +
            "\n   assign new methods here to make them available to all maps" +
            "\n   (though because of a limitation in MiniScript's parser, such" +
            "\n   methods do not work on numeric literals)." +
            "\n Example: 42 isa number		returns 1" +
            "\n See also: string, list, map, funcRef" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public Result Number() {
            FunctionInjector.Context.Vm.NumberType ??= Intrinsic.NumberType.EvalCopy(FunctionInjector.Context.Vm.GlobalContext);
            return new Result(FunctionInjector.Context.Vm.NumberType);
        }

        [Description(
            "\n   Returns the universal constant π, that is, the ratio of" +
            "\n   a circle's circumference to its diameter." +
            "\n Example: pi		returns 3.141593" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Pi() {
            return Math.PI;
        }
        
        [Description(
            "\n   Display the given value on the default output stream.  The" +
            "\n   exact effect may vary with the environment.  In most cases, the" +
            "\n   given string will be followed by the standard line delimiter." +
            "\n s (any): value to print (converted to a string as needed)" +
            "\n Returns: null" +
            "\n Example: print 6*7" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public void Print(string s) {
            FunctionInjector.Context.Vm.StandardOutput(s ?? "null");
        }

        [Description(
            "\n   Removes and	returns the last item in a list, or an arbitrary" +
            "\n   key of a map.  If the list or map is empty (or if called on" +
            "\n   any other data type), returns null." +
            "\n   May be called with function syntax or dot syntax." +
            "\n self (list or map): object to remove an element from the end of" +
            "\n Returns: value removed, or null" +
            "\n Example: [1, 2, 3].pop		returns (and removes) 3" +
            "\n See also: pull; push; remove" +
            "\n"
        )]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        [Category(INTRINSIC)]
        public Result Pop(Value self) {
            switch (self) {
                case ValList valList: {
                    var list = valList.Values;
                    if (list.Count < 1) return Result.Null;
                    var result = list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    return new Result(result);
                }
                case ValMap valMap: {
                    if (valMap.Map.Count < 1) return Result.Null;
                    var result = valMap.Map.Keys.First();
                    valMap.Map.Remove(result);
                    return new Result(result);
                }
                default:
                    return Result.Null;
            }
        }
        
        [Description(
            "\n   Removes and	returns the first item in a list, or an arbitrary" +
            "\n   key of a map.  If the list or map is empty (or if called on" +
            "\n   any other data type), returns null." +
            "\n   May be called with function syntax or dot syntax." +
            "\n self (list or map): object to remove an element from the end of" +
            "\n Returns: value removed, or null" +
            "\n Example: [1, 2, 3].pull		returns (and removes) 1" +
            "\n See also: pop; push; remove" +
            "\n"
        )]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        [Category(INTRINSIC)]
        public Result Pull(Value self) {
            switch (self) {
                case ValList valList: {
                    var list = valList.Values;
                    if (list.Count < 1) return Result.Null;
                    var result = list[0];
                    list.RemoveAt(0);
                    return new Result(result);
                }
                case ValMap valMap: {
                    if (valMap.Map.Count < 1) return Result.Null;
                    var result = valMap.Map.Keys.First();
                    valMap.Map.Remove(result);
                    return new Result(result);
                }
                default:
                    return Result.Null;
            }
        }
        
        [Description(
            "\n   Appends an item to the end of a list, or inserts it into a map" +
            "\n   as a key with a value of 1." +
            "\n   May be called with function syntax or dot syntax." +
            "\n self (list or map): object to append an element to" +
            "\n Returns: self" +
            "\n See also: pop, pull, insert" +
            "\n"
        )]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        [Category(INTRINSIC)]
        public Result Push(Value self, Value value) {
            switch (self) {
                case ValList valList: {
                    var list = valList.Values;
                    list.Add(value);
                    return new Result(valList);
                }
                case ValMap valMap: {
                    valMap.Map[value] = ValNumber.One;
                    return new Result(valMap);
                }
                default:
                    return Result.Null;
            }
        }

        [Description(
            "\n   Return a list containing a series of numbers within a range." +
            "\n from (number, default 0): first number to include in the list" +
            "\n to (number, default 0): point at which to stop adding numbers to the list" +
            "\n step (number, optional): amount to add to the previous number on each step;" +
            "\n   defaults to 1 if to > from, or -1 if to < from" +
            "\n Example: range(50, 5, -10)		returns [50, 40, 30, 20, 10]" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public Result Range(double from, double to, int? step = null) {
            step ??= (to >= from ? 1 : -1);
            if (step == 0) throw new RuntimeException("range() error (step==0)");
            var count = (int) ((to - from) / step) + 1;
            if (count > ValList.MaxSize) throw new RuntimeException("list too large");
            List<Value> values;
            try {
                values = new List<Value>(count);
                for (var v = from; step > 0 ? (v <= to) : (v >= to); v += (double)step) {
                    values.Add(TAC.Num(v));
                }
            } catch (SystemException e) {
                // uh-oh... probably out-of-memory exception; clean up and bail out
                throw (new LimitExceededException("range() error", e));
            }

            return new Result(new ValList(values));
        }
        
        [Description(
            "\n   Removes part of a list, map, or string.  Exact behavior depends on" +
            "\n   the data type of self:" +
            "\n     list: removes one element by its index; the list is mutated in place;" +
            "\n       returns null, and throws an error if the given index out of range" +
            "\n     map: removes one key/value pair by key; the map is mutated in place;" +
            "\n       returns 1 if key was found, 0 otherwise" +
            "\n     string:	returns a new string with the first occurrence of k removed" +
            "\n   May be called with function syntax or dot syntax." +
            "\n self (list, map, or string): object to remove something from" +
            "\n k (any): index or substring to remove" +
            "\n Returns: (see above)" +
            "\n Example: a=[\"a\",\"b\",\"c\"]; a.remove 1		leaves a == [\"a\", \"c\"]" +
            "\n Example: d={\"ichi\":\"one\"}; d.remove \"ni\"		returns 0" +
            "\n Example: \"Spam\".remove(\"S\")		returns \"pam\"" +
            "\n See also: indexOf" +
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        [Category(INTRINSIC)]
        public Result Remove(Value self, Value k) {
            switch (self) {
                case ValMap valMap: {
                    k ??= ValNull.Instance;
                    if (!valMap.Map.ContainsKey(k)) return Result.False;
                    valMap.Map.Remove(k);
                    return Result.True;
                }
                case ValList _ when k == null:
                    throw new RuntimeException("argument to 'remove' must not be null");
                case ValList valList: {
                    var idx = k.IntValue();
                    if (idx < 0) idx += valList.Values.Count;
                    Check.Range(idx, 0, valList.Values.Count-1);
                    valList.Values.RemoveAt(idx);
                    return Result.Null;
                }
                case ValString _ when k == null:
                    throw new RuntimeException("argument to 'remove' must not be null");
                case ValString valString: {
                    var substr = k.ToString();
                    var foundPos = valString.Value.IndexOf(substr, StringComparison.Ordinal);
                    return foundPos < 0 ? new Result(valString) : new Result(valString.Value.Remove(foundPos, substr.Length));
                }
                default:
                    throw new TypeException("Type Error: 'remove' requires map, list, or string");
            }
        }
        
        [Description(
            "\n   Replace all matching elements of a list or map, or substrings of a string," +
            "\n   with a new value.Lists and maps are mutated in place, and return themselves." +
            "\n   Strings are immutable, so the original string is (of course) unchanged, but" +
            "\n   a new string with the replacement is returned.  Note that with maps, it is" +
            "\n   the values that are searched for and replaced, not the keys." +
            "\n self (list, map, or string): object to replace elements of" +
            "\n oldVal (any): value or substring to replace" +
            "\n newVal (any): new value or substring to substitute where oldVal is found" +
            "\n maxCount (number, optional): if given, replace no more than this many" +
            "\n Returns: modified list or map, or new string, with replacements Done" +
            "\n Example: \"Happy Pappy\".replace(\"app\", \"ol\")		returns \"Holy Poly\"" +
            "\n Example: [1,2,3,2,5].replace(2, 42)		returns (and mutates to) [1, 42, 3, 42, 5]" +
            "\n Example: d = {1: \"one\"}; d.replace(\"one\", \"ichi\")		returns (and mutates to) {1: \"ichi\"}" +
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        [Category(INTRINSIC)]
        public Result Replace(Value self, Value oldVal, Value newVal, Value maxCountVal) {
            if (self == null) throw new RuntimeException("argument to 'replace' must not be null");
            var maxCount = -1;
            if (maxCountVal != null) {
                maxCount = maxCountVal.IntValue();
                if (maxCount < 1) return new Result(self);
            }

            var count = 0;
            switch (self) {
                case ValMap selfMap: {
                    // C# doesn't allow changing even the values while iterating
                    // over the keys.  So gather the keys to change, then change
                    // them afterwards.
                    List<Value> keysToChange = null;
                    foreach (var k in selfMap.Map.Keys.Where(k => selfMap.Map[k].Equality(oldVal) == 1)) {
                        keysToChange ??= new List<Value>();
                        keysToChange.Add(k);
                        count++;
                        if (maxCount > 0 && count == maxCount) break;
                    }

                    if (keysToChange == null) return new Result(selfMap);
                    {
                        foreach (var k in keysToChange) {
                            selfMap.Map[k] = newVal;
                        }
                    }

                    return new Result(selfMap);
                }
                case ValList selfList: {
                    var idx = -1;
                    while (true) {
                        idx = selfList.Values.FindIndex(idx + 1, x => x.Equality(oldVal) == 1);
                        if (idx < 0) break;
                        selfList.Values[idx] = newVal;
                        count++;
                        if (maxCount > 0 && count == maxCount) break;
                    }

                    return new Result(selfList);
                }
                case ValString _: {
                    var str = self.ToString();
                    var oldStr = oldVal == null ? "" : oldVal.ToString();
                    if (string.IsNullOrEmpty(oldStr)) throw new RuntimeException("replace: oldVal argument is empty");
                    var newStr = newVal == null ? "" : newVal.ToString();
                    var idx = 0;
                    while (true) {
                        idx = str.IndexOf(oldStr, idx, StringComparison.Ordinal);
                        if (idx < 0) break;
                        str = str.Substring(0, idx) + newStr + str.Substring(idx + oldStr.Length);
                        idx += newStr.Length;
                        count++;
                        if (maxCount > 0 && count == maxCount) break;
                    }

                    return new Result(str);
                }
                default:
                    throw new TypeException("Type Error: 'replace' requires map, list, or string");
            }
        }

        [Description(
            "\n   Rounds a number to the specified number of decimal places.  If given" +
            "\n   a negative number for decimalPlaces, then rounds to a power of 10:" +
            "\n   -1 rounds to the nearest 10, -2 rounds to the nearest 100, etc." +
            "\n x (number): number to round" +
            "\n decimalPlaces (number, defaults to 0): how many places past the decimal point to round to" +
            "\n Example: round(pi, 2)		returns 3.14" +
            "\n Example: round(12345, -3)		returns 12000" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Round(double x, int decimalPlaces) {
            if (decimalPlaces >= 0) {
                x = Math.Round(x, decimalPlaces);
            } else {
                var pow10 = Math.Pow(10, -decimalPlaces);
                x = Math.Round(x / pow10) * pow10;
            }

            return x;
        }

        [Description(
            "\n   Generates a pseudorandom number between 0 and 1 (including 0 but" +
            "\n   not including 1).  If given a seed, then the generator is reset" +
            "\n   with that seed value, allowing you to create repeatable sequences" +
            "\n   of random numbers.  If you never specify a seed, then it is" +
            "\n   initialized automatically, generating a unique sequence on each run." +
            "\n seed (number, optional): if given, reset the sequence with this value" +
            "\n Returns: pseudorandom number in the range [0,1)" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Rnd(int? seed) {
            if (seed != null) random = new Random((int)seed);
            return random.NextDouble();
        }

        [Description(
            "\n   Return -1 for negative numbers, 1 for positive numbers, and 0 for zero." +
            "\n x (number): number to get the sign of" +
            "\n Returns: sign of the number" +
            "\n Example: sign(-42.6)		returns -1" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Sign(double x) {
            return Math.Sign(x);
        }

        [Description(
            "\n   Returns the sine of the given angle (in radians)." +
            "\n radians (number): angle, in radians, to get the sine of" +
            "\n Returns: sine of the given angle" +
            "\n Example: sin(pi/2)		returns 1" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Sin(double radians) {
            return Math.Sin(radians);
        }

        [Description(
            "\n   Return a subset of a string or list.  This is equivalent to using" +
            "\n   the square-brackets slice operator seq[from:to], but with ordinary" +
            "\n   function syntax." +
            "\n seq (string or list): sequence to get a subsequence of" +
            "\n from (number, default 0): 0-based index to the first element to return (if negative, counts from the end)" +
            "\n to (number, optional): 0-based index of first element to *not* include in the result" +
            "\n     (if negative, count from the end; if omitted, return the rest of the sequence)" +
            "\n Returns: substring or sublist" +
            "\n Example: slice(\"Hello\", -2)		returns \"lo\"" +
            "\n Example: slice([\"a\",\"b\",\"c\",\"d\"], 1, 3)		returns [\"b\", \"c\"]" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public Result Slice(Value seq, int from, Value toVal) {
            var toIdx = 0;
            if (toVal != null) toIdx = toVal.IntValue();
            switch (seq) {
                case ValList valList: {
                    var list = valList.Values;
                    if (from < 0) from += list.Count;
                    if (from < 0) from = 0;
                    if (toVal == null) toIdx = list.Count;
                    if (toIdx < 0) toIdx += list.Count;
                    if (toIdx > list.Count) toIdx = list.Count;
                    var slice = new ValList();
                    if (from < list.Count && toIdx > from) {
                        for (int i = from; i < toIdx; i++) {
                            slice.Values.Add(list[i]);
                        }
                    }
                    return new Result(slice);
                }
                case ValString valString: {
                    var str = valString.Value;
                    if (from < 0) from += str.Length;
                    if (from < 0) from = 0;
                    if (toVal == null) toIdx = str.Length;
                    if (toIdx < 0) toIdx += str.Length;
                    if (toIdx > str.Length) toIdx = str.Length;
                    if (toIdx - from <= 0) return Result.EmptyString;
						
                    return new Result(str.Substring(from, toIdx - from));
                }
                default:
                    return Result.Null;
            }
        }
 
        [Description(
            "\n   Sorts a list in place.  With null or no argument, this sorts the" +
            "\n   list elements by their own values.  With the byKey argument, each" +
            "\n   element is indexed by that argument, and the elements are sorted" +
            "\n   by the result.  (This only works if the list elements are maps, or" +
            "\n   they are lists and byKey is an integer index.)" +
            "\n self (list): list to sort" +
            "\n byKey (optional): if given, sort each element by indexing with this key" +
            "\n ascending (optional, default true): if false, sort in descending order" +
            "\n Returns: self (which has been sorted in place)" +
            "\n Example: a = [5,3,4,1,2]; a.sort		results in a == [1, 2, 3, 4, 5]" +
            "\n See also: shuffle" +
            "\n"
        )]
        [MethodOf(typeof(ValList))]
        [Category(INTRINSIC)]
        public Result Sort(Value self, Value byKey, bool ascending = true) {
            if (!(self is ValList list) || list.Values.Count < 2) return new Result(self);

            IComparer<Value> sorter;
            if (ascending) {
                sorter = ValueSorter.Instance;
            } else {
                sorter = ValueReverseSorter.Instance;
            }

            if (byKey == null) {
                // Simple case: sort the values as themselves
                list.Values = list.Values.OrderBy((arg) => arg, sorter).ToList();
            } else {
                // Harder case: sort by a key.
                var count = list.Values.Count;
                var arr = new KeyedValue[count];
                for (int i = 0; i < count; i++) {
                    arr[i].Value = list.Values[i];
                }

                // The key for each item will be the item itself, unless it is a map, in which
                // case it's the item indexed by the given key.  (Works too for lists if our
                // index is an integer.)
                var byKeyInt = byKey.IntValue();
                for (int i = 0; i < count; i++) {
                    var item = list.Values[i];
                    switch (item) {
                        case ValMap map:
                            arr[i].SortKey = map.Lookup(byKey);
                            break;
                        case ValList valList: {
                            if (byKeyInt > -valList.Values.Count && byKeyInt < valList.Values.Count)
                                arr[i].SortKey = valList.Values[byKeyInt];
                            else arr[i].SortKey = null;
                            break;
                        }
                    }
                }

                // Now sort our list of keyed values, by key
                var sortedArr = arr.OrderBy((arg) => arg.SortKey, sorter);
                // And finally, convert that back into our list
                var idx = 0;
                foreach (var kv in sortedArr) {
                    list.Values[idx++] = kv.Value;
                }
            }

            return new Result(list);
        }

        [Description(
            "\n   Split a string into a list, by some delimiter." +
            "\n   May be called with function syntax or dot syntax." +
            "\n self (string): string to split" +
            "\n delimiter (string, default \" \"): substring to split on" +
            "\n maxCount (number, default -1): if > 0, split into no more than this many strings" +
            "\n Returns: list of substrings found by splitting on delimiter" +
            "\n Example: \"foo bar baz\".split		returns [\"foo\", \"bar\", \"baz\"]" +
            "\n Example: \"foo bar baz\".split(\"a\", 2)		returns [\"foo b\", \"r baz\"]" +
            "\n See also: join" +
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [Category(INTRINSIC)]
        public Result Split(Value self, string delimiter = " ", double maxCount = -1) {
            var selfStr = self.ToString();
            var result = new ValList();
            var pos = 0;
            while (pos < selfStr.Length) {
                int nextPos;
                if (maxCount >= 0 && result.Values.Count == maxCount - 1) nextPos = selfStr.Length;
                else if (delimiter.Length == 0) nextPos = pos+1;
                else nextPos = selfStr.IndexOf(delimiter, pos, StringComparison.InvariantCulture);
                if (nextPos < 0) nextPos = selfStr.Length;
                result.Values.Add(new ValString(selfStr.Substring(pos, nextPos - pos)));
                pos = nextPos + delimiter.Length;
                if (pos == selfStr.Length && delimiter.Length > 0) result.Values.Add(ValString.Empty);
            }

            return new Result(result);
        }
        
        [Description(
            "\n   Returns the square root of a number." +
            "\n x (number): number to get the square root of" +
            "\n Returns: square root of x" +
            "\n Example: sqrt(1764)		returns 42" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Sqrt(double x) {
            return Math.Sqrt(x);
        }
        
        [Description(
            "\n   Convert any value to a string." +
            "\n x (any): value to convert" +
            "\n Returns: string representation of the given value" +
            "\n Example: str(42)		returns \"42\"" +
            "\n See also: val" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public string Str(Value x) {
            return x.ToString();
        }

        [Description(
            "\n   Returns a map that represents the string datatype in" +
            "\n   MiniScript's core type system.  This can be used with `isa`" +
            "\n   to check whether a variable refers to a string.  You can also" +
            "\n   assign new methods here to make them available to all strings." +
            "\n Example: \"Hello\" isa string		returns 1" +
            "\n See also: number, list, map, funcRef" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public Result String() {
            FunctionInjector.Context.Vm.StringType ??= Intrinsic.StringType.EvalCopy(FunctionInjector.Context.Vm.GlobalContext);
            return new Result(FunctionInjector.Context.Vm.StringType);
        }
        
        [Description(
            "\n   Randomize the order of elements in a list, or the mappings from" +
            "\n   keys to values in a map.  This is Done in place." +
            "\n self (list or map): object to shuffle" +
            "\n Returns: null" +
            "\n"
        )]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        [Category(INTRINSIC)]
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
        
        [Description(
            "\n   Returns the total of all elements in a list, or all values in a map." +
            "\n self (list or map): object to sum" +
            "\n Returns: result of adding up all values in self" +
            "\n Example: range(3).sum		returns 6 (3 + 2 + 1 + 0)" +
            "\n"
        )]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        [Category(INTRINSIC)]
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


        [Description(
            "\n   Returns the tangent of the given angle (in radians)." +
            "\n radians (number): angle, in radians, to get the tangent of" +
            "\n Returns: tangent of the given angle" +
            "\n Example: tan(pi/4)		returns 1" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Tan(double radians) {
            return Math.Tan(radians);
        }
        
        [Description(
            "\n   Returns the number of seconds since the script started running." +
            "\n"
        )]
        [Category(INTRINSIC)]
        public double Time() {
            return FunctionInjector.Context.Vm.RunTime;
        }
        
        [Description(
            "\n   Return an upper-case (all capitals) version of a string." +
            "\n   May be called with function syntax or dot syntax." +
            "\n self (string): string to upper-case" +
            "\n Returns: string with all lowercase letters converted to capitals" +
            "\n Example: \"Mo Spam\".upper		returns \"MO SPAM\"" +
            "\n See also: lower" +
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [Category(INTRINSIC)]
        public string Upper(Value self) {
            return self is ValString str ? str.Value.ToUpper() : self?.ToString();
        }
        
        [Description(
            "\n   Return the numeric value of a given string.  (If given a number," +
            "\n   returns it as-is; if given a list or map, returns null.)" +
            "\n   May be called with function syntax or dot syntax." +
            "\n self (string or number): string to get the value of" +
            "\n Returns: numeric value of the given string" +
            "\n Example: \"1234.56\".val		returns 1234.56" +
            "\n See also: str" + 
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [Category(INTRINSIC)]
        public double Val(Value self) {
            return double.TryParse(self.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0; 
        }
        
        [Description(
            "\n   Returns the values of a dictionary, or the characters of a string." +
            "\n   (Returns any other value as-is.)" +
            "\n   May be called with function syntax or dot syntax." +
            "\n self (any): object to get the values of." +
            "\n Example: d={1:\"one\", 2:\"two\"}; d.values		returns [\"one\", \"two\"]" +
            "\n Example: \"abc\".values		returns [\"a\", \"b\", \"c\"]" +
            "\n See also: indexes" +
            "\n"
        )]
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        [Category(INTRINSIC)]
        public Result Values(Value self) {
            switch (self) {
                case ValMap valMap: {
                    var values = new List<Value>(valMap.Map.Values);
                    return new Result(new ValList(values));
                }
                case ValString valString: {
                    var str = valString.Value;
                    var values = new List<Value>(str.Length);
                    for (int i = 0; i < str.Length; i++) {
                        values.Add(TAC.Str(str[i].ToString()));
                    }
                    return new Result(new ValList(values));
                }
                default:
                    return new Result(self);
            }
        }
        
        [Description(
            "\n   Get a map with information about the version of MiniScript and" +
            "\n   the host environment that you're currently running.  This will" +
            "\n   include at least the following keys:" +
            "\n     miniscript: a string such as \"1.5\"" +
            "\n     buildDate: a date in yyyy-mm-dd format, like \"2020-05-28\"" +
            "\n     host: a number for the host major and minor version, like 0.9" +
            "\n     hostName: name of the host application, e.g. \"Mini Micro\"" +
            "\n     hostInfo: URL or other short info about the host app" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public Result Version() {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (FunctionInjector.Context.Vm.VersionMap != null) return new Result(FunctionInjector.Context.Vm.VersionMap);

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
            return new Result(FunctionInjector.Context.Vm.VersionMap);
        }
        
        // Moved to the Intrinsic.cs - this method was added manually there, in order to improve performance
        // [Description(
        //     "\n   Pause execution of this script for some amount of time." +
        //     "\n seconds (default 1.0): how many seconds to wait" +
        //     "\n Example: wait 2.5		pauses the script for 2.5 seconds" +
        //     "\n See also: time, yield" +
        //     "\n"
        // )]
        // [Category(INTRINSIC)]
        // public Result Wait(double seconds = 1) {
        //     var now = FunctionInjector.Context.Vm.RunTime;
        //     if (FunctionInjector.PartialResult == null) {
        //         // Just starting our wait; calculate end time and return as partial result
        //         return new Result(new ValNumber(now + seconds), false);
        //     } else {
        //         // Continue until current time exceeds the time in the partial result
        //         return now > FunctionInjector.PartialResult.ResultValue.DoubleValue() ? 
        //             Result.Null : 
        //             FunctionInjector.PartialResult;
        //     }
        // }
        
        [Description(
            "\n   Pause the execution of the script until the next \"tick\" of" +
            "\n   the host app.  In Mini Micro, for example, this waits until" +
            "\n   the next 60Hz frame.  Exact meaning may very, but generally" +
            "\n   if you're doing something in a tight loop, calling yield is" +
            "\n   polite to the host app or other scripts." +
            "\n"
        )]
        [Category(INTRINSIC)]
        public void Yield() {
            FunctionInjector.Context.Vm.Yielding = true;
        }
        
        [Description(
            "\n   To see the signatures of all the intrinsic functions try write: help all" +
            "\n   To see the description of function try write: help \"function name\"" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public string Help(string topicName) {
            return topicName == ALL ? Intrinsic.GetAllIntrinsicInfo() : Intrinsic.GetDescription(topicName);
        }
        
        [Description(
            "\n   To see all categories of the intrinsic functions try write: category all" +
            "\n   To see all functions of category try write: category \"category name\"" +
            "\n Example: category \"" + INTRINSIC + "\"" +
            "\n See also: help" +
            "\n"
        )]
        [Category(INTRINSIC)]
        public string Category(string topicName) {
            return topicName == ALL ? Intrinsic.GetAllCategoriesInfo() : Intrinsic.GetCategory(topicName);
        }

        [Description(
            "\n   Reserved function by interpreter, returns '" + ALL + "'" +
            "\n See also: help, category" +
            "\n"
        )]
        public string All() {
            return ALL;
        }
    }

}