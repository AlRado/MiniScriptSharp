using MiniScriptSharp.Tac;

namespace MiniScriptSharp.Intrinsics {

    /// <summary>
    /// IntrinsicCode is a delegate to the actual C# code invoked by an intrinsic method.
    /// </summary>
    /// <param name="context">Context in which the intrinsic was invoked</param>
    /// <param name="partialResult">partial result from a previous invocation, if any</param>
    /// <returns>result of the computation: whether it's complete, a partial result if not, and a Value if so</returns>
    public delegate Result IntrinsicCode(Context context, Result partialResult);


}