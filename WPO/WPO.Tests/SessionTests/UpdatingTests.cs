using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using WPO.Connection;
using WPO.Enums;

using static WPO.Tests.Utils.WPOTestClasses;

namespace WPO.Tests
{
    [TestClass]
    public class UpdatingTests
    {
        private List<ExecuteCommand> commands;
        private Mock<IDbConnection> connectionMock;
        private Session session;
        private WPOManager wpoManager;

        [TestInitialize]
        public void MyTestInitialize()
        {
            commands = new List<ExecuteCommand>();
            connectionMock = new Mock<IDbConnection>();
            connectionMock.Setup(c => c.MakeTransaction(It.IsAny<IEnumerable<ExecuteCommand>>()))
                          .Callback<IEnumerable<ExecuteCommand>>(_commands => commands.AddRange(_commands));

            wpoManager = WPOManager.GetInstance();
            session = wpoManager.GetSession(connectionMock.Object, Guid.NewGuid().ToString());
        }

        [TestMethod]
        public void UpdateRecordsTest()
        {
            // PART 1
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
                              },
                              new Dictionary<string, string>()
                              {
                                  ["Id"] = "2",
                                  ["Name"] = "Brand 2",
                                  ["IsNew"] = null,
                                  ["Type"] = "3",
                                  ["Date"] = "2015-01-14 00:00:00",
                              }
                          });

            var query = wpoManager.GetQuery<Brand>(session);
            Brand brand1 = query.First();
            Brand brand2 = query.Last();
            session.Commit();

            // Assert no. 1
            UpdateSingleRecordGetObjAssert(brand1, brand2);

            // PART 2
            brand1.Name = "Brand 1 New name";
            brand1.Date = new DateTime(2018, 2, 15);
            brand1.IsNew = false;
            session.Commit();

            // Assert no. 2
            UpdateSingleRecordChangeSingleObjAssert();

            // PART 3
            commands.Clear();
            session.Commit();

            // Assert no. 3
            UpdateSingleRecordNonChangedObjAssert();

            // PART 4
            brand1.Name = "Brand 1";
            brand2.Name = "New Brand 2";
            session.Commit();

            // Assert no. 4
            UpdateSingleRecordChangeMultipleObjAssert();

            // Final Assert
            connectionMock.Verify(c => c.ExecuteReader(It.IsAny<QueryFilter>()), Times.Once);
        }

        private void UpdateSingleRecordGetObjAssert(Brand brand1, Brand brand2)
        {
            Assert.IsTrue(brand1.Id == 1 && brand1.Name == "Brand 1" && brand1.IsNew == null && brand1.CustomType == 4 && brand1.Date == new DateTime(2017, 1, 14));
            Assert.AreEqual(brand1.Status, ObjectStatus.Unchanged);
            Assert.IsTrue(brand2.Id == 2 && brand2.Name == "Brand 2" && brand2.IsNew == null && brand2.CustomType == 3 && brand2.Date == new DateTime(2015, 1, 14));
            Assert.AreEqual(brand2.Status, ObjectStatus.Unchanged);

            Assert.IsTrue(commands == null || !commands.Any());
        }

        private void UpdateSingleRecordChangeSingleObjAssert()
        {
            Assert.IsTrue(commands?.Count() == 1 && commands?.FirstOrDefault().Objects.Count == 3 && commands.First().Type == ExecuteCommand.CommandType.Update);
            Assert.AreEqual(1L, commands.First().ObjectsIdentifiers.First().Value);

            Dictionary<string, Tuple<object, bool>> objects = commands.First().Objects;
            Assert.IsTrue(objects["Name"].Item1.ToString() == "Brand 1 New name");
            Assert.IsTrue((bool?)objects["IsNew"].Item1 == false);
            Assert.IsTrue((DateTime)objects["Date"].Item1 == new DateTime(2018, 2, 15));
        }

        private void UpdateSingleRecordNonChangedObjAssert()
        {
            Assert.IsTrue(!commands.Any());
        }

        private void UpdateSingleRecordChangeMultipleObjAssert()
        {
            Assert.IsTrue(commands?.Count() == 2 && commands.All(c => c.Objects.Count == 1 && c.Type == ExecuteCommand.CommandType.Update));

            Dictionary<string, Tuple<object, bool>> brand1Objects = commands.First().Objects;
            Dictionary<string, Tuple<object, bool>> brand2Objects = commands.Last().Objects;
            Assert.IsTrue(brand1Objects["Name"].Item1.ToString() == "Brand 1");
            Assert.IsTrue(brand2Objects["Name"].Item1.ToString() == "New Brand 2");
        }
    }
}
