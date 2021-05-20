using System;

namespace Miniscript.intrinsic {
    
    /*
     * Attribute for adding functions as methods to the specified types
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MethodOfAttribute : Attribute {  
        public readonly Type Type;  
  
        public MethodOfAttribute(Type type) {  
            Type = type;
        }  
    }

}