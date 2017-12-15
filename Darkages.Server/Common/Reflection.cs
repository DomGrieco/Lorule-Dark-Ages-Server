﻿using System;
using System.Reflection;

namespace Darkages.Common
{
    public static class Reflection
    {

        public static object Create(string typeAssemblyQualifiedName)
        {
            Type targetType = ResolveType(typeAssemblyQualifiedName);
            if (targetType == null)
                throw new ArgumentException("Unable to resolve object type: " + typeAssemblyQualifiedName);

            return Create(targetType);
        }

        public static T Create<T>()
        {
            Type targetType = typeof(T);
            return (T) Create(targetType);
        }


        public static object Create(Type targetType)
        {
            if (Type.GetTypeCode(targetType) == TypeCode.String)
                return string.Empty;

            Type[] types = new Type[0];
            ConstructorInfo info = targetType.GetConstructor(types);
            object targetObject = null;

            if (info == null)
                if (targetType.BaseType != null && (targetType.BaseType.UnderlyingSystemType.FullName != null && (targetType.BaseType != null && targetType.BaseType.UnderlyingSystemType.FullName.Contains("Enum"))))
                    targetObject = Activator.CreateInstance(targetType);
                else
                    throw new ArgumentException("Unable to instantiate type: " + targetType.AssemblyQualifiedName +
                                                " - Constructor not found");
            else
                targetObject = info.Invoke(null);

            if (targetObject == null)
                throw new ArgumentException("Unable to instantiate type: " + targetType.AssemblyQualifiedName +
                                            " - Unknown Error");
            return targetObject;
        }

        public static Type ResolveType(string typeAssemblyQualifiedName)
        {
            int commaIndex = typeAssemblyQualifiedName.IndexOf(",");
            string className = typeAssemblyQualifiedName.Substring(0, commaIndex).Trim();
            string assemblyName = typeAssemblyQualifiedName.Substring(commaIndex + 1).Trim();

            if (className.Contains("[]"))
                className.Remove(className.IndexOf("[]"), 2);

            Assembly assembly = null;
            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch
            {
                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch
                {
                    throw new ArgumentException("Can't load assembly " + assemblyName);
                }
            }
            return assembly.GetType(className, false, false);
        }
    }
}
