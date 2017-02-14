using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WPO.Enums;

namespace WPO.Helpers
{
    internal static class BasicMapper
    {
        internal static WPOBaseObject MapPropertiesToObject(WPOBaseObject obj, Dictionary<PropertyInfo, string> values)
        {
            foreach (KeyValuePair<PropertyInfo, string> prop in values)
            {
                if (string.IsNullOrEmpty(prop.Value))
                {
                    prop.Key.SetValue(obj, null);
                }
                else if (prop.Key.PropertyType.IsGenericType)
                {
                    SetPropertyValue(obj, prop, prop.Key.PropertyType.GenericTypeArguments.First().Name);
                }
                else
                {
                    SetPropertyValue(obj, prop, prop.Key.PropertyType.Name);
                }
            }

            return obj;
        }

        private static void SetPropertyValue(WPOBaseObject obj, KeyValuePair<PropertyInfo, string> prop, string propertyname)
        {
            switch (propertyname)
            {
                case "Int32":
                    prop.Key.SetValue(obj, int.Parse(prop.Value));
                    break;
                case "Int16":
                    prop.Key.SetValue(obj, short.Parse(prop.Value));
                    break;
                case "Int64":
                    prop.Key.SetValue(obj, long.Parse(prop.Value));
                    break;
                case "Double":
                    prop.Key.SetValue(obj, double.Parse(prop.Value));
                    break;
                case "Byte":
                    prop.Key.SetValue(obj, byte.Parse(prop.Value));
                    break;
                case "String":
                    prop.Key.SetValue(obj, prop.Value);
                    break;
                case "Char":
                    prop.Key.SetValue(obj, char.Parse(prop.Value));
                    break;
                case "Boolean":
                    prop.Key.SetValue(obj, bool.Parse(prop.Value));
                    break;
                case "Decimal":
                    prop.Key.SetValue(obj, decimal.Parse(prop.Value));
                    break;
                case "Single":
                    prop.Key.SetValue(obj, float.Parse(prop.Value));
                    break;
                case "DateTime":
                    prop.Key.SetValue(obj, DateTime.Parse(prop.Value));
                    break;
                case "Money":
                    prop.Key.SetValue(obj, decimal.Parse(prop.Value));
                    break;
                default:
                    throw new NotSupportedException("property name: '" + propertyname + "'");
            }
        }        
    }
}
