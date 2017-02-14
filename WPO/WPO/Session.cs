using System;
using System.Collections.Generic;
using System.Linq;
using WPO.Connection;
using WPO.Enums;

namespace WPO
{
    public class Session : IDisposable
    {
        internal HashSet<WPOBaseObject> objectsFromDatabase;

        internal HashSet<WPOBaseObject> objectsAll; //Contains all objects, also those from database (but as different references)

        internal IDbConnection dbConnection;

        private static object lockObject = new object();
        private static object sequenceLockObject = new object();

        private static Session empty = new Session();
        public static Session Empty { get { return empty; } }

        private Session()
        {
            objectsFromDatabase = new HashSet<WPOBaseObject>();
            objectsAll = new HashSet<WPOBaseObject>();
        }

        internal Session(IDbConnection dbConnection, string connectionString) : this()
        {
            if (dbConnection == null)
            {
                throw new NullReferenceException(nameof(dbConnection));
            }

            this.dbConnection = dbConnection;
            this.dbConnection.ConnectionString = connectionString;
        }
        
        #region Public Methods

        public void Dispose()
        {
            objectsFromDatabase = null;
            objectsAll = null;
            dbConnection.Close();
            dbConnection.Dispose();
            dbConnection = null;
        }

        public void Rollback()
        {
            foreach (var obj in objectsAll)
            {
                obj.Session = null;
            }
            
            objectsAll.Clear();
            objectsAll = new HashSet<WPOBaseObject>();
            objectsFromDatabase.Clear();
            objectsFromDatabase = new HashSet<WPOBaseObject>();
        }
        
        public void Commit()
        {
            if (objectsAll.Any())
            {
                //Exectue commands
                lock (lockObject)
                {
                    dbConnection.Open();

                    CheckForChanges();
                    
                    dbConnection.MakeTransaction(CreateInsertTransactions());
                    dbConnection.MakeTransaction(CreateUpdateTransactions());
                    dbConnection.MakeTransaction(CreateDeleteTransactions());

                    //Remove deleted objects from collection
                    objectsAll.RemoveWhere(x => x.Status == ObjectStatus.Deleted);

                    objectsFromDatabase = new HashSet<WPOBaseObject>();
                    if (objectsAll != null)
                    {
                        foreach (var obj in objectsAll)
                        {
                            obj.Status = ObjectStatus.Unchanged;
                            objectsFromDatabase.Add((WPOBaseObject)obj.Clone());
                        }
                    }

                    dbConnection.Close();
                }
            }
        }
        #endregion Public Methods

        #region Internal Methods

        internal void Register(WPOBaseObject wpoObject)
        {
            if (wpoObject == null)
            {
                throw new NullReferenceException(nameof(wpoObject));
            }

            if (objectsAll == null)
            {
                objectsAll = new HashSet<WPOBaseObject>();
            }

            if (!objectsAll.Contains(wpoObject))
            {
                objectsAll.Add(wpoObject);
            }
        }

        internal WPOType CreateOrGetObject<WPOType>(Dictionary<string, string> dataRow, WPOTableObject table, out bool alreadyExsist) where WPOType : WPOBaseObject
        {
            WPOType exsistObject = (WPOType)objectsAll.FirstOrDefault(o => o.DataRow != null && 
                                                                           o.TableObject.TableName == table.TableName && 
                                                                           o.DataRow[table.PrimaryKey.ColumnName] == dataRow[table.PrimaryKey.ColumnName]);
            alreadyExsist = exsistObject != null;
            return exsistObject ?? (WPOType)Activator.CreateInstance(typeof(WPOType), new object[] { this });
        }

        #endregion Internal Methods

        #region Private Methods 

        //Set changed objects status to Modified
        private void CheckForChanges()
        {
            foreach (var obj in objectsAll.Where(x => x.Status == ObjectStatus.Unchanged))
            {
                var originalObj = GetOriginalObject(obj);
                if (obj.GetWPOHashCode() != originalObj.GetWPOHashCode())
                {
                    obj.Status = ObjectStatus.Modified;
                }
            }
        }

        private List<ExecuteCommand> CreateInsertTransactions()
        {
            List<ExecuteCommand> commands = new List<ExecuteCommand>();
            var objectsToInsert = objectsAll.Where(x => x.Status == ObjectStatus.New);
            if (objectsToInsert.Any())
            {
                List<WPOBaseObject> readyObjects = new List<WPOBaseObject>();
                List<WPOBaseObject> notReadyObjects = new List<WPOBaseObject>();

                Dictionary<string, int> sequences = new Dictionary<string, int>(); //using to storage sequnce name and the count of needed instances

                // Check for every object if it's using any sequence
                foreach (var obj in objectsToInsert)
                {
                    var primaryKeys = obj.GetPrimaryKey();
                    if (string.IsNullOrEmpty(primaryKeys.SequenceName))
                    {
                        readyObjects.Add(obj);
                    }
                    else
                    {
                        notReadyObjects.Add(obj);
                        if (sequences.ContainsKey(primaryKeys.SequenceName))
                        {
                            sequences[primaryKeys.SequenceName]++;
                        }
                        else
                        {
                            sequences.Add(primaryKeys.SequenceName, 1);
                        }
                    }
                }

                // Gets sequences values from database
                List<ExecuteResult<long>> sequencesFromDB = null;
                if (sequences.Any())
                {
                    sequencesFromDB = GetSequencesValuesFromDB(sequences);
                }

                // Puts sequences values into primary keys
                if (sequencesFromDB != null && sequencesFromDB.Any())
                {
                    foreach (var obj in notReadyObjects)
                    {
                        obj.SetSequences(sequencesFromDB);
                        readyObjects.Add(obj);
                    }
                }

                // Creates command for each object
                foreach (var obj in readyObjects)
                {
                    ExecuteCommand command = new ExecuteCommand(ExecuteCommand.CommandType.Insert);

                    obj.TableObject.PrimaryKey = obj.GetPrimaryKey();
                    command.TableName = obj.TableObject.TableName;
                    command.ObjectsIdentifiers.Add(obj.TableObject.PrimaryKey);
                    command.Objects = CreateDiffrentsDictionary(obj, false);

                    if (obj.TableObject.Inheritance == InheritanceType.ClassTable)
                    {
                        List<ExecuteCommand> tmpCommands = new List<ExecuteCommand>();
                        WPOTableObject tableObj = obj.TableObject;
                        while (tableObj.BaseTable != null)
                        {
                            ExecuteCommand baseTypeCommand = new ExecuteCommand(ExecuteCommand.CommandType.Insert);
                            baseTypeCommand.TableName = tableObj.BaseTable.TableName;
                            baseTypeCommand.ObjectsIdentifiers.Add(tableObj.BaseTable.PrimaryKey);
                            baseTypeCommand.ObjectsIdentifiers.First().Value = obj.TableObject.PrimaryKey.Value;
                            baseTypeCommand.Objects = CreateDiffrentsDictionary(obj, false, tableObj.BaseTable.WPOObject.GetType().Name);
                            baseTypeCommand.Objects[tableObj.BaseTable.PrimaryKey.ColumnName] = Tuple.Create(obj.TableObject.PrimaryKey.Value, false);
                            tmpCommands.Add(baseTypeCommand);

                            tableObj = tableObj.BaseTable;
                        }

                        tmpCommands.Reverse();
                        commands.AddRange(tmpCommands);
                    }

                    commands.Add(command);
                }
            }

            return commands;
        }

        private List<ExecuteCommand> CreateUpdateTransactions()
        {
            List<ExecuteCommand> commands = new List<ExecuteCommand>();
            var objectsToUpdate = objectsAll.Where(x => x.Status == ObjectStatus.Modified); 
            if (objectsToUpdate.Any())
            {
                foreach (var obj in objectsToUpdate)
                {
                    ExecuteCommand command = new ExecuteCommand(ExecuteCommand.CommandType.Update);

                    command.TableName = obj.TableObject.TableName;
                    command.ObjectsIdentifiers.Add(obj.TableObject.PrimaryKey);
                    command.Objects = CreateDiffrentsDictionary(obj, true);

                    if (obj.TableObject.Inheritance == InheritanceType.ClassTable)
                    {
                        List<ExecuteCommand> tmpCommands = new List<ExecuteCommand>();
                        WPOTableObject tableObj = obj.TableObject;
                        while (tableObj.BaseTable != null)
                        {
                            ExecuteCommand baseTypeCommand = new ExecuteCommand(ExecuteCommand.CommandType.Update);
                            baseTypeCommand.TableName = tableObj.BaseTable.TableName;
                            baseTypeCommand.ObjectsIdentifiers.Add(tableObj.BaseTable.PrimaryKey);
                            baseTypeCommand.ObjectsIdentifiers.First().Value = obj.TableObject.PrimaryKey.Value;
                            baseTypeCommand.Objects = CreateDiffrentsDictionary(obj, true, tableObj.BaseTable.WPOObject.GetType().Name);
                            tmpCommands.Add(baseTypeCommand);

                            tableObj = tableObj.BaseTable;
                        }

                        tmpCommands.Reverse();
                        commands.AddRange(tmpCommands);
                    }

                    commands.Add(command);
                }
            }

            return commands;
        }

        private List<ExecuteCommand> CreateDeleteTransactions()
        {
            List<ExecuteCommand> commands = new List<ExecuteCommand>();
            var existsObjectsIds = objectsFromDatabase.Select(o => o.GetIdentityKey());
            var objectsToDelete = objectsAll.Where(x => x.Status == ObjectStatus.Deleted && existsObjectsIds.Contains(x.GetIdentityKey())).ToList();
            
            if (objectsToDelete.Any())
            {
                //Group objects by types - it allows to improve perfmormance
                foreach (var objGroup in objectsToDelete.GroupBy(x => x.GetType()))
                {
                    ExecuteCommand command = new ExecuteCommand(ExecuteCommand.CommandType.Delete);

                    command.TableName = objGroup.First().GetTableName();
                    command.ObjectsIdentifiers.AddRange(objGroup.Select(x => x.GetPrimaryKey()));

                    commands.Add(command);
                }                
            }

            return commands;
        }
        
        // Turning the object properties into dictionary with positions as column name and value
        private Dictionary<string, Tuple<object, bool>> CreateDiffrentsDictionary(WPOBaseObject obj, bool onlyChangedPositions, string baseClassName = null)
        {
            Dictionary<string, Tuple<object, bool>> result = new Dictionary<string, Tuple<object, bool>>();
            if (onlyChangedPositions)
            {
                WPOBaseObject originalObj = GetOriginalObject(obj);
                foreach (var property in obj.GetAllColumnsWithoutRelations(baseClassName))
                {
                    string columnName = obj.GetColumnName(property);
                    object value = obj.GetPropertyColumnValue(property);
                    object originalValue = originalObj.GetPropertyValueByColumnName(columnName);
                    if ((value == null ^ originalValue == null) || (originalValue != null && !originalValue.Equals(value)))
                    {
                        result.Add(columnName, Tuple.Create(value, false));
                    }
                }

                foreach (var property in obj.GetAllRelations())
                {
                    object value = obj.GetForgeinKeyValue(property);
                    object originalValue = originalObj.GetForgeinKeyValue(property);
                    if ((value == null ^ originalValue == null) || (originalValue != null && !originalValue.Equals(value)))
                    {
                        result.Add(obj.GetColumnName(property), Tuple.Create(value, true));
                    }
                }
            }
            else
            {
                foreach (var property in obj.GetAllColumnsWithoutRelations(baseClassName))
                {
                    result.Add(obj.GetColumnName(property), Tuple.Create(obj.GetPropertyColumnValue(property), false));
                }

                foreach (var property in obj.GetAllRelations().Where(prop => obj.GetPropertyValue(prop) != null))
                {
                    result.Add(obj.GetColumnName(property), Tuple.Create(obj.GetForgeinKeyValue(property), true));
                }
            }

            return result;
        }
        
        private List<ExecuteResult<long>> GetSequencesValuesFromDB(Dictionary<string, int> commands)
        {
            List<ExecuteResult<long>> result;
            lock (sequenceLockObject)
            {
                result = dbConnection.GetSequences(commands).ToList();
            }

            return result;
        }

        private WPOBaseObject GetOriginalObject(WPOBaseObject obj)
        {
            return objectsFromDatabase.FirstOrDefault(x => x.ObjectGuid == obj.ObjectGuid);
        }


        #endregion Private Methods
    }
}
