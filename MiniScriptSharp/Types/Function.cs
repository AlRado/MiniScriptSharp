using System.Collections.Generic;
using System.Linq;
using MiniScriptSharp.Intrinsics;
using MiniScriptSharp.Tac;

namespace MiniScriptSharp.Types {

    /// <summary>
    /// Function: our internal representation of a MiniScript function.
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
            var name = string.IsNullOrEmpty(Name) ? "FUNCTION" : Name;
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