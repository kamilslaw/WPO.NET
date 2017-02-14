using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using WPO.Connection;
using Moq;

using static WPO.Tests.Utils.WPOTestClasses;
using System.Linq;

namespace WPO.Tests
{
    [TestClass]
    public class DeletingTests
    {
        private IEnumerable<ExecuteCommand> commands;
        private Mock<IDbConnection> connectionMock;
        private Session session;
        private WPOManager wpoManager;

        [TestInitialize]
        public void MyTestInitialize()
        {
            commands = null;
            connectionMock = new Mock<IDbConnection>();
            connectionMock.Setup(c => c.MakeTransaction(It.IsAny<IEnumerable<ExecuteCommand>>()))
                          .Callback<IEnumerable<ExecuteCommand>>(_commands => commands = _commands);

            wpoManager = WPOManager.GetInstance();
            session = wpoManager.GetSession(connectionMock.Object, Guid.NewGuid().ToString());
        }

        [TestMethod]
        public void SimpleRecordDeleteTest()
        {
            connectionMock.Setup(c => c.ExecuteReader(It.IsAny<QueryFilter>()))
                          .Returns(new List<Dictionary<string, string>>()
                          {
                              new Dictionary<string, string>()
                              {
                                  ["Id"] = "1",
                                  ["Name"] = "Brand 1",
                                  ["IsNew"] = null,
                                  ["Type"] = "4",
                                  ["Date"] = "2017-01-14 00:00:00",
                              }
                          });

            var query = wpoManager.GetQuery<Brand>(session);
            Brand brand = query.Single();

            brand.Remove();
            session.Commit();

            Assert.IsTrue(commands?.Count() == 1 && commands.First().Type == ExecuteCommand.CommandType.Delete);
            Assert.AreEqual(1L, commands.Single().ObjectsIdentifiers.Single().Value);
        }

        [TestMethod]
        public void RecordWithRelationDeleteTest()
        {
            connectionMock.Setup(c => c.ExecuteReader(It.Is<QueryFilter>((filter) => filter.Table.TableName.Equals("simple2"))))
                          .Returns(new List<Dictionary<string, string>>()
                          {
                              new Dictionary<string, string>()
                              {
                                  ["id"] = "1",
                                  ["name"] = "Simple2 1",
                                  ["simple1id"] = "1",
                              },
                              new Dictionary<string, string>()
                              {
                                  ["id"] = "2",
                                  ["name"] = "Simple2 2",
                                  ["simple1id"] = "1",
                              }
                          });

            connectionMock.Setup(c => c.ExecuteReader(It.Is<QueryFilter>((filter) => filter.Table.TableName.Equals("simple1"))))
                          .Returns(new List<Dictionary<string, string>>()
                          {
                              new Dictionary<string, string>()
                              {
                                  ["id"] = "1",
                                  ["name"] = "Simple1 1",
                              }
                          });

            var query = wpoManager.GetQuery<Simple2>(session);

            Simple2 simple2_1 = query.First();
            Simple2 simple2_2 = query.Last();
            Simple1 simple1 = simple2_1.Parent;

            simple2_1.Remove();
            session.Commit();

            Assert.AreEqual(1, simple1.Children2.Count);
            Assert.AreEqual(2, simple1.Children2.First().Id);

            simple1.Remove();
            session.Commit();

            Assert.AreEqual(null, simple2_2.Parent);
        }
    }
}
