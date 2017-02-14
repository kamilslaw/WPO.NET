using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WPO.Attributes;
using WPO.Helpers;

namespace WPO.Schemas
{
    public static class WPCreator
    {
        private static List<TableSchema> PrepareSchema(string namespc, Assembly assembly)
        {
            List<Type> tables = assembly.GetTypes().Where(t => string.Equals(t.Namespace, @namespc, StringComparison.Ordinal)).ToList();            
            List<TableSchema> list = new List<TableSchema>();
            foreach (Type t in tables)
            {                
                WPOTableAttribute tableAttribute = t.GetCustomAttributes<WPOTableAttribute>().SingleOrDefault();
                if (tableAttribute != null)
                {
                    bool checkInheritance = false;
                    TableSchema tmp = new TableSchema();
                    tmp.Name = tableAttribute.TableName ?? t.Name.ToLower();
                    tmp.InType = tableAttribute.Inheritance;
                    if (tmp.InType == Enums.InheritanceType.ClassTable) checkInheritance = true;                
                    List<PropertyInfo> props = (tmp.InType == Enums.InheritanceType.ClassTable) ? t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList() : t.GetProperties().ToList();
                    foreach (PropertyInfo prop in props)
                    {
                        WPOColumnAttribute attribute = prop.GetCustomAttributes<WPOColumnAttribute>().SingleOrDefault(); 
                        if (attribute != null)
                        {
                            bool allowNull = false;
                            Type propType = prop.PropertyType;
                            if (Nullable.GetUnderlyingType(propType) != null)
                            {
                                allowNull = true;
                                propType = Nullable.GetUnderlyingType(propType);
                            }
                            int? length = null;
                            int? fractionalPartLength = null;
                            WPOSizeAttribute sizeAttribute = prop.GetCustomAttributes<WPOSizeAttribute>().SingleOrDefault();
                            if(sizeAttribute != null)
                            {
                                length = sizeAttribute.Length;
                                fractionalPartLength = sizeAttribute.FractionalPartLength;
                            }
                            string colName = attribute.ColumnName ?? prop.Name.ToLower();
                            if (attribute.GetType() == typeof(WPOPrimaryKeyAttribute))
                            {
                                if(!checkInheritance)
                                    tmp.Columns.Add(new TableSchema.ColumnSchema
                                    {
                                        IsPrimaryKey = true,
                                        Name = colName, ColType = Converter.TypeToEnum(propType),
                                        AllowNull = allowNull,
                                        Length = length,
                                        FractionalPartLength = fractionalPartLength
                                    });
                                else
                                {
                                    List<PropertyInfo> baseProps = t.BaseType.GetProperties().ToList();
                                    foreach(PropertyInfo baseProp in baseProps)
                                    {
                                        WPOColumnAttribute attr = baseProp.GetCustomAttributes<WPOColumnAttribute>().SingleOrDefault();
                                        if (attr != null)
                                        {
                                            if (attr.GetType() == typeof(WPOPrimaryKeyAttribute))
                                            {
                                                string foreigntablename = t.BaseType.GetCustomAttributes<WPOTableAttribute>().SingleOrDefault().TableName ?? t.BaseType.Name.ToLower();
                                                string foreignColName = baseProp.GetCustomAttributes<WPOColumnAttribute>().SingleOrDefault().ColumnName ?? baseProp.Name.ToLower();
                                                tmp.Columns.Add(new TableSchema.ColumnSchema
                                                {
                                                    IsPrimaryKey = true,
                                                    Name = colName,
                                                    ColType = Converter.TypeToEnum(propType),
                                                    AllowNull = allowNull,
                                                    ForeignKeyName = foreignColName,
                                                    HasForeignKey = true,
                                                    ForeignTableName = foreigntablename,
                                                    Length = length,
                                                    FractionalPartLength = fractionalPartLength
                                                });
                                            }
                                        }
                                    }                                    
                                }
                            }
                            else if (attribute.GetType() == typeof(WPORelationAttribute))
                            {
                                List<PropertyInfo> foreignProps = propType.GetProperties().ToList();
                                WPO.Enums.ColumnType foreigntype = Enums.ColumnType.BOOL;
                                string foreigntablename = prop.PropertyType.GetCustomAttributes<WPOTableAttribute>().SingleOrDefault().TableName ?? prop.PropertyType.Name.ToLower();
                                foreach (PropertyInfo fprop in foreignProps)
                                {
                                    WPOColumnAttribute attr = fprop.GetCustomAttributes<WPOColumnAttribute>().SingleOrDefault();
                                    string foreignColName = attr.ColumnName ?? fprop.Name.ToLower();
                                    if (attr != null && foreignColName == ((WPORelationAttribute)attribute).ForeignKey)
                                    {
                                        foreigntype = Converter.TypeToEnum(fprop.PropertyType);
                                        break;
                                    }                                    
                                }                                                        
                                tmp.Columns.Add(new TableSchema.ColumnSchema
                                {
                                    IsPrimaryKey = false,
                                    Name = colName, HasForeignKey = true,
                                    ForeignKeyName = ((WPORelationAttribute)attribute).ForeignKey,
                                    ColType = foreigntype,
                                    AllowNull = allowNull,
                                    ForeignTableName = foreigntablename,
                                    Length = length,
                                    FractionalPartLength = fractionalPartLength
                                });
                            }
                            else
                            {
                                tmp.Columns.Add(new TableSchema.ColumnSchema
                                {
                                    IsPrimaryKey = false,
                                    Name = colName,
                                    ColType = Converter.TypeToEnum(propType),
                                    AllowNull = allowNull,
                                    Length = length,
                                    FractionalPartLength = fractionalPartLength
                                });
                            }
                        }   
                    }
                    list.Add(tmp);
                }                
            }
            return list;
        }
        
        public static void CreateSchema(Session s, string namespc, Assembly a)
        {
            List<TableSchema> tables = PrepareSchema(namespc,a);
            s.dbConnection.CreateSchema(tables);    
        }
    }
}
