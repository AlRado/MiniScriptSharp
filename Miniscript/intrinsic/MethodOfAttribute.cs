using System;
using Miniscript.types;

namespace Miniscript.intrinsic {
    
    /*
     * Атрибут для добавления функций в качестве методов указанным типам
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MethodOfAttribute : Attribute {  
        public readonly Type Type;  
  
        public MethodOfAttribute(Type type) {  
            Type = type;
        }  
    }

}