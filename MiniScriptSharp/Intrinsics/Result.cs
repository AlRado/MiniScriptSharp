using MiniScriptSharp.Types;

namespace MiniScriptSharp.Intrinsics {

		/// <summary>
		/// Result represents the result of an intrinsic call.  An intrinsic will either
		/// be Done with its work, or not yet Done (e.g. because it's waiting for something).
		/// If it's Done, set Done=true, and store the result Value in result.
		/// If it's not Done, set Done=false, and store any partial result in result (and 
		/// then your intrinsic will get invoked with this Result passed in as partialResult).
		/// </summary>
		public class Result {

			/// <summary>
			/// Result.Null: static Result representing null (no value).
			/// </summary>
			public static readonly Result Null = new Result(null, true);

			/// <summary>
			/// Result.EmptyString: static Result representing "" (empty string).
			/// </summary>
			public static readonly Result EmptyString = new Result(ValString.Empty);

			/// <summary>
			/// Result.True: static Result representing true (1.0).
			/// </summary>
			public static readonly Result True = new Result(ValNumber.One, true);

			/// <summary>
			/// Result.True: static Result representing false (0.0).
			/// </summary>
			public static readonly Result False = new Result(ValNumber.Zero, true);

			/// <summary>
			/// Result.Waiting: static Result representing a need to wait,
			/// with no in-progress value.
			/// </summary>
			public static readonly Result Waiting = new Result(null, false);
			
			public readonly bool Done;		// true if our work is complete; false if we need to Continue
			public readonly Value ResultValue;	// final result if Done; in-progress data if not Done
			
			/// <summary>
			/// Result constructor taking a Value, and an optional Done flag.
			/// </summary>
			/// <param name="result">result or partial result of the call</param>
			/// <param name="done">whether our work is Done (optional, defaults to true)</param>
			public Result(Value result, bool done=true) {
				this.Done = done;
				this.ResultValue = result;
			}

			/// <summary>
			/// Result constructor for a simple numeric result.
			/// </summary>
			public Result(double resultNum) {
				this.Done = true;
				this.ResultValue = new ValNumber(resultNum);
			}

			/// <summary>
			/// Result constructor for a simple string result.
			/// </summary>
			public Result(string resultStr) {
				this.Done = true;
				this.ResultValue = string.IsNullOrEmpty(resultStr) ? ValString.Empty : new ValString(resultStr);
			}
		}
}