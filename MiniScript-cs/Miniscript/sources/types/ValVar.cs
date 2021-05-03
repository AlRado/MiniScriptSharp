using Miniscript.sources.tac;

namespace Miniscript.sources.types {

    public class ValVar : Value {
        
        // Special name for the implicit result variable we assign to on expression statements:
        public static ValVar implicitResult = new ValVar("_");

        // Special var for 'self'
        public static ValVar self = new ValVar("self");

        public string identifier;
        
        public bool noInvoke; // reflects use of "@" (address-of) operator

        public ValVar(string identifier) {
            this.identifier = identifier;
        }

        public override Value Val(Context context) {
            if (this == self) return context.self;
            return context.GetVar(identifier);
        }

        public override Value Val(Context context, out ValMap valueFoundIn) {
            valueFoundIn = null;
            if (this == self) return context.self;
            return context.GetVar(identifier);
        }

        public override string ToString(Machine vm) {
            if (noInvoke) return "@" + identifier;
            return identifier;
        }

        public override int Hash(int recursionDepth = 16) {
            return identifier.GetHashCode();
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            return rhs is ValVar && ((ValVar) rhs).identifier == identifier ? 1 : 0;
        }

    }

}