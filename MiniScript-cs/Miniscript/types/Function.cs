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

            public string name;
            public Value defaultValue;

            public Param(string name, Value defaultValue) {
                this.name = name;
                this.defaultValue = defaultValue;
            }

        }

        // Function parameters
        public List<Param> parameters;

        // Function code (compiled down to TAC form)
        public List<Line> code;

        public Function(List<Line> code) {
            this.code = code;
            parameters = new List<Param>();
        }

        public string ToString(Machine vm) {
            var s = new System.Text.StringBuilder();
            s.Append("FUNCTION(");
            for (var i = 0; i < parameters.Count(); i++) {
                if (i > 0) s.Append(", ");
                s.Append(parameters[i].name);
                if (parameters[i].defaultValue != null) s.Append("=" + parameters[i].defaultValue.CodeForm(vm));
            }

            s.Append(")");
            return s.ToString();
        }

    }

}