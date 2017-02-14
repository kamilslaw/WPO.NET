using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using WPO.Connection;
using WPO.Tests.Utils;

using static WPO.Tests.Utils.WPOTestClasses;

namespace WPO.Tests.SessionTests
{
    [TestClass]
    public class CreatingTests
    {
        private List<ExecuteCommand> commands;
        private Session session;

        [TestInitialize]
        public void MyTestInitialize()
        {
            commands = new List<ExecuteCommand>();
            var connectionMock = new Mock<IDbConnection>();
            connectionMock.Setup(c => c.MakeTransaction(It.IsAny<IEnumerable<ExecuteCommand>>()))
                          .Callback<IEnumerable<ExecuteCommand>>(_commands => commands.AddRange(_commands));
            
            session = WPOManager.GetInstance().GetSession(connectionMock.Object, Guid.NewGuid().ToString());
        }

        [TestMethod]
        public void AddSingleRecordTest()
        {
            // Arange
            Table1 table1 = new Table1(session) { Id = 1, Name = "Hello world", Value = 44.32m };

            // Act
            session.Commit();

            // Assert
            Assert.IsTrue(commands != null && commands.Count() == 1);
            var command = commands.Single();
            Assert.IsTrue(command.TableName == "table1" && command.Type == ExecuteCommand.CommandType.Insert);
            // Check primary key
            Assert.IsTrue(command.ObjectsIdentifiers != null && command.ObjectsIdentifiers.Count == 1);
            var primaryKey = command.ObjectsIdentifiers.Single();
            Assert.IsTrue(primaryKey.ColumnName == "id" && (primaryKey.Value as int?) == 1);
            // Check values
            Assert.IsTrue(command.Objects != null && command.Objects.Count == 3);
            Assert.IsTrue((int)command.Objects["id"].Item1 == 1);
            Assert.IsTrue(command.Objects["customname"].Item1.ToString() == "Hello world");
            Assert.IsTrue((decimal)command.Objects["value"].Item1 == 44.32m);
        }

        [TestMethod]
        public void AddMultipleRecordTest()
        {
            // Arange
            Enumerable.Range(1, 10)
                      .Select(val => new Table1(session) { Id = val, Name = "Hello world " + val, Value = val * 13.05m })
                      .ToList();

            // Act
            session.Commit();

            // Assert
            Assert.IsTrue(commands != null && commands.Count() == 10);
            Assert.IsTrue(commands.All(c => c.TableName == "table1" && c.Type == ExecuteCommand.CommandType.Insert));
            // Check primary keys
            Assert.IsTrue(commands.All(c => c.ObjectsIdentifiers != null && c.ObjectsIdentifiers.Count == 1));
            var ids = commands.SelectMany(c => c.ObjectsIdentifiers).Select(p => (int)p.Value).OrderBy(x => x);
            Assert.IsTrue(ids.SequenceEqual(Enumerable.Range(1, 10)));
            Assert.IsTrue(commands.All(c => c.Objects != null && c.Objects.Count == 3));
            var values = commands.Select(obj => obj.Objects).OrderBy(x => x["id"]).ToList();
            var correctValues = Enumerable.Range(1, 10)
                                          .Select(val => new Dictionary<string, Tuple<object, bool>>
                                          {
                                              ["id"] = Tuple.Create<object, bool>(val, false),
                                              ["customname"] = Tuple.Create<object, bool>("Hello world " + val, false),
                                              ["value"] = Tuple.Create<object, bool>(val * 13.05m, false)
                                          })
                                          .ToList();
            var comparer = new DictionaryComparer<string, Tuple<object, bool>>();
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(comparer.Equals(values[i], correctValues[i]));
            }
        }
    }
}
