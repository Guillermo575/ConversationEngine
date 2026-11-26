using ConversationEngine.ConversationScheme;
using System;
using System.Reflection;
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
                Console.WriteLine($"Method '{function.MethodName}' not found.");
                return;
            }
            ParameterInfo[] parameterInfos = method.GetParameters();
            if (parameterInfos.Length != function.Parameters.Count)
            {
                Console.WriteLine("Number of parameters does not match the method.");
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
                        Console.WriteLine($"Could not convert parameter '{paramName}' to type '{paramInfo.ParameterType}'.");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"Missing parameter '{paramName}' in the dictionary.");
                    return;
                }
            }
            method.Invoke(this, parameters);
        }
        public void MyMethod(string name, int age)
        {
            Console.WriteLine($"Name: {name}, Age: {age}");
        }
    }
}