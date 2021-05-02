﻿using Miniscript.sources.intrinsic;

namespace Miniscript {

    /// <summary>
    /// IntrinsicCode is a delegate to the actual C# code invoked by an intrinsic method.
    /// </summary>
    /// <param name="context">TAC.Context in which the intrinsic was invoked</param>
    /// <param name="partialResult">partial result from a previous invocation, if any</param>
    /// <returns>result of the computation: whether it's complete, a partial result if not, and a Value if so</returns>
    public delegate Result IntrinsicCode(TAC.Context context, Result partialResult);


}