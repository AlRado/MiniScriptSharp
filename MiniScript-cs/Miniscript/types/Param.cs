namespace Miniscript.types {

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

}