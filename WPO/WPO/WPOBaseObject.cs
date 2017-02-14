using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WPO.Attributes;
using WPO.Connection;
using WPO.Enums;
using WPO.Exceptions;
using WPO.Helpers;

namespace WPO
{
    public abstract class WPOBaseObject : ICloneable
    {
        internal Dictionary<string, string> DataRow { get; set; }

        public Session Session { get; internal set; }
        public WPOTableObject TableObject { get; internal set; }
        public Guid ObjectGuid { get; internal set; }
        public ObjectStatus Status { get; internal set; }

        public WPOBaseObject(Session session)
        {
            this.Session = session;
            session.Register(this);

            Status = ObjectStatus.New;
            ObjectGuid = Guid.NewGuid();
            TableObject = new WPOTableObject(this);
        }

        #region Overriden Methods

        public override int GetHashCode() => base.GetHashCode();

        public override bool Equals(object obj) => GetIdentityKey() == (obj as WPOBaseObject).GetIdentityKey();

        #endregion Overriden Methods

        #region Public Methods

        public object Clone()
        {
            object clone = Activator.CreateInstance(GetType(), new object[] { Session.Empty });

            foreach (PropertyInfo property in GetType().GetProperties())
            {
                property.SetValue(clone, property.GetValue(this));
            }

            return clone;
        }

        //Identity key depends on object primary kyes and type hash codes
        public int GetIdentityKey() 
        {
            if (Status == ObjectStatus.New)
            {
                return ObjectGuid.GetHashCode();
            }
            else
            {
                int primaryKeysHashcode = (GetPrimaryKey().Value ?? 0).GetHashCode();
                return primaryKeysHashcode ^ GetType().GUID.GetHashCode();
            }
        }

        public int GetWPOHashCode()
        {
            return GetAllColumns().OrderBy(p => p.Name)
                                  .Aggregate(0, (acc, p) => acc ^ (GetPropertyValue(p) ?? 0).GetHashCode());
        }

        public void Remove()
        {
            Status = ObjectStatus.Deleted;
            InformChildrenAboutMyDeath();
        }

        public string GetTableName()
        {
            var attribute = GetType().GetAttribute<WPOTableAttribute>();

            if (attribute == null)
            {
                throw new NotTableAttributeDefinedException();
            }

            return attribute.TableName ?? GetType().Name.ToLower();
        }

        public InheritanceType GetInheritanceType()
        {
            var attribute = GetType().GetAttribute<WPOTableAttribute>();
            return attribute.Inheritance;
        }

        public PrimaryKey GetPrimaryKey()
        {
            InheritanceType inheritance = GetInheritanceType();
            var primaryKeysType = this.GetPropertiesByAttribute<WPOPrimaryKeyAttribute>()
                                      .FirstOrDefault(p => inheritance == InheritanceType.ClassTable ? 
                                                           p.DeclaringType.Name == GetType().Name : 
                                                           true);
            if (primaryKeysType == null)
            {
                throw new NotPrimaryKeyDefinedException();
            }

            PrimaryKey result = new PrimaryKey();
            var attribute = primaryKeysType.GetAttribute<WPOPrimaryKeyAttribute>();
            result.ColumnName = GetColumnName(primaryKeysType);
            result.Value = GetPropertyValue(primaryKeysType);
            if (attribute.UseSequence)
            {
                result.SequenceName = attribute.SequenceName;
            }

            return result;
        }

        public void LoadDependencies()
        {
            if (Status != ObjectStatus.New)
            {
                //Get WPOCollections 
                var collectionProperties = GetType().GetProperties().Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(WPOCollection<>));
                foreach (var collectionProperty in collectionProperties)
                {
                    Type childType = collectionProperty.PropertyType.GetGenericArguments()[0];
                    //Gets the query
                    object query = typeof(WPOManager).GetMethod("GetQuery", BindingFlags.Public | BindingFlags.Instance)
                                                     .MakeGenericMethod(childType)
                                                     .Invoke(WPOManager.GetInstance(), new object[] { Session });

                    //Finds the connected field name
                    string relatedObjColumn = GetColumnName(childType.GetProperties().FirstOrDefault(prop => prop.PropertyType == GetType()));
                    //Set filter
                    query.GetType().GetMethod("Where", BindingFlags.Public | BindingFlags.Instance).Invoke(query, new object[] { relatedObjColumn + " = " + TableObject.PrimaryKey.Value, false });
                    //Get objects and automatically set references
                    query.GetType().GetMethod("GetObjects", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(query, null);
                }

                //Get WPOObjects
                var relatedObjProperties = GetType().GetProperties().Where(p => p.GetAttribute<WPORelationAttribute>() != null);
                foreach (var relatedObjProperty in relatedObjProperties)
                {
                    //Finds the connected field name
                    WPORelationAttribute relation = relatedObjProperty.GetAttribute<WPORelationAttribute>();
                    if (DataRow.ContainsKey(relation.ColumnName) && !string.IsNullOrEmpty(DataRow[relation.ColumnName]))
                    {
                        //Gets the query
                        object query = typeof(WPOManager).GetMethod("GetQuery", BindingFlags.Public | BindingFlags.Instance)
                                                         .MakeGenericMethod(relatedObjProperty.PropertyType)
                                                         .Invoke(WPOManager.GetInstance(), new object[] { Session });

                        //Set filter
                        query.GetType().GetMethod("Where", BindingFlags.Public | BindingFlags.Instance).Invoke(query, new object[] { relation.ForeignKey + " = " + DataRow[relation.ColumnName], false });
                        //Get object
                        query.GetType().GetMethod("GetObjects", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(query, null);

                        var queryList = query.GetType().GetProperty("InternalList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(query);
                        var queryObj = queryList.GetType().GetMethod("get_Item").Invoke(queryList, new object[] { 0 });
                        relatedObjProperty.SetValue(this, queryObj);
                        queryObj.GetType().GetMethod("AddToCollection", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(GetType()).Invoke(queryObj, new object[] { this });
                    }
                }

            }
        }

        #endregion Public Methods

        #region Internal Methods

        internal string GetColumnName(string propertyName)
        {
            return GetColumnName(GetType().GetProperty(propertyName));
        }

        internal List<PropertyInfo> GetAllColumns(bool ignorePrimaryKeys = false)
        {
            return this.GetPropertiesByAttribute<WPOColumnAttribute>()
                       .Where(prop => !ignorePrimaryKeys || !Attribute.IsDefined(prop, typeof(WPOPrimaryKeyAttribute)))
                       .ToList();
        }

        internal List<PropertyInfo> GetAllColumnsWithoutRelations(string baseClassName = null)
        {
            if (TableObject.Inheritance == InheritanceType.ClassTable)
            {
                string typeName = GetType().Name;
                return GetAllColumns().Where(prop => !Attribute.IsDefined(prop, typeof(WPORelationAttribute)) && 
                                                     prop.DeclaringType.Name == (string.IsNullOrEmpty(baseClassName) ? typeName : baseClassName))
                                      .ToList();
            }
            else
            {
                return GetAllColumns().Where(prop => !Attribute.IsDefined(prop, typeof(WPORelationAttribute))).ToList();
            }
        }

        internal List<PropertyInfo> GetAllRelations()
        {
            return this.GetPropertiesByAttribute<WPORelationAttribute>().ToList();
        }

        internal string GetColumnName(PropertyInfo property)
        {
            var attribute = property.GetAttribute<WPOColumnAttribute>();

            if (attribute == null)
            {
                throw new NotColumnAttributeDefinedException();
            }

            return attribute.ColumnName ?? property.Name.ToLower();
        }
        
        internal object GetPropertyValueByColumnName(string columnName)
        {
            PropertyInfo property = GetAllColumns().SingleOrDefault(prop => GetColumnName(prop) == columnName);

            return property != null ? GetPropertyValue(property) : null;
        }

        internal object GetPropertyValue(PropertyInfo properytInfo)
        {
            return properytInfo.GetValue(this);
        }
        
        internal object GetPropertyColumnValue(PropertyInfo properytInfo)
        {
            return GetPropertyValue(properytInfo);
        }

        internal object GetForgeinKeyValue(PropertyInfo property)
        {
            var attribute = property.GetAttribute<WPORelationAttribute>();
            if (attribute == null)
            {
                throw new ArgumentException("Not relation defined");
            }

            var obj = GetPropertyValue(property) as WPOBaseObject;
            return obj?.GetPropertyValueByColumnName(attribute.ForeignKey);
        }

        internal void SetSequences(List<ExecuteResult<long>> sequencesList)
        {
            var primaryKeysTypes = this.GetPropertiesByAttribute<WPOPrimaryKeyAttribute>();
            foreach (var primaryKeysType in primaryKeysTypes)
            {
                var attribute = primaryKeysType.GetAttribute<WPOPrimaryKeyAttribute>();
                if (attribute.UseSequence)
                {
                    var seq = sequencesList.First(s => s.Key == attribute.SequenceName);
                    primaryKeysType.SetValue(this, seq.Value);
                    sequencesList.Remove(seq);
                }
            }
        }

        internal void AddToCollection<WPOType>(WPOType obj) 
            where WPOType : WPOBaseObject
        {
            PropertyInfo collectionProperty = GetType().GetProperties().SingleOrDefault(prop => prop.PropertyType == typeof(WPOCollection<WPOType>));
            if (collectionProperty != null)
            {
                var collection = (WPOCollection<WPOType>)GetPropertyValue(collectionProperty);
                if (collection == null)
                {
                    collection = new WPOCollection<WPOType>();
                }

                if (!collection.Contains(obj))
                {
                    collection.Add(obj);
                    collectionProperty.SetValue(this, collection);
                }
            }
        }

        internal void AddRelation<WPOType>(WPOType obj, string foreignKey)
            where WPOType : WPOBaseObject
        {
            PropertyInfo relatedObjProperty = GetType().GetProperties().SingleOrDefault(prop => prop.PropertyType == typeof(WPOType) && GetColumnName(prop) == foreignKey);
            if (relatedObjProperty != null)
            {
                relatedObjProperty.SetValue(this, obj);
            }
        }

        #endregion Internal Methods

        #region Private Methods

        private void InformChildrenAboutMyDeath() // Sets own reference to null in every object that is in relation with this object
        {
            var childrenTypes = this.GetPropertiesByAttribute<WPORelationAttribute>();

            foreach (var childType in childrenTypes)
            {
                var attribute = childType.GetAttribute<WPORelationAttribute>();
                WPOBaseObject child = GetPropertyValue(childType) as WPOBaseObject;
                // Remove in one to one relation
                if (attribute.RelationType == RelationType.OneToOne)
                {
                    // Set reference to this object in child to null
                    var propertyInfo = child.GetAllColumns().Single(prop => GetColumnName(prop) == attribute.ForeignKey);
                    WPOBaseObject property = GetPropertyValue(propertyInfo) as WPOBaseObject;
                    property = null;
                }
                // remove in one to many relation, from single object position
                else
                {                    
                    var relatedCollection = child.GetWPOCollection(this.GetType());
                    if (relatedCollection != null)
                    {
                        int index = (relatedCollection as List<WPOBaseObject>).FindIndex(x => x.ObjectGuid == ObjectGuid);
                        if (index >= 0)
                        {
                            (relatedCollection as List<WPOBaseObject>).RemoveAt(index);
                            Type wpoCollectionType = typeof(WPOCollection<>).MakeGenericType(new Type[] { GetType() });
                            MethodInfo castMethod = relatedCollection.GetType().GetMethod("Cast", BindingFlags.Instance | BindingFlags.Public).MakeGenericMethod(GetType());
                            var castCollectionInstance = Activator.CreateInstance(wpoCollectionType, castMethod.Invoke(relatedCollection, new object[] { relatedCollection }));
                            child.GetWPOCollectionType(GetType()).SetValue(child, castCollectionInstance);
                        }
                    }
                }
            }

            //Remove relation from objects in WPOCollections
            var collectionTypes = GetType().GetProperties().Where(prop => prop.PropertyType.IsGenericType && prop.PropertyType.Name.Contains("WPOCollection"));
            foreach (var collectionType in collectionTypes)
            {
                var collection = GetPropertyValue(collectionType) as IEnumerable<WPOBaseObject>;
                if (collection != null) 
                {
                    foreach (var obj in collection)
                    {
                        obj.GetType().GetProperties().Single(prop => obj.GetPropertyValue(prop) == this).SetValue(obj, null);
                    }
                }
             }
        }

        private PropertyInfo GetWPOCollectionType(Type wpoType)
        {
            Type collectionType = typeof(WPOCollection<>).MakeGenericType(new Type[] { wpoType });
            return GetType().GetProperties().FirstOrDefault(prop => prop.PropertyType.Equals(collectionType));
        }

        private WPOCollection<WPOBaseObject> GetWPOCollection(Type wpoType)
        {
            PropertyInfo collectionProperty = GetWPOCollectionType(wpoType);
            if (collectionProperty != null)
            {
                return new WPOCollection<WPOBaseObject>(GetPropertyValue(collectionProperty) as IEnumerable<WPOBaseObject>);
            }

            return null;
        }

        #endregion Private Methods
    }
}
