using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using MiniScriptSharp.Constants;
using MiniScriptSharp.Intrinsics;
using MiniScriptSharp.Tac;
using MiniScriptSharp.Types;

namespace MiniScriptSharp.Inject {
    
    /// <summary>
    /// Static class for embed public methods from instance of target class into MiniScript
    /// You can specify log output to see information about all injected functions
    /// </summary>
    public static class FunctionInjector {

        public static Context Context { get; private set; }

        public static Result PartialResult { get; private set; }

        public static void AddFunctions(object classInstance, Action<string> logOutput = null) {
            var timer  = new Stopwatch();
            timer.Start();
            
            logOutput?.Invoke($"Function injector added functions:");
            
            var methods = classInstance.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            foreach (var info in methods) {
                var nameChars = info.Name.ToCharArray();
                nameChars[0] = char.ToLower(nameChars[0]);
                var methodName = new string(nameChars);

                var function = Intrinsic.Create(methodName);
                var msg = $"\t{GetAlias(info.ReturnType)} {methodName}(";
                var isFirstParam = true;
                foreach (var parameter in info.GetParameters()) {
                    if (!isFirstParam) msg += ", ";
                    isFirstParam = false;
                    msg += $"{GetAlias(parameter.ParameterType)} {parameter.Name}";

                    switch (parameter.ParameterType) {
                        case Type d when ReferenceEquals(d, typeof(double)):
                        case Type f when ReferenceEquals(f, typeof(float)):
                        case Type i when ReferenceEquals(i, typeof(int)):
                            if (parameter.RawDefaultValue is double defaultDouble) {
                                function.AddNumberParam(parameter.Name, defaultDouble);
                                msg += $" = {defaultDouble}";
                            } else {
                                function.AddNumberParam(parameter.Name);
                            }
                            break;
                        case Type dNullable when ReferenceEquals(dNullable, typeof(double?)):
                        case Type fNullable when ReferenceEquals(fNullable, typeof(float?)):
                        case Type iNullable when ReferenceEquals(iNullable, typeof(int?)):
                            if (parameter.RawDefaultValue is double defaultDoubleNullable) {
                                function.AddNumberParam(parameter.Name, defaultDoubleNullable);
                                msg += $" = {defaultDoubleNullable}";
                            } else {
                                function.AddValueParam(parameter.Name, null); // set null!
                            }
                            break;
                        case Type b when ReferenceEquals(b, typeof(bool)):
                            if (parameter.RawDefaultValue is bool defaultBool) {
                                function.AddBoolParam(parameter.Name, defaultBool);
                                msg += $" = {defaultBool}";
                            } else {
                                function.AddBoolParam(parameter.Name);
                            }
                            break;
                        case Type s when ReferenceEquals(s, typeof(string)):
                            if (parameter.RawDefaultValue is string defaultString) {
                                function.AddStringParam(parameter.Name, defaultString);
                                msg += $" = \"{defaultString}\"";
                            } else {
                                function.AddStringParam(parameter.Name);
                            }
                            break;                        
                        case Type v when ReferenceEquals(v, typeof(Value)):
                            if (parameter.RawDefaultValue is Value defaultValue) {
                                function.AddValueParam(parameter.Name, defaultValue);
                                msg += $" = {defaultValue}";
                            } else {
                                function.AddValueParam(parameter.Name, null); // set null!
                            }
                            break;

                        default:
                            throw new Exception($"ParameterType: {parameter.ParameterType} not supported!");
                    }
                }
                msg += ");"; 
                logOutput?.Invoke(msg);

                function.Сode = (context, partialResult) => {
                    Context = context;
                    PartialResult = partialResult;
                    
                    var parameterInfos = info.GetParameters();
                    var parametersValues = new object[parameterInfos.Length];
                    for (var i = 0; i < parameterInfos.Length; i++) {
                        var paramValue = GetParam(parameterInfos[i], context);
                        parametersValues[i] = paramValue is DBNull ? null : paramValue;
                    }
                    return GetResult(classInstance, info, parametersValues);
                };
                
                AddDefaultMethods(methodName, info);
            }
            logOutput?.Invoke("");
            
            timer.Stop();
            var timeTaken = timer.Elapsed;
            // time may be longer than it actually is due to logOutput?.Invoke()
            // logOutput?.Invoke("Time taken: " + timeTaken.ToString(@"m\:ss\.fff"));
        }
        
        private static object GetParam(ParameterInfo param, Context context) {
            var enabled = context.GetLocal(param.Name) != null;
            switch (param.ParameterType) {
                case Type d when ReferenceEquals(d, typeof(double)): 
                case Type dNullable when ReferenceEquals(dNullable, typeof(double?)): 
                    return enabled ? context.GetLocalDouble(param.Name) : param.RawDefaultValue;
                case Type f when ReferenceEquals(f, typeof(float)): 
                case Type fNullable when ReferenceEquals(fNullable, typeof(float?)): 
                    return enabled ? context.GetLocalFloat(param.Name) : param.RawDefaultValue;
                case Type i when ReferenceEquals(i, typeof(int)): 
                case Type iNullable when ReferenceEquals(iNullable, typeof(int?)):
                    return enabled ? context.GetLocalInt(param.Name) : param.RawDefaultValue;
                case Type b when ReferenceEquals(b, typeof(bool)): 
                    return enabled ? context.GetLocalBool(param.Name) : param.RawDefaultValue;
                case Type s when ReferenceEquals(s, typeof(string)): 
                    return enabled ? context.GetLocalString(param.Name) : param.RawDefaultValue;
                case Type v when ReferenceEquals(v, typeof(Value)):
                    return param.Name == Consts.SELF ? context.Self : enabled ? context.GetLocal(param.Name) : param.RawDefaultValue;
                default: throw new Exception($"ParameterType: {param.ParameterType} not supported!");
            }
        }

        private static Result GetResult(object classInstance, MethodInfo method, object[] parameters) {
            switch (method.ReturnType) {
                case Type d when ReferenceEquals(d, typeof(double)):
                    return new Result((double) method.Invoke(classInstance, parameters));
                case Type f when ReferenceEquals(f, typeof(float)):
                    return new Result((float) method.Invoke(classInstance, parameters));
                case Type i when ReferenceEquals(i, typeof(int)):
                    return new Result((int) method.Invoke(classInstance, parameters));
                case Type b when ReferenceEquals(b, typeof(bool)):
                    return (bool) method.Invoke(classInstance, parameters) ? Result.True : Result.False;
                case Type s when ReferenceEquals(s, typeof(string)):
                    return new Result((string) method.Invoke(classInstance, parameters));
                case Type tVoid when ReferenceEquals(tVoid, typeof(void)): {
                    method.Invoke(classInstance, parameters); 
                    return Result.Null;
                }
                case Type result when ReferenceEquals(result, typeof(Result)):
                    return (Result) method.Invoke(classInstance, parameters);
                default: throw new Exception($"Returned Type: {method.ReturnType} not supported!");
            };
        }
        
        private static string GetAlias(Type t) {
            return TypeAliases.TryGetValue(t, out var alias) ? alias : t.ToString();
        }
        
        public static void AddDefaultMethods(string name, MethodInfo method) {
            var valFunc = Intrinsic.GetByName(name).GetFunc();
            foreach (var attribute in Attribute.GetCustomAttributes(method)) {
                if (attribute.GetType() == typeof(MethodOfAttribute)) {
                    var methodOfAttribute = (MethodOfAttribute)attribute;
                    var map = GetMap(methodOfAttribute.Type);
                    map[name] = valFunc;
                }

                if (attribute.GetType() == typeof(DescriptionAttribute)) {
                    var descriptionAttribute = (DescriptionAttribute)attribute;
                    Intrinsic.AddDescription(name, $" {valFunc}{descriptionAttribute.Description}");
                }
                
                if (attribute.GetType() == typeof(CategoryAttribute)) {
                    var categoryAttribute = (CategoryAttribute)attribute;
                    Intrinsic.AddCategory(valFunc.ToString(), categoryAttribute.Category);
                }
            }

            if (Attribute.GetCustomAttribute(method, typeof(CategoryAttribute)) == null)
                Intrinsic.AddCategory(valFunc.ToString(), Consts.NONE);
        }

        private static ValMap GetMap(Type type) {
            return type switch {
                Type valFunction when ReferenceEquals(valFunction, typeof(ValFunction)) => Intrinsic.FunctionType,
                Type valList when ReferenceEquals(valList, typeof(ValList)) => Intrinsic.ListType,
                Type valString when ReferenceEquals(valString, typeof(ValString)) => Intrinsic.StringType,
                Type valMap when ReferenceEquals(valMap, typeof(ValMap)) => Intrinsic.MapType,
                Type valNumber when ReferenceEquals(valNumber, typeof(ValNumber)) => Intrinsic.NumberType,
                _ => throw new Exception($"Type: {type} not supported!")
            };
        }
        
        private static readonly Dictionary<Type, string> TypeAliases = new Dictionary<Type, string> {
            {typeof(byte), "byte"},
            {typeof(sbyte), "sbyte"},
            {typeof(short), "short"},
            {typeof(ushort), "ushort"},
            {typeof(int), "int"},
            {typeof(uint), "uint"},
            {typeof(long), "long"},
            {typeof(ulong), "ulong"},
            {typeof(float), "float"},
            {typeof(double), "double"},
            {typeof(decimal), "decimal"},
            {typeof(object), "object"},
            {typeof(bool), "bool"},
            {typeof(char), "char"},
            {typeof(string), "string"},
            {typeof(void), "void"},
            {typeof(byte?), "byte?"},
            {typeof(sbyte?), "sbyte?"},
            {typeof(short?), "short?"},
            {typeof(ushort?), "ushort?"},
            {typeof(int?), "int?"},
            {typeof(uint?), "uint?"},
            {typeof(long?), "long?"},
            {typeof(ulong?), "ulong?"},
            {typeof(float?), "float?"},
            {typeof(double?), "double?"},
            {typeof(decimal?), "decimal?"},
            {typeof(bool?), "bool?"},
            {typeof(char?), "char?"},
            {typeof(Result), "Result"},
            {typeof(Value), "Value"},
            {typeof(ValList), "ValList"},
            {typeof(ValMap), "ValMap"},
            {typeof(ValFunction), "ValFunction"},
            {typeof(ValString), "ValString"}
        };

    }

}