/*
 * Intrinsic.cs
 * 
 * This file defines the Intrinsic class, which represents a built-in function
 * available to MiniScript code.  All intrinsics are held in static storage, so
 * this class includes static functions such as GetByName to look up 
 * already-defined intrinsics.  See Chapter 2 of the MiniScript Integration
 * Guide for details on adding your own intrinsics.
 * 
 * This file also contains the Intrinsics static class, where all of the standard
 * intrinsics are defined.  This is initialized automatically, so normally you
 * don’t need to worry about it, though it is a good place to look for examples
 * of how to write intrinsic functions.
 * 
 * Note that you should put any intrinsics you add in a separate file; leave the
 * MiniScript source files untouched, so you can easily replace them when updates
 * become available.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniScriptSharp.Inject;
using MiniScriptSharp.Tac;
using MiniScriptSharp.Types;
using static MiniScriptSharp.Constants.Consts;

namespace MiniScriptSharp.Intrinsics {
		
	/// <summary>
	/// Intrinsic: represents an intrinsic function available to MiniScript code.
	/// </summary>
	public class Intrinsic {
		
		/// <summary>
		/// FunctionType: a static map that represents the Function type.
		/// </summary>
		public static readonly ValMap FunctionType = new ValMap();

		/// <summary>
		/// ListType: a static map that represents the List type, and provides
		/// intrinsic methods that can be invoked on it via dot syntax.
		/// </summary>
		public static readonly ValMap ListType = new ValMap();

		/// <summary>
		/// StringType: a static map that represents the String type, and provides
		/// intrinsic methods that can be invoked on it via dot syntax.
		/// </summary>
		public static readonly ValMap StringType = new ValMap();

		/// <summary>
		/// MapType: a static map that represents the Map type, and provides
		/// intrinsic methods that can be invoked on it via dot syntax.
		/// </summary>
		public static readonly ValMap MapType = new ValMap();

		/// <summary>
		/// NumberType: a static map that represents the Number type.
		/// </summary>
		public static readonly ValMap NumberType = new ValMap();

		// static map from Values to short names, used when displaying lists/maps;
		// feel free to add to this any values (especially lists/maps) provided
		// by your own intrinsics.
		public static readonly Dictionary<Value, string> ShortNames = new Dictionary<Value, string>();
		
		private static readonly List<Intrinsic> all = new List<Intrinsic>() { null };

		private static readonly Dictionary<string, Intrinsic> nameMap = new Dictionary<string, Intrinsic>();
		
		private static readonly Dictionary<string, string> descriptionMap = new Dictionary<string, string>();
		private static readonly Dictionary<string, string> categoryMap = new Dictionary<string, string>();

		// a numeric ID (used internally -- don't worry about this)
		public int Id { get; private set; }
		
		// name of this intrinsic (should be a valid MiniScript identifier)
		public string Name;
		
		// actual C# code invoked by the intrinsic
		public IntrinsicCode Сode;
		
		private Function function;
		private ValFunction valFunction;	// (cached wrapper for function)
		
		private readonly ValString _self = new ValString(SELF);

		/// <summary>
		/// Intrinsic static constructor: called automatically during script setup to make sure
		/// that all our standard intrinsics are defined.
		/// </summary>
		static Intrinsic() {
			FunctionInjector.AddFunctions(new IntrinsicFunctions(), Console.WriteLine);

			// You can use the manual method to add intrinsic functions.
			// This method is not as convenient as adding functions using FunctionInjector,
			// but in some cases it can give a performance boost because in this case reflection will not be used 
			var intrinsicFunc = Create("wait");
			intrinsicFunc.AddDoubleParam("seconds", 1);
			intrinsicFunc.Сode = (context, partialResult) => {
				var now = context.Vm.RunTime;
				if (partialResult == null) {
					// Just starting our wait; calculate end time and return as partial result
					var interval = context.GetVar("seconds").DoubleValue();
					return new Result(new ValNumber(now + interval), false);
				} else {
					// Continue until current time exceeds the time in the partial result
					if (now > partialResult.ResultValue.DoubleValue()) return Result.Null;
					return partialResult;
				}
			};
			AddDescription(intrinsicFunc,
				"\n   Pause execution of this script for some amount of time." +
				"\n seconds (default 1.0): how many seconds to wait" +
				"\n Example: wait 2.5		pauses the script for 2.5 seconds" +
				"\n See also: time, yield" +
				"\n"
			);
			AddCategory(intrinsicFunc, INTRINSIC);
			
			// You can also use the manual method to
			// provides intrinsic methods that can be invoked on it via dot syntax:
			
			// ListType["hasIndex"] = GetByName("hasIndex").GetFunc();
			// StringType["hasIndex"] = GetByName("hasIndex").GetFunc();
			// MapType["hasIndex"] = GetByName("hasIndex").GetFunc();
		}
		
		/// <summary>
		/// Factory method to create a new Intrinsic, filling out its name as given,
		/// and other internal properties as needed.  You'll still need to add any
		/// parameters, and define the code it runs.
		/// </summary>
		/// <param name="name">intrinsic name</param>
		/// <returns>freshly minted (but empty) static Intrinsic</returns>
		public static Intrinsic Create(string name) {
			var result = new Intrinsic {Name = name, Id = all.Count, function = new Function(null, name: name)};
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

		public static string GetAllIntrinsicInfo() {
			var sb = new StringBuilder();
			var sortedAll = all.OrderBy(x => x?.Name);
			foreach (var intrinsic in sortedAll) {
				sb.Append($"{intrinsic?.GetFunc()}\n");
			}
			return sb.ToString();
		}
		
		/// <summary>
		/// Look up an Intrinsic by its name.
		/// </summary>
		public static Intrinsic GetByName(string name) {
			return nameMap.TryGetValue(name, out var result) ? result : null;
		}
		
		/// <summary>
		/// Helper method to compile a call to Slice (when invoked directly via slice syntax).
		/// </summary>
		public static void CompileSlice(List<Line> code, Value list, Value fromIdx, Value toIdx, int resultTempNum) {
			code.Add(new Line(null, Op.PushParam, list));
			code.Add(new Line(null, Op.PushParam, fromIdx ?? TAC.Num(0)));
			code.Add(new Line(null, Op.PushParam, toIdx));// toIdx == null ? TAC.Num(0) : toIdx));
			var func = GetByName(SLICE).GetFunc();
			code.Add(new Line(TAC.LTemp(resultTempNum), Op.CallFunctionA, func, TAC.Num(3)));
		}

		/// <summary>
		/// Internally-used function to execute an intrinsic (by ID) given a
		/// context and a partial result.
		/// </summary>
		public static Result Execute(int id, Context context, Result partialResult) {
			var item = GetByID(id);
			return item.Сode(context, partialResult);
		}
		
		/// <summary>
		/// Add a parameter to this Intrinsic, optionally with a default value
		/// to be used if the user doesn't supply one.  You must add parameters
		/// in the same order in which arguments must be supplied.
		/// </summary>
		/// <param name="name">parameter name</param>
		/// <param name="defaultValue">default value, if any</param>
		public void AddValueParam(string name, Value defaultValue = default) {
			function.Parameters.Add(new Param(name, defaultValue));
		}
		
		/// <summary>
		/// Add a parameter with a numeric default value.  (See comments on
		/// the first version of AddParam above.)
		/// </summary>
		/// <param name="name">parameter name</param>
		/// <param name="defaultValue">default value for this parameter</param>
		public void AddNumberParam(string name, double defaultValue = default) {
			Value defVal = defaultValue switch {
				0 => ValNumber.Zero,
				1 => ValNumber.One,
				_ => TAC.Num(defaultValue)
			};
			AddValueParam(name, defVal);
		}
		
		public void AddIntParam(string name, int defaultValue = default) {
			AddDoubleParam(name, defaultValue);
		}
		
		public void AddFloatParam(string name, float defaultValue = default) {
			AddDoubleParam(name, defaultValue);
		}
		
		public void AddDoubleParam(string name, double defaultValue = default) {
			AddNumberParam(name, defaultValue);
		}

		public void AddBoolParam(string name, bool defaultValue = default) {
			AddDoubleParam(name, defaultValue ? 1 : 0);
		}

		/// <summary>
		/// Add a parameter with a string default value.  (See comments on
		/// the first version of AddParam above.)
		/// </summary>
		/// <param name="name">parameter name</param>
		/// <param name="defaultValue">default value for this parameter</param>
		public void AddStringParam(string name, string defaultValue = default) {
			Value defVal;
			if (string.IsNullOrEmpty(defaultValue)) {
				defVal = ValString.Empty;
			} else {
				defVal = defaultValue switch {
					IS_A => ValString.MagicIsA,
					SELF => _self,
					_ => new ValString(defaultValue)
				};
			}
			AddValueParam(name, defVal);
		}
		
		/// <summary>
		/// GetFunc is used internally by the compiler to get the MiniScript function
		/// that makes an intrinsic call.
		/// </summary>
		public ValFunction GetFunc() {
			if (function.Code == null) {
				// Our little wrapper function is a single opcode: CallIntrinsicA.
				// It really exists only to provide a local variable context for the parameters.
				function.Code = new List<Line>();
				function.Code.Add(new Line(TAC.LTemp(0), Op.CallIntrinsicA, TAC.Num(Id)));
			}
			return valFunction;
		}

		public static void AddDescription(Intrinsic intrinsic, string description) {
			AddDescription(intrinsic.function.Name, description);
		}
		
		public static void AddDescription(string functionName, string description) {
			if (string.IsNullOrEmpty(functionName)) return;
			
			descriptionMap[functionName] = description;
		}

		public static string GetDescription(string functionName) {
			if (string.IsNullOrEmpty(functionName)) 
				return GetDescription(HELP);
			
			return descriptionMap.TryGetValue(functionName, out var description) ? 
				description : 
				$"Help description for function \"{functionName}\" not founded!";
		}

		public static void AddCategory(Intrinsic intrinsic, string category) {
			AddCategory(intrinsic.function.ToString(), category);
		}

		public static void AddCategory(string functionSignature, string category) {
			if (string.IsNullOrEmpty(category)) return;

			var functionInfo = $"\n {functionSignature}";
			if (categoryMap.TryGetValue(category, out var categoryInfo)) {
				categoryMap[category] += functionInfo;
			} else {
				categoryMap[category] = functionInfo;
			}
		}

		public static string GetCategory(string category) {
			if (string.IsNullOrEmpty(category)) 
				return GetDescription(CATEGORY);
			
			return categoryMap.TryGetValue(category, out var signatures) ? 
				signatures : 
				$"Functions not found in the category \"{category}\"!";
		}
		
		public static string GetAllCategoriesInfo() {
			var result = "";
			foreach (var name in categoryMap.Keys) {
				result += $"\n {name}";
			}
			return result;
		}

	}
}

