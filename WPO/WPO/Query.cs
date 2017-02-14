using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WPO.Attributes;
using WPO.Enums;
using WPO.Helpers;

namespace WPO
{
    public class Query<T> : IEnumerable<T>
        where T : WPOBaseObject
    {
        internal List<T> InternalList { get; private set; }

        private QueryFilter filter = new QueryFilter(typeof(T).CreateModelObj().TableObject);
        private Session session;

        internal Query(Session session)
        {
            this.session = session;
        }

        #region Collection Interfaces Implementations
        
        public IEnumerator<T> GetEnumerator()
        {
            if (InternalList == null)
            {
                GetObjects();
            }

            return InternalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion Collection Interfaces Implementations

        #region Overridden LINQ Methods

        public Query<T> Skip(int count)
        {
            InternalList = null;
            filter.Skip = count;
            return this;
        }

        public Query<T> Take(int count)
        {
            InternalList = null;
            filter.Take = count;
            return this;
        }

        public Query<T> Where(string statement, bool isAlternative = false)
        {
            InternalList = null;
            statement = NormalizeStatement(statement);
            filter.AddCondition(statement, isAlternative ? QueryFilter.Statement.Operator.OR : QueryFilter.Statement.Operator.AND);
            return this;
        }

        public Query<T> OrderBy(string columnName, bool isAscending = false)
        {
            InternalList = null;
            filter.AddOrder(columnName, isAscending ? QueryFilter.Order.Direction.ASC : QueryFilter.Order.Direction.DESC);
            return this;
        }

        public T GetObjectByKey(object keyValue)
        {
            filter.Clear();
            filter.AddCondition(filter.Table.PrimaryKey.ColumnName + " = " + keyValue);

            GetObjects();
            return InternalList.FirstOrDefault();
        }

        public IEnumerable<T> GetObjectsByKey(params object[] keyValues)
        {
            if (keyValues == null || keyValues.Count() == 0)
            {
                throw new ArgumentNullException(nameof(keyValues));
            }

            filter.Clear();
            filter.AddCondition(filter.Table.PrimaryKey.ColumnName + " in (" + string.Join(",", keyValues) + ")");

            GetObjects();
            return InternalList;
        }

        #endregion Overridden LINQ Methods

        #region Private Methods

        private void GetObjects()
        {
            InternalList = new List<T>();
            int depth = WPOManager.Configuration.DependencyDepth > 0 ? WPOManager.Configuration.DependencyDepth : int.MaxValue;
            try
            {
                session.dbConnection.Open();
                foreach (Dictionary<string, string> dataRow in session.dbConnection.ExecuteReader(filter))
                {
                    bool alreadyExist;
                    T obj = session.CreateOrGetObject<T>(dataRow, filter.Table, out alreadyExist);
                    obj.Status = ObjectStatus.Unchanged;
                    obj.DataRow = dataRow;

                    Dictionary<PropertyInfo, string> values = new Dictionary<PropertyInfo, string>();
                    List<PropertyInfo> properties = obj.GetPropertiesByAttribute<WPOColumnAttribute>();
                    GetSimpleProperties(obj, dataRow, values, properties);
                    obj = (T)BasicMapper.MapPropertiesToObject(obj, values);
                    GetRelatedObjects(obj, dataRow, properties, depth - 1);
                    obj.TableObject = new WPOTableObject(obj);

                    if (!alreadyExist)
                    {
                        session.objectsFromDatabase.Add((WPOBaseObject)obj.Clone());
                    }

                    InternalList.Add(obj);
                }
            }
            finally
            {
                filter.Clear();
                session.dbConnection.Close();
            }
        }

        private void GetRelatedObjects<WPOType>(WPOType obj, Dictionary<string, string> dataRow, List<PropertyInfo> properties, int depth) where WPOType : WPOBaseObject
        {
            foreach (PropertyInfo property in properties.Where(prop => prop.GetAttribute<WPORelationAttribute>() != null))
            {
                string columnName = obj.GetColumnName(property);
                string value = dataRow[columnName];
                if (!string.IsNullOrEmpty(value))
                {
                    WPORelationAttribute relation = property.GetAttribute<WPORelationAttribute>();
                    var relatedObject = session.objectsAll.FirstOrDefault(o => o.GetType() == property.PropertyType &&
                                                                                        o.GetPropertyValueByColumnName(relation.ForeignKey) != null &&
                                                                                        o.GetPropertyValueByColumnName(relation.ForeignKey).ToString() == value);
                    if (relatedObject == null && depth > 0)
                    {
                        MethodInfo method = GetType().GetMethod("GetObjectWithDependencies", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(property.PropertyType);
                        relatedObject = (WPOBaseObject)method.Invoke(this, new object[] { relation.ForeignKey, value, depth });
                    }

                    // Add itself to releted object collection
                    if (relatedObject != null)
                    {
                        if (relation.RelationType == RelationType.OneToMany)
                        {
                            relatedObject.AddToCollection(obj);
                        }
                        else if (relation.RelationType == RelationType.OneToOne)
                        {
                            relatedObject.AddRelation(obj, relation.ForeignKey);
                        }
                    }

                    property.SetValue(obj, relatedObject);
                }
            }
        }

        private WPOType GetObjectWithDependencies<WPOType>(string fieldName, string fieldValue, int depth)
            where WPOType : WPOBaseObject
        {
            QueryFilter dependencyFilter = new QueryFilter(typeof(WPOType).CreateModelObj().TableObject);
            dependencyFilter.AddCondition(fieldName + " = " + fieldValue);
            Dictionary<string, string> dataRow = session.dbConnection.ExecuteReader(dependencyFilter).FirstOrDefault();

            bool alreadyExist;
            WPOType obj = session.CreateOrGetObject<WPOType>(dataRow, dependencyFilter.Table, out alreadyExist);
            obj.Status = ObjectStatus.Unchanged;
            obj.DataRow = dataRow;

            Dictionary<PropertyInfo, string> values = new Dictionary<PropertyInfo, string>();
            List<PropertyInfo> properties = obj.GetPropertiesByAttribute<WPOColumnAttribute>();
            GetSimpleProperties(obj, dataRow, values, properties);
            obj = (WPOType)BasicMapper.MapPropertiesToObject(obj, values);
            GetRelatedObjects(obj, dataRow, properties, depth - 1);
            obj.TableObject = new WPOTableObject(obj);

            if (!alreadyExist)
            {
                session.objectsFromDatabase.Add((WPOBaseObject)obj.Clone());
            }

            return obj;
        }

        private static void GetSimpleProperties<WPOType>(WPOType obj, Dictionary<string, string> dataRow, Dictionary<PropertyInfo, string> values, List<PropertyInfo> properties) where WPOType : WPOBaseObject
        {
            foreach (PropertyInfo property in properties.Where(prop => prop.GetAttribute<WPORelationAttribute>() == null))
            {
                string columnName = obj.GetColumnName(property);
                if (dataRow.ContainsKey(columnName))
                {
                    string value = dataRow[columnName];
                    values.Add(property, value);
                }
            }
        }

        private string NormalizeStatement(string statement)
        {
            return statement.Replace("!=", "<>").Replace("==", "=");
        }
        
        #endregion Private Methods
    }
}
