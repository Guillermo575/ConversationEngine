using ConversationEngine.ConversationScheme;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace ConversationEngine.GameHelper
{
    public class FunctionHelper
    {
        public void ExecuteFunction(ConversationFunction function)
        {
            Type type = this.GetType();
            MethodInfo method = type.GetMethod(function.MethodName, BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                Console.WriteLine($"Método '{function.MethodName}' no encontrado.");
                return;
            }
            ParameterInfo[] parameterInfos = method.GetParameters();
            if (parameterInfos.Length != function.Parameters.Count)
            {
                Console.WriteLine("Número de parámetros no coincide con el método.");
                return;
            }
            object[] parameters = new object[parameterInfos.Length];
            for (int i = 0; i < parameterInfos.Length; i++)
            {
                ParameterInfo paramInfo = parameterInfos[i];
                string paramName = paramInfo.Name;
                if (function.Parameters.TryGetValue(paramName, out string paramValue))
                {
                    try
                    {
                        parameters[i] = Convert.ChangeType(paramValue, paramInfo.ParameterType);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"No se pudo convertir el parámetro '{paramName}' al tipo '{paramInfo.ParameterType}'.");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"Falta el parámetro '{paramName}' en el diccionario.");
                    return;
                }
            }
            method.Invoke(this, parameters);
        }
        public void MyMethod(string name, int age)
        {
            Console.WriteLine($"Nombre: {name}, Edad: {age}");
        }
    }
}