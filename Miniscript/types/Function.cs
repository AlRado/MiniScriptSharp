using System.Collections.Generic;
using System.Linq;
using Miniscript.intrinsic;
using Miniscript.tac;

namespace Miniscript.types {

    /// <summary>
    /// Function: our internal representation of a Minisript function.  This includes
    /// its parameters and its code.  (It does not include a name -- functions don't 
    /// actually HAVE names; instead there are named variables whose value may happen 
    /// to be a function.)
    /// </summary>
    public class Function {

        public string Name;

        // Function parameters
        public List<Param> Parameters;

        // Function code (compiled down to TAC form)
        public List<Line> Code;

        public Function(List<Line> code, string name = "") {
            Code = code;
            Parameters = new List<Param>();
            Name = name;
        }

        public string ToString(Machine vm) {
            var s = new System.Text.StringBuilder();
            var name = string.IsNullOrEmpty(Name) ? Consts.FUNCTION : Name;
            s.Append($"{name}(");
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