namespace MiniScriptSharp.Types {

    /// <summary>
    /// Param: helper class representing a function parameter.
    /// </summary>
    public class Param {

        public readonly string Name;
        public readonly Value DefaultValue;

        public Param(string name, Value defaultValue) {
            Name = name;
            DefaultValue = defaultValue;
        }

    }

}