using Miniscript.tac;

namespace Miniscript.types {

    public class ValVar : Value {
        
        // Special name for the implicit result variable we assign to on expression statements:
        public static readonly ValVar implicitResult = new ValVar("_");

        // Special var for 'self'
        public static readonly ValVar self = new ValVar("self");

        public string identifier;
        
        public bool noInvoke; // reflects use of "@" (address-of) operator

        public ValVar(string identifier) {
            this.identifier = identifier;
        }

        public override Value Val(Context context) {
            return this == self ? context.self : context.GetVar(identifier);
        }

        public override Value Val(Context context, out ValMap valueFoundIn) {
            valueFoundIn = null;
            return this == self ? context.self : context.GetVar(identifier);
        }

        public override string ToString(Machine vm) {
            return noInvoke ? $"@{identifier}" : identifier;
        }

        public override int Hash(int recursionDepth = 16) {
            return identifier.GetHashCode();
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            return rhs is ValVar valVar && valVar.identifier == identifier ? 1 : 0;
        }

    }

}