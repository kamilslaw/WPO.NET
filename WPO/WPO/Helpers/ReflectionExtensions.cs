using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WPO.Helpers
{
    internal static class ReflectionExtensions
    {
        internal static T GetAttribute<T>(this MemberInfo property) 
            where T : Attribute
        {
            return property.GetCustomAttributes(typeof(T)).SingleOrDefault() as T;
        }

        internal static List<PropertyInfo> GetPropertiesByAttribute<T>(this object obj)
            where T : Attribute
        {
            return obj.GetType().GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(T))).ToList();
        }

        internal static WPOBaseObject CreateModelObj(this Type type)
        {
            return (WPOBaseObject)Activator.CreateInstance(type, new object[] { Session.Empty });
        }
    }
}
