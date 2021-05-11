using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CSharp;

namespace Miniscript.intrinsic {

    public static class FunctionInjector {

        public static void AddFunctions(object classInstance) {
            Console.WriteLine($"Function injector added functions:");
            
            var methods = classInstance.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            foreach (var info in methods) {
                var nameChars = info.Name.ToCharArray();
                nameChars[0] = char.ToLower(nameChars[0]);
                var methodName = new string(nameChars);
                
                var function = Intrinsic.Create(methodName);
                var msg = $"\t{GetAlias(info.ReturnType)} {methodName} (";
                var firstParam = true;
                foreach (var parameter in info.GetParameters()) {
                    if (!firstParam) msg += ", ";
                    firstParam = false;
                    msg += $"{GetAlias(parameter.ParameterType)} {parameter.Name}";

                    switch (parameter.ParameterType) {
                        case Type d when ReferenceEquals(d, typeof(double)):
                        case Type f when ReferenceEquals(f, typeof(float)):
                        case Type i when ReferenceEquals(i, typeof(int)):
                        case Type b when ReferenceEquals(b, typeof(bool)):
                            if (parameter.RawDefaultValue is double defaultDouble) {
                                function.AddDoubleParam(parameter.Name, defaultDouble);
                                msg += $" = {defaultDouble}";
                            } else {
                                function.AddDoubleParam(parameter.Name);
                            }
                            break;

                        case Type s when ReferenceEquals(s, typeof(string)):
                            if (parameter.RawDefaultValue is string defaultString) {
                                function.AddStringParam(parameter.Name, defaultString);
                                msg += $" = {defaultString}";
                            } else {
                                function.AddStringParam(parameter.Name);
                            }
                            break;

                        default:
                            throw new Exception($"ParameterType: {parameter.ParameterType} not supported!");
                    }
                }
                msg += ");"; 
                Console.WriteLine(msg);

                function.Сode = (context, partialResult) => {
                    var parametersValues = new List<object>();

                    foreach (var parameter in info.GetParameters()) {
                        var paramEnabled = context.GetVar(parameter.Name) != null;
                        // Console.WriteLine($"parameter.Name: {parameter.Name}, paramEnabled: {paramEnabled}");

                        switch (parameter.ParameterType) {
                            case Type d when ReferenceEquals(d, typeof(double)):
                            case Type f when ReferenceEquals(f, typeof(float)):
                            case Type i when ReferenceEquals(i, typeof(int)):
                            case Type b when ReferenceEquals(b, typeof(bool)):
                                parametersValues.Add(paramEnabled
                                    ? context.GetLocalDouble(parameter.Name)
                                    : parameter.RawDefaultValue);
                                break;

                            case Type s when ReferenceEquals(s, typeof(string)):
                                parametersValues.Add(paramEnabled
                                    ? context.GetLocalString(parameter.Name)
                                    : parameter.RawDefaultValue);
                                break;
                            default:
                                throw new Exception($"ParameterType: {parameter.ParameterType} not supported!");
                        }
                    }

                    return GetResult(classInstance, info.Name, info.ReturnType, parametersValues.ToArray());
                };
            }
        }

        private static Result GetResult(object classInstance, string methodName, Type returnedType,
            object[] parameters) {
            var method = classInstance.GetType().GetMethod(methodName);
            switch (returnedType) {
                case Type d when ReferenceEquals(d, typeof(double)):
                case Type f when ReferenceEquals(f, typeof(float)):
                case Type i when ReferenceEquals(i, typeof(int)):
                case Type b when ReferenceEquals(b, typeof(bool)):
                    return new Result((double) method.Invoke(classInstance, parameters));

                case Type s when ReferenceEquals(s, typeof(string)):
                    return new Result((string) method.Invoke(classInstance, parameters));

                default:
                    throw new Exception($"Returned Type: {returnedType} not supported!");
            }
        }
        
        private static string GetAlias(Type t) {
            using var provider = new CSharpCodeProvider();
            var typeRef = new CodeTypeReference(t);
            var typeName = provider.GetTypeOutput(typeRef);
            
            return typeName;
        }

    }

}