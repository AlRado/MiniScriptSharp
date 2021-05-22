using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Miniscript.errors;
using Miniscript.keywords;
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
        
        // funcRef
        //	Returns a map that represents a function reference in
        //	MiniScript's core type system.  This can be used with `isa`
        //	to check whether a variable refers to a function (but be
        //	sure to use @ to avoid invoking the function and testing
        //	the result).
        // Example: @floor isa funcRef		returns 1
        // See also: number, string, list, map
        public Result FuncRef() {
            FunctionInjector.Context.Vm.FunctionType ??= Intrinsic.FunctionType.EvalCopy(FunctionInjector.Context.Vm.GlobalContext);
            return new Result(FunctionInjector.Context.Vm.FunctionType);
        }
        
        // code
        //	Return the Unicode code point of the first character of
        //	the given string.  This is the inverse of `char`.
        //	May be called with function syntax or dot syntax.
        // self (string): string to get the code point of
        // Returns: Unicode code point of the first character of self
        // Example: "*".code		returns 42
        // Example: code("*")		returns 42
        [MethodOf(typeof(ValString))]
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
        //	across different MiniScript implementations.
        // obj (any type): value to hash
        // Returns: integer hash of the given value
        public int Hash(Value obj) {
            return obj.Hash();
        }
        
        // hasIndex
        //	Return whether the given index is valid for this object, that is,
        //	whether it could be used with square brackets to get some value
        //	from self.  When self is a list or string, the result is true for
        //	integers from -(length of string) to (length of string-1).  When
        //	self is a map, it is true for any key (index) in the map.  If
        //	called on a number, this method throws a runtime exception.
        // self (string, list, or map): object to check for an index on
        // index (any): value to consider as a possible index
        // Returns: 1 if self[index] would be valid; 0 otherwise
        // Example: "foo".hasIndex(2)		returns 1
        // Example: "foo".hasIndex(3)		returns 0
        // See also: indexes
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
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
        
        // indexes
        //	Returns the keys of a dictionary, or the non-negative indexes
        //	for a string or list.
        // self (string, list, or map): object to get the indexes of
        // Returns: a list of valid indexes for self
        // Example: "foo".indexes		returns [0, 1, 2]
        // See also: hasIndex
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
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
        
        // indexOf
        //	Returns index or key of the given value, or if not found,		returns null.
        // self (string, list, or map): object to search
        // value (any): value to search for
        // after (any, optional): if given, starts the search after this index
        // Returns: first index (after `after`) such that self[index] == value, or null
        // Example: "Hello World".indexOf("o")		returns 4
        // Example: "Hello World".indexOf("o", 4)		returns 7
        // Example: "Hello World".indexOf("o", 7)		returns null
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
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
        
        // insert
        //	Insert a new element into a string or list.  In the case of a list,
        //	the list is both modified in place and returned.  Strings are immutable,
        //	so in that case the original string is unchanged, but a new string is
        //	returned with the value inserted.
        // self (string or list): sequence to insert into
        // index (number): position at which to insert the new item
        // value (any): element to insert at the specified index
        // Returns: modified list, new string
        // Example: "Hello".insert(2, 42)		returns "He42llo"
        // See also: remove
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
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

        // self.join
        //	Join the elements of a list together to form a string.
        // self (list): list to join
        // delimiter (string, default " "): string to insert between each pair of elements
        // Returns: string built by joining elements of self with delimiter
        // Example: [2,4,8].join("-")		returns "2-4-8"
        // See also: split
        [MethodOf(typeof(ValList))]
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
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
        public int Len(Value self) {
            return self switch {
                ValList valList => valList.Values.Count,
                ValString valString => valString.Value.Length,
                ValMap map => map.Count,
                _ => 0
            };
        }
        
        // list type
        //	Returns a map that represents the list datatype in
        //	MiniScript's core type system.  This can be used with `isa`
        //	to check whether a variable refers to a list.  You can also
        //	assign new methods here to make them available to all lists.
        // Example: [1, 2, 3] isa list		returns 1
        // See also: number, string, map, funcRef
        public Result List() {
            FunctionInjector.Context.Vm.ListType ??= Intrinsic.ListType.EvalCopy(FunctionInjector.Context.Vm.GlobalContext);
            return new Result(FunctionInjector.Context.Vm.ListType);
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
        [MethodOf(typeof(ValString))]
        public string Lower(Value self) {
            if (!(self is ValString valString)) return self?.ToString();
            
            return valString.Value.ToLower();
        }
        
        // map type
        //	Returns a map that represents the map datatype in
        //	MiniScript's core type system.  This can be used with `isa`
        //	to check whether a variable refers to a map.  You can also
        //	assign new methods here to make them available to all maps.
        // Example: {1:"one"} isa map		returns 1
        // See also: number, string, list, funcRef
        public Result Map() {
            FunctionInjector.Context.Vm.MapType ??= Intrinsic.MapType.EvalCopy(FunctionInjector.Context.Vm.GlobalContext); 
            return new Result(FunctionInjector.Context.Vm.MapType);
        }
        
        // number type
        //	Returns a map that represents the number datatype in
        //	MiniScript's core type system.  This can be used with `isa`
        //	to check whether a variable refers to a number.  You can also
        //	assign new methods here to make them available to all maps
        //	(though because of a limitation in MiniScript's parser, such
        //	methods do not work on numeric literals).
        // Example: 42 isa number		returns 1
        // See also: string, list, map, funcRef
        public Result Number() {
            FunctionInjector.Context.Vm.NumberType ??= Intrinsic.NumberType.EvalCopy(FunctionInjector.Context.Vm.GlobalContext);
            return new Result(FunctionInjector.Context.Vm.NumberType);
        }

        // pi
        //	Returns the universal constant π, that is, the ratio of
        //	a circle's circumference to its diameter.
        // Example: pi		returns 3.141593
        public double Pi() {
            return Math.PI;
        }
        
        // print
        //	Display the given value on the default output stream.  The
        //	exact effect may vary with the environment.  In most cases, the
        //	given string will be followed by the standard line delimiter.
        // s (any): value to print (converted to a string as needed)
        // Returns: null
        // Example: print 6*7
        public void Print(string s) {
            FunctionInjector.Context.Vm.StandardOutput(s ?? "null");
        }
        
        // pop
        //	Removes and	returns the last item in a list, or an arbitrary
        //	key of a map.  If the list or map is empty (or if called on
        //	any other data type), returns null.
        //	May be called with function syntax or dot syntax.
        // self (list or map): object to remove an element from the end of
        // Returns: value removed, or null
        // Example: [1, 2, 3].pop		returns (and removes) 3
        // See also: pull; push; remove
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
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
        
        // pull
        //	Removes and	returns the first item in a list, or an arbitrary
        //	key of a map.  If the list or map is empty (or if called on
        //	any other data type), returns null.
        //	May be called with function syntax or dot syntax.
        // self (list or map): object to remove an element from the end of
        // Returns: value removed, or null
        // Example: [1, 2, 3].pull		returns (and removes) 1
        // See also: pop; push; remove
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
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
        
        // push
        //	Appends an item to the end of a list, or inserts it into a map
        //	as a key with a value of 1.
        //	May be called with function syntax or dot syntax.
        // self (list or map): object to append an element to
        // Returns: self
        // See also: pop, pull, insert
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
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
        
        // range
        //	Return a list containing a series of numbers within a range.
        // from (number, default 0): first number to include in the list
        // to (number, default 0): point at which to stop adding numbers to the list
        // step (number, optional): amount to add to the previous number on each step;
        //	defaults to 1 if to > from, or -1 if to < from
        // Example: range(50, 5, -10)		returns [50, 40, 30, 20, 10]
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
        
        // remove
        //	Removes part of a list, map, or string.  Exact behavior depends on
        //	the data type of self:
        // 		list: removes one element by its index; the list is mutated in place;
        //			returns null, and throws an error if the given index out of range
        //		map: removes one key/value pair by key; the map is mutated in place;
        //			returns 1 if key was found, 0 otherwise
        //		string:	returns a new string with the first occurrence of k removed
        //	May be called with function syntax or dot syntax.
        // self (list, map, or string): object to remove something from
        // k (any): index or substring to remove
        // Returns: (see above)
        // Example: a=["a","b","c"]; a.remove 1		leaves a == ["a", "c"]
        // Example: d={"ichi":"one"}; d.remove "ni"		returns 0
        // Example: "Spam".remove("S")		returns "pam"
        // See also: indexOf
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
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
        
        // replace
        //	Replace all matching elements of a list or map, or substrings of a string,
        //	with a new value.Lists and maps are mutated in place, and return themselves.
        //	Strings are immutable, so the original string is (of course) unchanged, but
        //	a new string with the replacement is returned.  Note that with maps, it is
        //	the values that are searched for and replaced, not the keys.
        // self (list, map, or string): object to replace elements of
        // oldVal (any): value or substring to replace
        // newVal (any): new value or substring to substitute where oldVal is found
        // maxCount (number, optional): if given, replace no more than this many
        // Returns: modified list or map, or new string, with replacements Done
        // Example: "Happy Pappy".replace("app", "ol")		returns "Holy Poly"
        // Example: [1,2,3,2,5].replace(2, 42)		returns (and mutates to) [2, 42, 3, 42, 5]
        // Example: d = {1: "one"}; d.replace("one", "ichi")		returns (and mutates to) {1: "ichi"}
        [MethodOf(typeof(ValString))]
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
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
        
        // sign
        //	Return -1 for negative numbers, 1 for positive numbers, and 0 for zero.
        // x (number): number to get the sign of
        // Returns: sign of the number
        // Example: sign(-42.6)		returns -1
        public double Sign(double x) {
            return Math.Sign(x);
        }
        
        // sin
        //	Returns the sine of the given angle (in radians).
        // radians (number): angle, in radians, to get the sine of
        // Returns: sine of the given angle
        // Example: sin(pi/2)		returns 1
        public double Sin(double radians) {
            return Math.Sin(radians);
        }
        
        // slice
        //	Return a subset of a string or list.  This is equivalent to using
        //	the square-brackets slice operator seq[from:to], but with ordinary
        //	function syntax.
        // seq (string or list): sequence to get a subsequence of
        // from (number, default 0): 0-based index to the first element to return (if negative, counts from the end)
        // to (number, optional): 0-based index of first element to *not* include in the result
        //		(if negative, count from the end; if omitted, return the rest of the sequence)
        // Returns: substring or sublist
        // Example: slice("Hello", -2)		returns "lo"
        // Example: slice(["a","b","c","d"], 1, 3)		returns ["b", "c"]
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
        
        // sort
        //	Sorts a list in place.  With null or no argument, this sorts the
        //	list elements by their own values.  With the byKey argument, each
        //	element is indexed by that argument, and the elements are sorted
        //	by the result.  (This only works if the list elements are maps, or
        //	they are lists and byKey is an integer index.)
        // self (list): list to sort
        // byKey (optional): if given, sort each element by indexing with this key
        // ascending (optional, default true): if false, sort in descending order
        // Returns: self (which has been sorted in place)
        // Example: a = [5,3,4,1,2]; a.sort		results in a == [1, 2, 3, 4, 5]
        // See also: shuffle
        [MethodOf(typeof(ValList))]
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

        // split
        //	Split a string into a list, by some delimiter.
        //	May be called with function syntax or dot syntax.
        // self (string): string to split
        // delimiter (string, default " "): substring to split on
        // maxCount (number, default -1): if > 0, split into no more than this many strings
        // Returns: list of substrings found by splitting on delimiter
        // Example: "foo bar baz".split		returns ["foo", "bar", "baz"]
        // Example: "foo bar baz".split("a", 2)		returns ["foo b", "r baz"]
        // See also: join
        [MethodOf(typeof(ValString))]
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
        
        // sqrt
        //	Returns the square root of a number.
        // x (number): number to get the square root of
        // Returns: square root of x
        // Example: sqrt(1764)		returns 42
        public double Sqrt(double x) {
            return Math.Sqrt(x);
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
        //	MiniScript's core type system.  This can be used with `isa`
        //	to check whether a variable refers to a string.  You can also
        //	assign new methods here to make them available to all strings.
        // Example: "Hello" isa string		returns 1
        // See also: number, list, map, funcRef
        public Result String() {
            FunctionInjector.Context.Vm.StringType ??= Intrinsic.StringType.EvalCopy(FunctionInjector.Context.Vm.GlobalContext);
            return new Result(FunctionInjector.Context.Vm.StringType);
        }
        
        // shuffle
        //	Randomize the order of elements in a list, or the mappings from
        //	keys to values in a map.  This is Done in place.
        // self (list or map): object to shuffle
        // Returns: null
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
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
        [MethodOf(typeof(ValList))]
        [MethodOf(typeof(ValMap))]
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
        
        // version
        //	Get a map with information about the version of MiniScript and
        //	the host environment that you're currently running.  This will
        //	include at least the following keys:
        //		miniscript: a string such as "1.5"
        //		buildDate: a date in yyyy-mm-dd format, like "2020-05-28"
        //		host: a number for the host major and minor version, like 0.9
        //		hostName: name of the host application, e.g. "Mini Micro"
        //		hostInfo: URL or other short info about the host app
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
        
        [Description(
            "\n   To see the signatures of all the intrinsic functions try write: help \"all\"" +
            "\n   To see the description of function try write: help \"function name\"" +
            "\n"
        )]
        public string Help(string topicName) {
            return topicName == Consts.ALL ? Intrinsic.GetAllIntrinsicInfo() : Intrinsic.GetDescription(topicName);
        }

    }

}