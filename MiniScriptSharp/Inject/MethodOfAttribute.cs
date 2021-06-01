using System;

namespace MiniScriptSharp.Inject {
    
    /// <summary>
    /// Attribute for adding functions as methods to the specified types
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MethodOfAttribute : Attribute {  
        public readonly Type Type;  
  
        public MethodOfAttribute(Type type) {  
            Type = type;
        }  
    }

}