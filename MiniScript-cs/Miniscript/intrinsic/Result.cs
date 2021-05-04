using Miniscript.types;

namespace Miniscript.intrinsic {

		/// <summary>
		/// Result represents the result of an intrinsic call.  An intrinsic will either
		/// be done with its work, or not yet done (e.g. because it's waiting for something).
		/// If it's done, set done=true, and store the result Value in result.
		/// If it's not done, set done=false, and store any partial result in result (and 
		/// then your intrinsic will get invoked with this Result passed in as partialResult).
		/// </summary>
		public class Result {
			public bool done;		// true if our work is complete; false if we need to Continue
			public Value result;	// final result if done; in-progress data if not done
			
			/// <summary>
			/// Result constructor taking a Value, and an optional done flag.
			/// </summary>
			/// <param name="result">result or partial result of the call</param>
			/// <param name="done">whether our work is done (optional, defaults to true)</param>
			public Result(Value result, bool done=true) {
				this.done = done;
				this.result = result;
			}

			/// <summary>
			/// Result constructor for a simple numeric result.
			/// </summary>
			public Result(double resultNum) {
				this.done = true;
				this.result = new ValNumber(resultNum);
			}

			/// <summary>
			/// Result constructor for a simple string result.
			/// </summary>
			public Result(string resultStr) {
				this.done = true;
				this.result = string.IsNullOrEmpty(resultStr) ? ValString.empty : new ValString(resultStr);
			}
			
			/// <summary>
			/// Result.Null: static Result representing null (no value).
			/// </summary>
			public static readonly Result Null = new Result(null, true);

			/// <summary>
			/// Result.EmptyString: static Result representing "" (empty string).
			/// </summary>
			public static readonly Result EmptyString  = new Result(ValString.empty);

			/// <summary>
			/// Result.True: static Result representing true (1.0).
			/// </summary>
			public static readonly Result True = new Result(ValNumber.one, true);

			/// <summary>
			/// Result.True: static Result representing false (0.0).
			/// </summary>
			public static readonly Result False = new Result(ValNumber.zero, true);

			/// <summary>
			/// Result.Waiting: static Result representing a need to wait,
			/// with no in-progress value.
			/// </summary>
			public static readonly Result Waiting = new Result(null, false);

		}

}