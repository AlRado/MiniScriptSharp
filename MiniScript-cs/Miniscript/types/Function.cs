using System.Collections.Generic;
using System.Linq;
using Miniscript.tac;

namespace Miniscript.types {

    /// <summary>
    /// Function: our internal representation of a Minisript function.  This includes
    /// its parameters and its code.  (It does not include a name -- functions don't 
    /// actually HAVE names; instead there are named variables whose value may happen 
    /// to be a function.)
    /// </summary>
    public class Function {

        /// <summary>
        /// Param: helper class representing a function parameter.
        /// </summary>
        public class Param {

            public string Name;
            public Value DefaultValue;

            public Param(string name, Value defaultValue) {
                Name = name;
                DefaultValue = defaultValue;
            }

        }

        // Function parameters
        public List<Param> Parameters;

        // Function code (compiled down to TAC form)
        public List<Line> Code;

        public Function(List<Line> code) {
            Code = code;
            Parameters = new List<Param>();
        }

        public string ToString(Machine vm) {
            var s = new System.Text.StringBuilder();
            s.Append("FUNCTION(");
            for (var i = 0; i < Parameters.Count(); i++) {
                if (i > 0) s.Append(", ");
                s.Append(Parameters[i].Name);
                if (Parameters[i].DefaultValue != null) s.Append("=" + Parameters[i].DefaultValue.CodeForm(vm));
            }

            s.Append(")");
            return s.ToString();
        }

    }

}