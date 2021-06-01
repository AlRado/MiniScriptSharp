using MiniScriptSharp.Tac;

namespace MiniScriptSharp.Types {

    /// <summary>
    /// ValFunction: a Value that is, in fact, a Function.
    /// </summary>
    public class ValFunction : Value {

        public Function Function;
        public readonly ValMap OuterVars; // local variables where the function was defined (usually, the module)

        public ValFunction(Function function) {
            Function = function;
        }

        public ValFunction(Function function, ValMap outerVars) {
            Function = function;
            OuterVars = outerVars;
        }

        public override string ToString(Machine vm) {
            return Function.ToString(vm);
        }

        public override bool BoolValue() {
            // A function value is ALWAYS considered true.
            return true;
        }

        public override bool IsA(Value type, Machine vm) {
            return type == vm.FunctionType;
        }

        public override int Hash(int recursionDepth = 16) {
            return Function.GetHashCode();
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            // Two Function values are equal only if they refer to the exact same function
            if (!(rhs is ValFunction other)) return 0;
            return Function == other.Function ? 1 : 0;
        }

        public ValFunction BindAndCopy(ValMap contextVariables) {
            return new ValFunction(Function, contextVariables);
        }

    }

}