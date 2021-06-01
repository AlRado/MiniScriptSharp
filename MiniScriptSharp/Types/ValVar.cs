using MiniScriptSharp.Constants;
using MiniScriptSharp.Tac;

namespace MiniScriptSharp.Types {

    public class ValVar : Value {
        
        // Special name for the implicit result variable we assign to on expression statements:
        public static readonly ValVar ImplicitResult = new ValVar("_");

        // Special var for 'self'
        public static readonly ValVar Self = new ValVar(Consts.SELF);

        public string Identifier;
        
        public bool NoInvoke; // reflects use of "@" (address-of) operator

        public ValVar(string identifier) {
            Identifier = identifier;
        }

        public override Value Val(Context context) {
            return this == Self ? context.Self : context.GetVar(Identifier);
        }

        public override Value Val(Context context, out ValMap valueFoundIn) {
            valueFoundIn = null;
            return this == Self ? context.Self : context.GetVar(Identifier);
        }

        public override string ToString(Machine vm) {
            return NoInvoke ? $"@{Identifier}" : Identifier;
        }

        public override int Hash(int recursionDepth = 16) {
            return Identifier.GetHashCode();
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            return rhs is ValVar valVar && valVar.Identifier == Identifier ? 1 : 0;
        }

    }

}