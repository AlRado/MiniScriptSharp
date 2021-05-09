/*	MiniscriptIntrinsics.cs

This file defines the Intrinsic class, which represents a built-in function
available to Minisript code.  All intrinsics are held in static storage, so
this class includes static functions such as GetByName to look up 
already-defined intrinsics.  See Chapter 2 of the Minisript Integration
Guide for details on adding your own intrinsics.

This file also contains the Intrinsics static class, where all of the standard
intrinsics are defined.  This is initialized automatically, so normally you
don’t need to worry about it, though it is a good place to look for examples
of how to write intrinsic functions.

Note that you should put any intrinsics you add in a separate file; leave the
Minisript source files untouched, so you can easily replace them when updates
become available.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Miniscript.errors;
using Miniscript.tac;
using Miniscript.types;
using static Miniscript.intrinsic.Consts;

namespace Miniscript.intrinsic {
		
	/// <summary>
	/// Intrinsic: represents an intrinsic function available to Minisript code.
	/// </summary>
	public class Intrinsic {
		
		/// <summary>
		/// FunctionType: a static map that represents the Function type.
		/// </summary>
		public static readonly ValMap FunctionType;

		/// <summary>
		/// ListType: a static map that represents the List type, and provides
		/// intrinsic methods that can be invoked on it via dot syntax.
		/// </summary>
		public static readonly ValMap ListType;

		/// <summary>
		/// StringType: a static map that represents the String type, and provides
		/// intrinsic methods that can be invoked on it via dot syntax.
		/// </summary>
		public static readonly ValMap StringType;

		/// <summary>
		/// MapType: a static map that represents the Map type, and provides
		/// intrinsic methods that can be invoked on it via dot syntax.
		/// </summary>
		public static readonly ValMap MapType;

		/// <summary>
		/// NumberType: a static map that represents the Number type.
		/// </summary>
		public static readonly ValMap NumberType;

		// static map from Values to short names, used when displaying lists/maps;
		// feel free to add to this any values (especially lists/maps) provided
		// by your own intrinsics.
		public static readonly Dictionary<Value, string> ShortNames = new Dictionary<Value, string>();
		
		// name of this intrinsic (should be a valid Minisript identifier)
		private string name;
		
		// actual C# code invoked by the intrinsic
		private IntrinsicCode code;
		
		// a numeric ID (used internally -- don't worry about this)
		public int Id { get; private set; }

		private Function function;
		private ValFunction valFunction;	// (cached wrapper for function)

		private static readonly List<Intrinsic> all = new List<Intrinsic>() { null };
		private static readonly Dictionary<string, Intrinsic> nameMap = new Dictionary<string, Intrinsic>();
		
		private readonly ValString _self = new ValString(SELF);
		
		private static Random random;	// TODO: consider storing this on the context, instead of global!
		
		/// <summary>
		/// Factory method to create a new Intrinsic, filling out its name as given,
		/// and other internal properties as needed.  You'll still need to add any
		/// parameters, and define the code it runs.
		/// </summary>
		/// <param name="name">intrinsic name</param>
		/// <returns>freshly minted (but empty) static Intrinsic</returns>
		public static Intrinsic Create(string name) {
			var result = new Intrinsic {name = name, Id = all.Count, function = new Function(null)};
			result.valFunction = new ValFunction(result.function);
			all.Add(result);
			nameMap[name] = result;
			return result;
		}
		
		/// <summary>
		/// Look up an Intrinsic by its internal numeric ID.
		/// </summary>
		public static Intrinsic GetByID(int id) {
			return all[id];
		}
		
		/// <summary>
		/// Look up an Intrinsic by its name.
		/// </summary>
		public static Intrinsic GetByName(string name) {
			return nameMap.TryGetValue(name, out var result) ? result : null;
		}
		
		/// <summary>
		/// Add a parameter to this Intrinsic, optionally with a default value
		/// to be used if the user doesn't supply one.  You must add parameters
		/// in the same order in which arguments must be supplied.
		/// </summary>
		/// <param name="name">parameter name</param>
		/// <param name="defaultValue">default value, if any</param>
		public void AddParam(string name, Value defaultValue=null) {
			function.Parameters.Add(new Param(name, defaultValue));
		}
		
		/// <summary>
		/// Add a parameter with a numeric default value.  (See comments on
		/// the first version of AddParam above.)
		/// </summary>
		/// <param name="name">parameter name</param>
		/// <param name="defaultValue">default value for this parameter</param>
		public void AddParam(string name, double defaultValue) {
			Value defVal = defaultValue switch {
				0 => ValNumber.Zero,
				1 => ValNumber.One,
				_ => TAC.Num(defaultValue)
			};
			function.Parameters.Add(new Param(name, defVal));
		}

		/// <summary>
		/// Add a parameter with a string default value.  (See comments on
		/// the first version of AddParam above.)
		/// </summary>
		/// <param name="name">parameter name</param>
		/// <param name="defaultValue">default value for this parameter</param>
		public void AddParam(string name, string defaultValue) {
			Value defVal;
			if (string.IsNullOrEmpty(defaultValue)) defVal = ValString.Empty;
			else
				defVal = defaultValue switch {
					IS_A => ValString.MagicIsA,
					SELF => _self,
					_ => new ValString(defaultValue)
				};
			function.Parameters.Add(new Param(name, defVal));
		}
		
		/// <summary>
		/// GetFunc is used internally by the compiler to get the Minisript function
		/// that makes an intrinsic call.
		/// </summary>
		public ValFunction GetFunc() {
			if (function.Code == null) {
				// Our little wrapper function is a single opcode: CallIntrinsicA.
				// It really exists only to provide a local variable context for the parameters.
				function.Code = new List<Line>();
				function.Code.Add(new Line(TAC.LTemp(0), Line.Op.CallIntrinsicA, TAC.Num(Id)));
			}
			return valFunction;
		}
		
		/// <summary>
		/// Internally-used function to execute an intrinsic (by ID) given a
		/// context and a partial result.
		/// </summary>
		public static Result Execute(int id, Context context, Result partialResult) {
			var item = GetByID(id);
			return item.code(context, partialResult);
		}
		
		/// <summary>
		/// Intrinsic static constructor: called automatically during script setup to make sure
		/// that all our standard intrinsics are defined.  Note how we use a
		/// private bool flag to ensure that we don't create our intrinsics more
		/// than once, no matter how many times this method is called.
		/// </summary>
		static Intrinsic() {
			FunctionType = new ValMap();
			NumberType = new ValMap();
			ListType = new ValMap();
			StringType = new ValMap();
			MapType = new ValMap();
			
			// abs
			//	Returns the absolute value of the given number.
			// x (number, default 0): number to take the absolute value of.
			// Example: abs(-42)		returns 42
			var f = Create(ABS);
			f.AddParam("x", 0);
			f.code = (context, partialResult) => new Result(Math.Abs(context.GetVar("x").DoubleValue()));

			// acos
			//	Returns the inverse cosine, that is, the angle 
			//	(in radians) whose cosine is the given value.
			// x (number, default 0): cosine of the angle to find.
			// Returns: angle, in radians, whose cosine is x.
			// Example: acos(0) 		returns 1.570796
			f = Create(ACOS);
			f.AddParam("x", 0);
			f.code = (context, partialResult) => new Result(Math.Acos(context.GetVar("x").DoubleValue()));

			// asin
			//	Returns the inverse sine, that is, the angle
			//	(in radians) whose sine is the given value.
			// x (number, default 0): cosine of the angle to find.
			// Returns: angle, in radians, whose cosine is x.
			// Example: asin(1) return 1.570796
			f = Create(ASIN);
			f.AddParam("x", 0);
			f.code = (context, partialResult) => new Result(Math.Asin(context.GetVar("x").DoubleValue()));

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
			f = Create(ATAN);
			f.AddParam("y", 0);
			f.AddParam("x", 1);
			f.code = (context, partialResult) => {
				var y = context.GetVar("y").DoubleValue();
				var x = context.GetVar("x").DoubleValue();
				return x == 1.0 ? new Result(Math.Atan(y)) : new Result(Math.Atan2(y, x));
			};

			// bitAnd
			//	Treats its arguments as integers, and computes the bitwise
			//	`and`: each bit in the result is set only if the corresponding
			//	bit is set in both arguments.
			// i (number, default 0): first integer argument
			// j (number, default 0): second integer argument
			// Returns: bitwise `and` of i and j
			// Example: bitAnd(14, 7)		returns 6
			// See also: bitOr; bitXor
			f = Create(BIT_AND);
			f.AddParam("i", 0);
			f.AddParam("j", 0);
			f.code = (context, partialResult) => {
				var i = context.GetLocalInt("i");
				var j = context.GetLocalInt("j");
				return new Result(i & j);
			};
			
			// bitOr
			//	Treats its arguments as integers, and computes the bitwise
			//	`or`: each bit in the result is set if the corresponding
			//	bit is set in either (or both) of the arguments.
			// i (number, default 0): first integer argument
			// j (number, default 0): second integer argument
			// Returns: bitwise `or` of i and j
			// Example: bitOr(14, 7)		returns 15
			// See also: bitAnd; bitXor
			f = Create(BIT_OR);
			f.AddParam("i", 0);
			f.AddParam("j", 0);
			f.code = (context, partialResult) => {
				var i = context.GetLocalInt("i");
				var j = context.GetLocalInt("j");
				return new Result(i | j);
			};
			
			// bitXor
			//	Treats its arguments as integers, and computes the bitwise
			//	`xor`: each bit in the result is set only if the corresponding
			//	bit is set in exactly one (not zero or both) of the arguments.
			// i (number, default 0): first integer argument
			// j (number, default 0): second integer argument
			// Returns: bitwise `and` of i and j
			// Example: bitAnd(14, 7)		returns 9
			// See also: bitAnd; bitOr
			f = Create(BIT_XOR);
			f.AddParam("i", 0);
			f.AddParam("j", 0);
			f.code = (context, partialResult) => {
				var i = context.GetLocalInt("i");
				var j = context.GetLocalInt("j");
				return new Result(i ^ j);
			};
			
			// char
			//	Gets a character from its Unicode code point.
			// codePoint (number, default 65): Unicode code point of a character
			// Returns: string containing the specified character
			// Example: char(42)		returns "*"
			// See also: code
			f = Create(CHAR);
			f.AddParam("codePoint", 65);
			f.code = (context, partialResult) => {
				var codepoint = context.GetLocalInt("codePoint");
				var s = char.ConvertFromUtf32(codepoint);
				return new Result(s);
			};
			
			// ceil
			//	Returns the "ceiling", i.e. closest whole number 
			//	greater than or equal to the given number.
			// x (number, default 0): number to get the ceiling of
			// Returns: closest whole number not less than x
			// Example: ceil(41.2)		returns 42
			// See also: floor
			f = Create(CEIL);
			f.AddParam("x", 0);
			f.code = (context, partialResult) => new Result(Math.Ceiling(context.GetVar("x").DoubleValue()));
			
			// code
			//	Return the Unicode code point of the first character of
			//	the given string.  This is the inverse of `char`.
			//	May be called with function syntax or dot syntax.
			// self (string): string to get the code point of
			// Returns: Unicode code point of the first character of self
			// Example: "*".code		returns 42
			// Example: code("*")		returns 42
			f = Create(CODE);
			f.AddParam(SELF);
			f.code = (context, partialResult) => {
				var self = context.Self;
				var codepoint = 0;
				if (self != null) codepoint = char.ConvertToUtf32(self.ToString(), 0);
				return new Result(codepoint);
			};
						
			// cos
			//	Returns the cosine of the given angle (in radians).
			// radians (number): angle, in radians, to get the cosine of
			// Returns: cosine of the given angle
			// Example: cos(0)		returns 1
			f = Create(COS);
			f.AddParam("radians", 0);
			f.code = (context, partialResult) => new Result(Math.Cos(context.GetVar("radians").DoubleValue()));

			// floor
			//	Returns the "floor", i.e. closest whole number 
			//	less than or equal to the given number.
			// x (number, default 0): number to get the floor of
			// Returns: closest whole number not more than x
			// Example: floor(42.9)		returns 42
			// See also: floor
			f = Create(FLOOR);
			f.AddParam("x", 0);
			f.code = (context, partialResult) => new Result(Math.Floor(context.GetVar("x").DoubleValue()));

			// funcRef
			//	Returns a map that represents a function reference in
			//	Minisript's core type system.  This can be used with `isa`
			//	to check whether a variable refers to a function (but be
			//	sure to use @ to avoid invoking the function and testing
			//	the result).
			// Example: @floor isa funcRef		returns 1
			// See also: number, string, list, map
			f = Create(FUNC_REF);
			f.code = (context, partialResult) => {
				context.Vm.FunctionType ??= FunctionType.EvalCopy(context.Vm.GlobalContext);
				return new Result(context.Vm.FunctionType);
			};
			
			// hash
			//	Returns an integer that is "relatively unique" to the given value.
			//	In the case of strings, the hash is case-sensitive.  In the case
			//	of a list or map, the hash combines the hash values of all elements.
			//	Note that the value returned is platform-dependent, and may vary
			//	across different Minisript implementations.
			// obj (any type): value to hash
			// Returns: integer hash of the given value
			f = Create(HASH);
			f.AddParam("obj");
			f.code = (context, partialResult) => {
				var val = context.GetVar("obj");
				return new Result(val.Hash());
			};

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
			f = Create(HAS_INDEX);
			f.AddParam(SELF);
			f.AddParam(INDEX);
			f.code = (context, partialResult) => {
				var self = context.Self;
				var index = context.GetVar(INDEX);
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
			};
			
			// indexes
			//	Returns the keys of a dictionary, or the non-negative indexes
			//	for a string or list.
			// self (string, list, or map): object to get the indexes of
			// Returns: a list of valid indexes for self
			// Example: "foo".indexes		returns [0, 1, 2]
			// See also: hasIndex
			f = Create(INDEXES);
			f.AddParam(SELF);
			f.code = (context, partialResult) => {
				var self = context.Self;
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
			};
			
			// indexOf
			//	Returns index or key of the given value, or if not found,		returns null.
			// self (string, list, or map): object to search
			// value (any): value to search for
			// after (any, optional): if given, starts the search after this index
			// Returns: first index (after `after`) such that self[index] == value, or null
			// Example: "Hello World".indexOf("o")		returns 4
			// Example: "Hello World".indexOf("o", 4)		returns 7
			// Example: "Hello World".indexOf("o", 7)		returns null			
			f = Create(INDEX_OF);
			f.AddParam(SELF);
			f.AddParam("value");
			f.AddParam("after");
			f.code = (context, partialResult) => {
				var self = context.Self;
				var value = context.GetVar("value");
				var after = context.GetVar("after");
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
			};

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
			f = Create(INSERT);
			f.AddParam(SELF);
			f.AddParam(INDEX);
			f.AddParam("value");
			f.code = (context, partialResult) => {
				var self = context.Self;
				var index = context.GetVar(INDEX);
				var value = context.GetVar("value");
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
			};

			// self.join
			//	Join the elements of a list together to form a string.
			// self (list): list to join
			// delimiter (string, default " "): string to insert between each pair of elements
			// Returns: string built by joining elements of self with delimiter
			// Example: [2,4,8].join("-")		returns "2-4-8"
			// See also: split
			f = Create(JOIN);
			f.AddParam(SELF);
			f.AddParam("delimiter", " ");
			f.code = (context, partialResult) => {
				var val = context.Self;
				var delimiter = context.GetVar("delimiter").ToString();
				if (!(val is ValList valList)) return new Result(val);
				var list = new List<string>(valList.Values.Count);
				list.AddRange(valList.Values.Select(t => t?.ToString()));
				var result = string.Join(delimiter, list.ToArray());
				return new Result(result);
			};
			
			// self.len
			//	Return the number of characters in a string, elements in
			//	a list, or key/value pairs in a map.
			//	May be called with function syntax or dot syntax.
			// self (list, string, or map): object to get the length of
			// Returns: length (number of elements) in self
			// Example: "hello".len		returns 5
			f = Create(LEN);
			f.AddParam(SELF);
			f.code = (context, partialResult) => {
				return context.Self switch {
					ValList valList => new Result(valList.Values.Count),
					ValString valString => new Result(valString.Value.Length),
					ValMap map => new Result(map.Count),
					_ => Result.Null
				};
			};
			
			// list type
			//	Returns a map that represents the list datatype in
			//	Minisript's core type system.  This can be used with `isa`
			//	to check whether a variable refers to a list.  You can also
			//	assign new methods here to make them available to all lists.
			// Example: [1, 2, 3] isa list		returns 1
			// See also: number, string, map, funcRef
			f = Create(LIST);
			f.code = (context, partialResult) => {
				context.Vm.ListType ??= ListType.EvalCopy(context.Vm.GlobalContext);
				return new Result(context.Vm.ListType);
			};
			
			// log(x, base)
			//	Returns the logarithm (with the given) of the given number,
			//	that is, the number y such that base^y = x.
			// x (number): number to take the log of
			// base (number, default 10): logarithm base
			// Returns: a number that, when base is raised to it, produces x
			// Example: log(1000)		returns 3 (because 10^3 == 1000)
			f = Create(LOG);
			f.AddParam("x", 0);
			f.AddParam("base", 10);
			f.code = (context, partialResult) => {
				var x = context.GetVar("x").DoubleValue();
				var b = context.GetVar("base").DoubleValue();
				double result;
				if (Math.Abs(b - 2.718282) < 0.000001) result = Math.Log(x);
				else result = Math.Log(x) / Math.Log(b);
				return new Result(result);
			};
			
			// lower
			//	Return a lower-case version of a string.
			//	May be called with function syntax or dot syntax.
			// self (string): string to lower-case
			// Returns: string with all capital letters converted to lowercase
			// Example: "Mo Spam".lower		returns "mo spam"
			// See also: upper
			f = Create(LOWER);
			f.AddParam(SELF);
			f.code = (context, partialResult) => {
				var val = context.Self;
				if (!(val is ValString valString)) return new Result(val);
				var str = valString.Value;
				return new Result(str.ToLower());
			};

			// map type
			//	Returns a map that represents the map datatype in
			//	Minisript's core type system.  This can be used with `isa`
			//	to check whether a variable refers to a map.  You can also
			//	assign new methods here to make them available to all maps.
			// Example: {1:"one"} isa map		returns 1
			// See also: number, string, list, funcRef
			f = Create(MAP);
			f.code = (context, partialResult) => {
				context.Vm.MapType ??= MapType.EvalCopy(context.Vm.GlobalContext);
				return new Result(context.Vm.MapType);
			};
			
			// number type
			//	Returns a map that represents the number datatype in
			//	Minisript's core type system.  This can be used with `isa`
			//	to check whether a variable refers to a number.  You can also
			//	assign new methods here to make them available to all maps
			//	(though because of a limitation in Minisript's parser, such
			//	methods do not work on numeric literals).
			// Example: 42 isa number		returns 1
			// See also: string, list, map, funcRef
			f = Create(NUMBER);
			f.code = (context, partialResult) => {
				context.Vm.NumberType ??= NumberType.EvalCopy(context.Vm.GlobalContext);
				return new Result(context.Vm.NumberType);
			};
			
			// pi
			//	Returns the universal constant π, that is, the ratio of
			//	a circle's circumference to its diameter.
			// Example: pi		returns 3.141593
			f = Create(PI);
			f.code = (context, partialResult) => new Result(Math.PI);

			// print
			//	Display the given value on the default output stream.  The
			//	exact effect may vary with the environment.  In most cases, the
			//	given string will be followed by the standard line delimiter.
			// s (any): value to print (converted to a string as needed)
			// Returns: null
			// Example: print 6*7
			f = Create(PRINT);
			f.AddParam("s", ValString.Empty);
			f.code = (context, partialResult) => {
				var s = context.GetVar("s");
				context.Vm.StandardOutput(s != null ? s.ToString() : NULL);
				return Result.Null;
			};
				
			// pop
			//	Removes and	returns the last item in a list, or an arbitrary
			//	key of a map.  If the list or map is empty (or if called on
			//	any other data type), returns null.
			//	May be called with function syntax or dot syntax.
			// self (list or map): object to remove an element from the end of
			// Returns: value removed, or null
			// Example: [1, 2, 3].pop		returns (and removes) 3
			// See also: pull; push; remove
			f = Create(POP);
			f.AddParam(SELF);
			f.code = (context, partialResult) => {
				var self = context.Self;
				switch (self) {
					case ValList valList: {
						var list = valList.Values;
						if (list.Count < 1) return Result.Null;
						var result = list[list.Count-1];
						list.RemoveAt(list.Count-1);
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
			};

			// pull
			//	Removes and	returns the first item in a list, or an arbitrary
			//	key of a map.  If the list or map is empty (or if called on
			//	any other data type), returns null.
			//	May be called with function syntax or dot syntax.
			// self (list or map): object to remove an element from the end of
			// Returns: value removed, or null
			// Example: [1, 2, 3].pull		returns (and removes) 1
			// See also: pop; push; remove
			f = Create(PULL);
			f.AddParam(SELF);
			f.code = (context, partialResult) => {
				var self = context.Self;
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
			};

			// push
			//	Appends an item to the end of a list, or inserts it into a map
			//	as a key with a value of 1.
			//	May be called with function syntax or dot syntax.
			// self (list or map): object to append an element to
			// Returns: self
			// See also: pop, pull, insert
			f = Create(PUSH);
			f.AddParam(SELF);
			f.AddParam("value");
			f.code = (context, partialResult) => {
				var self = context.Self;
				var value = context.GetVar("value");
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
			};

			// range
			//	Return a list containing a series of numbers within a range.
			// from (number, default 0): first number to include in the list
			// to (number, default 0): point at which to stop adding numbers to the list
			// step (number, optional): amount to add to the previous number on each step;
			//	defaults to 1 if to > from, or -1 if to < from
			// Example: range(50, 5, -10)		returns [50, 40, 30, 20, 10]
			f = Create(RANGE);
			f.AddParam("from", 0);
			f.AddParam("to", 0);
			f.AddParam("step");
			List<Value> values;
			f.code = (context, partialResult) => {
				var fromVal = context.GetVar("from").DoubleValue();
				var toVal = context.GetVar("to").DoubleValue();
				double step = (toVal >= fromVal ? 1 : -1);
				var p2 = context.GetVar("step");
				if (p2 is ValNumber number) step = number.Value;
				if (step == 0) throw new RuntimeException("range() error (step==0)");
				var count = (int)((toVal - fromVal) / step) + 1;
				if (count > ValList.MaxSize) throw new RuntimeException("list too large");
				try {
					values = new List<Value>(count);
					for (double v = fromVal; step > 0 ? (v <= toVal) : (v >= toVal); v += step) {
						values.Add(TAC.Num(v));
					}
				} catch (SystemException e) {
					// uh-oh... probably out-of-memory exception; clean up and bail out
					values = null;
					throw(new LimitExceededException("range() error", e));
				}
				return new Result(new ValList(values));
			};

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
			f = Create(REMOVE);
			f.AddParam(SELF);
			f.AddParam("k");
			f.code = (context, partialResult) => {
				var self = context.Self;
				var k = context.GetVar("k");
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
			};

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
			f = Create(REPLACE);
			f.AddParam(SELF);
			f.AddParam("oldVal");
			f.AddParam("newVal");
			f.AddParam("maxCount");
			f.code = (context, partialResult) => {
				var self = context.Self;
				if (self == null) throw new RuntimeException("argument to 'replace' must not be null");
				var oldVal = context.GetVar("oldVal");
				var newVal = context.GetVar("newVal");
				var maxCountVal = context.GetVar("maxCount");
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
							idx = selfList.Values.FindIndex(idx+1, x => x.Equality(oldVal) == 1);
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
			};

			// round
			//	Rounds a number to the specified number of decimal places.  If given
			//	a negative number for decimalPlaces, then rounds to a power of 10:
			//	-1 rounds to the nearest 10, -2 rounds to the nearest 100, etc.
			// x (number): number to round
			// decimalPlaces (number, defaults to 0): how many places past the decimal point to round to
			// Example: round(pi, 2)		returns 3.14
			// Example: round(12345, -3)		returns 12000
			f = Create(ROUND);
			f.AddParam("x", 0);
			f.AddParam("decimalPlaces", 0);
			f.code = (context, partialResult) => {
				var num = context.GetVar("x").DoubleValue();
				var decimalPlaces = context.GetVar("decimalPlaces").IntValue();
				return new Result(Math.Round(num, decimalPlaces));
			};


			// rnd
			//	Generates a pseudorandom number between 0 and 1 (including 0 but
			//	not including 1).  If given a seed, then the generator is reset
			//	with that seed value, allowing you to create repeatable sequences
			//	of random numbers.  If you never specify a seed, then it is
			//	initialized automatically, generating a unique sequence on each run.
			// seed (number, optional): if given, reset the sequence with this value
			// Returns: pseudorandom number in the range [0,1)
			f = Create(RND);
			f.AddParam("seed");
			f.code = (context, partialResult) => {
				random ??= new Random();
				var seed = context.GetVar("seed");
				if (seed != null) random = new Random(seed.IntValue());
				return new Result(random.NextDouble());
			};

			// sign
			//	Return -1 for negative numbers, 1 for positive numbers, and 0 for zero.
			// x (number): number to get the sign of
			// Returns: sign of the number
			// Example: sign(-42.6)		returns -1
			f = Create(SIGN);
			f.AddParam("x", 0);
			f.code = (context, partialResult) => new Result(Math.Sign(context.GetVar("x").DoubleValue()));

			// sin
			//	Returns the sine of the given angle (in radians).
			// radians (number): angle, in radians, to get the sine of
			// Returns: sine of the given angle
			// Example: sin(pi/2)		returns 1
			f = Create(SIN);
			f.AddParam("radians", 0);
			f.code = (context, partialResult) => new Result(Math.Sin(context.GetVar("radians").DoubleValue()));
				
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
			f = Create(SLICE);
			f.AddParam("seq");
			f.AddParam("from", 0);
			f.AddParam("to");
			f.code = (context, partialResult) => {
				var seq = context.GetVar("seq");
				var fromIdx = context.GetVar("from").IntValue();
				var toVal = context.GetVar("to");
				var toIdx = 0;
				if (toVal != null) toIdx = toVal.IntValue();
				switch (seq) {
					case ValList valList: {
						var list = valList.Values;
						if (fromIdx < 0) fromIdx += list.Count;
						if (fromIdx < 0) fromIdx = 0;
						if (toVal == null) toIdx = list.Count;
						if (toIdx < 0) toIdx += list.Count;
						if (toIdx > list.Count) toIdx = list.Count;
						var slice = new ValList();
						if (fromIdx < list.Count && toIdx > fromIdx) {
							for (int i = fromIdx; i < toIdx; i++) {
								slice.Values.Add(list[i]);
							}
						}
						return new Result(slice);
					}
					case ValString valString: {
						var str = valString.Value;
						if (fromIdx < 0) fromIdx += str.Length;
						if (fromIdx < 0) fromIdx = 0;
						if (toVal == null) toIdx = str.Length;
						if (toIdx < 0) toIdx += str.Length;
						if (toIdx > str.Length) toIdx = str.Length;
						if (toIdx - fromIdx <= 0) return Result.EmptyString;
						
						return new Result(str.Substring(fromIdx, toIdx - fromIdx));
					}
					default:
						return Result.Null;
				}
			};
			
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
			f = Create(SORT);
			f.AddParam(SELF);
			f.AddParam("byKey");
			f.AddParam("ascending", ValNumber.One);
			f.code = (context, partialResult) => {
				var self = context.Self;
				if (!(self is ValList list) || list.Values.Count < 2) return new Result(self);

				IComparer<Value> sorter;
				if (context.GetVar("ascending").BoolValue()) sorter = ValueSorter.Instance;
				else sorter = ValueReverseSorter.Instance;

				var byKey = context.GetVar("byKey");
				if (byKey == null) {
					// Simple case: sort the values as themselves
					list.Values = list.Values.OrderBy((arg) => arg, sorter).ToList();
				} else {
					// Harder case: sort by a key.
					var count = list.Values.Count;
					var arr = new KeyedValue[count];
					for (int i=0; i<count; i++) {
						arr[i].Value = list.Values[i];
						//arr[i].valueIndex = i;
					}
					// The key for each item will be the item itself, unless it is a map, in which
					// case it's the item indexed by the given key.  (Works too for lists if our
					// index is an integer.)
					var byKeyInt = byKey.IntValue();
					for (int i=0; i<count; i++) {
						var item = list.Values[i];
						switch (item) {
							case ValMap map:
								arr[i].SortKey = map.Lookup(byKey);
								break;
							case ValList valList: {
								if (byKeyInt > -valList.Values.Count && byKeyInt < valList.Values.Count) arr[i].SortKey = valList.Values[byKeyInt];
								else arr[i].SortKey = null;
								break;
							}
						}
					}
					// Now sort our list of keyed values, by key
					var sortedArr = arr.OrderBy((arg) => arg.SortKey, sorter);
					// And finally, convert that back into our list
					var idx=0;
					foreach (var kv in sortedArr) {
						list.Values[idx++] = kv.Value;
					}
				}
				return new Result(list);
			};

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
			f = Create(SPLIT);
			f.AddParam(SELF);
			f.AddParam("delimiter", " ");
			f.AddParam("maxCount", -1);
			f.code = (context, partialResult) => {
				var self = context.Self.ToString();
				var delim = context.GetVar("delimiter").ToString();
				var maxCount = context.GetVar("maxCount").IntValue();
				var result = new ValList();
				var pos = 0;
				while (pos < self.Length) {
					int nextPos;
					if (maxCount >= 0 && result.Values.Count == maxCount - 1) nextPos = self.Length;
					else if (delim.Length == 0) nextPos = pos+1;
					else nextPos = self.IndexOf(delim, pos, StringComparison.InvariantCulture);
					if (nextPos < 0) nextPos = self.Length;
					result.Values.Add(new ValString(self.Substring(pos, nextPos - pos)));
					pos = nextPos + delim.Length;
					if (pos == self.Length && delim.Length > 0) result.Values.Add(ValString.Empty);
				}
				return new Result(result);
			};

			// sqrt
			//	Returns the square root of a number.
			// x (number): number to get the square root of
			// Returns: square root of x
			// Example: sqrt(1764)		returns 42
			f = Create(SQRT);
			f.AddParam("x", 0);
			f.code = (context, partialResult) => new Result(Math.Sqrt(context.GetVar("x").DoubleValue()));

			// str
			//	Convert any value to a string.
			// x (any): value to convert
			// Returns: string representation of the given value
			// Example: str(42)		returns "42"
			// See also: val
			f = Create(STR);
			f.AddParam("x", ValString.Empty);
			f.code = (context, partialResult) => new Result(context.GetVar("x").ToString());

			// string type
			//	Returns a map that represents the string datatype in
			//	Minisript's core type system.  This can be used with `isa`
			//	to check whether a variable refers to a string.  You can also
			//	assign new methods here to make them available to all strings.
			// Example: "Hello" isa string		returns 1
			// See also: number, list, map, funcRef
			f = Create(STRING);
			f.code = (context, partialResult) => {
				context.Vm.StringType ??= StringType.EvalCopy(context.Vm.GlobalContext);
				return new Result(context.Vm.StringType);
			};

			// shuffle
			//	Randomize the order of elements in a list, or the mappings from
			//	keys to values in a map.  This is Done in place.
			// self (list or map): object to shuffle
			// Returns: null
			f = Create(SHUFFLE);
			f.AddParam(SELF);
			f.code = (context, partialResult) => {
				var self = context.Self;
				random ??= new Random();
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
				return Result.Null;
			};

			// sum
			//	Returns the total of all elements in a list, or all values in a map.
			// self (list or map): object to sum
			// Returns: result of adding up all values in self
			// Example: range(3).sum		returns 6 (3 + 2 + 1 + 0)
			f = Create(SUM);
			f.AddParam(SELF);
			f.code = (context, partialResult) => {
				var val = context.Self;
				double sum = 0;
				switch (val) {
					case ValList valList: {
						sum += valList.Values.Sum(v => v.DoubleValue());
						break;
					}
					case ValMap valMap: {
						sum += valMap.Map.Values.Sum(v => v.DoubleValue());
						break;
					}
				}
				return new Result(sum);
			};

			// tan
			//	Returns the tangent of the given angle (in radians).
			// radians (number): angle, in radians, to get the tangent of
			// Returns: tangent of the given angle
			// Example: tan(pi/4)		returns 1
			f = Create(TAN);
			f.AddParam("radians", 0);
			f.code = (context, partialResult) => new Result(Math.Tan(context.GetVar("radians").DoubleValue()));

			// time
			//	Returns the number of seconds since the script started running.
			f = Create(TIME);
			f.code = (context, partialResult) => new Result(context.Vm.RunTime);
			
			// upper
			//	Return an upper-case (all capitals) version of a string.
			//	May be called with function syntax or dot syntax.
			// self (string): string to upper-case
			// Returns: string with all lowercase letters converted to capitals
			// Example: "Mo Spam".upper		returns "MO SPAM"
			// See also: lower
			f = Create(UPPER);
			f.AddParam(SELF);
			f.code = (context, partialResult) => {
				var val = context.Self;
				if (!(val is ValString)) return new Result(val);
				var str = ((ValString)val).Value;
				return new Result(str.ToUpper());
			};
			
			// val
			//	Return the numeric value of a given string.  (If given a number,
			//	returns it as-is; if given a list or map, returns null.)
			//	May be called with function syntax or dot syntax.
			// self (string or number): string to get the value of
			// Returns: numeric value of the given string
			// Example: "1234.56".val		returns 1234.56
			// See also: str
			f = Create(VAL);
			f.AddParam(SELF, 0);
			f.code = (context, partialResult) => {
				var val = context.Self;
				switch (val) {
					case ValNumber _:
						return new Result(val);
					case ValString _: {
						double.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value);
						return new Result(value);
					}
					default:
						return Result.Null;
				}
			};

            // values
			//	Returns the values of a dictionary, or the characters of a string.
            //  (Returns any other value as-is.)
			//	May be called with function syntax or dot syntax.
			// self (any): object to get the values of.
			// Example: d={1:"one", 2:"two"}; d.values		returns ["one", "two"]
			// Example: "abc".values		returns ["a", "b", "c"]
			// See also: indexes
			f = Create(VALUES);
            f.AddParam(SELF);
            f.code = (context, partialResult) => {
                var self = context.Self;
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
            };

			// version
			//	Get a map with information about the version of Minisript and
			//	the host environment that you're currently running.  This will
			//	include at least the following keys:
			//		miniscript: a string such as "1.5"
			//		buildDate: a date in yyyy-mm-dd format, like "2020-05-28"
			//		host: a number for the host major and minor version, like 0.9
			//		hostName: name of the host application, e.g. "Mini Micro"
			//		hostInfo: URL or other short info about the host app
			f = Create(VERSION);
			var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			f.code = (context, partialResult) => {
				if (context.Vm.VersionMap != null) return new Result(context.Vm.VersionMap);

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

				context.Vm.VersionMap = d;
				return new Result(context.Vm.VersionMap);
			};

			// wait
			//	Pause execution of this script for some amount of time.
			// seconds (default 1.0): how many seconds to wait
			// Example: wait 2.5		pauses the script for 2.5 seconds
			// See also: time, yield
			f = Create(WAIT);
			f.AddParam("seconds", 1);
			f.code = (context, partialResult) => {
				var now = context.Vm.RunTime;
				if (partialResult == null) {
					// Just starting our wait; calculate end time and return as partial result
					var interval = context.GetVar("seconds").DoubleValue();
					return new Result(new ValNumber(now + interval), false);
				} else {
					// Continue until current time exceeds the time in the partial result
					return now > partialResult.ResultValue.DoubleValue() ? Result.Null : partialResult;
				}
			};

			// yield
			//	Pause the execution of the script until the next "tick" of
			//	the host app.  In Mini Micro, for example, this waits until
			//	the next 60Hz frame.  Exact meaning may very, but generally
			//	if you're doing something in a tight loop, calling yield is
			//	polite to the host app or other scripts.
			f = Create(YIELD);
			f.code = (context, partialResult) => {
				context.Vm.Yielding = true;
				return Result.Null;
			};

			ListType[HAS_INDEX] = GetByName(HAS_INDEX).GetFunc();
			ListType[INDEXES] = GetByName(INDEXES).GetFunc();
			ListType[INDEX_OF] = GetByName(INDEX_OF).GetFunc();
			ListType[INSERT] = GetByName(INSERT).GetFunc();
			ListType[JOIN] = GetByName(JOIN).GetFunc();
			ListType[LEN] = GetByName(LEN).GetFunc();
			ListType[POP] = GetByName(POP).GetFunc();
			ListType[PULL] = GetByName(PULL).GetFunc();
			ListType[PUSH] = GetByName(PUSH).GetFunc();
			ListType[SHUFFLE] = GetByName(SHUFFLE).GetFunc();
			ListType[SORT] = GetByName(SORT).GetFunc();
			ListType[SUM] = GetByName(SUM).GetFunc();
			ListType[REMOVE] = GetByName(REMOVE).GetFunc();
			ListType[REPLACE] = GetByName(REPLACE).GetFunc();
			ListType[VALUES] = GetByName(VALUES).GetFunc();

			StringType[HAS_INDEX] = GetByName(HAS_INDEX).GetFunc();
			StringType[INDEXES] = GetByName(INDEXES).GetFunc();
			StringType[INDEX_OF] = GetByName(INDEX_OF).GetFunc();
			StringType[INSERT] = GetByName(INSERT).GetFunc();
			StringType[CODE] = GetByName(CODE).GetFunc();
			StringType[LEN] = GetByName(LEN).GetFunc();
			StringType[LOWER] = GetByName(LOWER).GetFunc();
			StringType[VAL] = GetByName(VAL).GetFunc();
			StringType[REMOVE] = GetByName(REMOVE).GetFunc();
			StringType[REPLACE] = GetByName(REPLACE).GetFunc();
			StringType[SPLIT] = GetByName(SPLIT).GetFunc();
			StringType[UPPER] = GetByName(UPPER).GetFunc();
			StringType[VALUES] = GetByName(VALUES).GetFunc();

			MapType[HAS_INDEX] = GetByName(HAS_INDEX).GetFunc();
			MapType[INDEXES] = GetByName(INDEXES).GetFunc();
			MapType[INDEX_OF] = GetByName(INDEX_OF).GetFunc();
			MapType[LEN] = GetByName(LEN).GetFunc();
			MapType[POP] = GetByName(POP).GetFunc();
			MapType[PUSH] = GetByName(PUSH).GetFunc();
			MapType[SHUFFLE] = GetByName(SHUFFLE).GetFunc();
			MapType[SUM] = GetByName(SUM).GetFunc();
			MapType[REMOVE] = GetByName(REMOVE).GetFunc();
			MapType[REPLACE] = GetByName(REPLACE).GetFunc();
			MapType[VALUES] = GetByName(VALUES).GetFunc();
		}

		// Helper method to compile a call to Slice (when invoked directly via slice syntax).
		public static void CompileSlice(List<Line> code, Value list, Value fromIdx, Value toIdx, int resultTempNum) {
			code.Add(new Line(null, Line.Op.PushParam, list));
			code.Add(new Line(null, Line.Op.PushParam, fromIdx ?? TAC.Num(0)));
			code.Add(new Line(null, Line.Op.PushParam, toIdx));// toIdx == null ? TAC.Num(0) : toIdx));
			var func = GetByName("slice").GetFunc();
			code.Add(new Line(TAC.LTemp(resultTempNum), Line.Op.CallFunctionA, func, TAC.Num(3)));
		}
		
	}
}

