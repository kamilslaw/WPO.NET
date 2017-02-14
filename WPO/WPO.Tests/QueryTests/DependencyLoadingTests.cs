using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WPO.Connection;

using static WPO.Tests.Utils.WPOTestClasses;
using System.Collections.Generic;

namespace WPO.Tests.QueryTests
{
    [TestClass]
    public class DependencyLoadingTests
    {
        private Mock<IDbConnection> connectionMock;
        private Session session, session2;
        private WPOManager wpoManager;

        [TestInitialize]
        public void MyTestInitialize()
        {
            connectionMock = new Mock<IDbConnection>();
            wpoManager = WPOManager.GetInstance();
            session = wpoManager.GetSession(connectionMock.Object, Guid.NewGuid().ToString());
            session2 = wpoManager.GetSession(connectionMock.Object, Guid.NewGuid().ToString());

            connectionMock.Setup(c => c.ExecuteReader(It.Is<QueryFilter>((filter) => filter.Table.TableName.Equals("a"))))
                          .Returns(new List<Dictionary<string, string>>()
                          {
                              new Dictionary<string, string>()
                              {
                                  ["id"] = "1",
                              }
                          });
            connectionMock.Setup(c => c.ExecuteReader(It.Is<QueryFilter>((filter) => filter.Table.TableName.Equals("b"))))
                          .Returns(new List<Dictionary<string, string>>()
                          {
                              new Dictionary<string, string>()
                              {
                                  ["id"] = "2",
                                  ["aid"] = "1",
                              },
                              new Dictionary<string, string>()
                              {
                                  ["id"] = "22",
                                  ["aid"] = "1",
                              },
                              new Dictionary<string, string>()
                              {
                                  ["id"] = "222",
                                  ["aid"] = null,
                              }
                          });
            connectionMock.Setup(c => c.ExecuteReader(It.Is<QueryFilter>((filter) => filter.Table.TableName.Equals("c"))))
                          .Returns(new List<Dictionary<string, string>>()
                          {
                              new Dictionary<string, string>()
                              {
                                  ["id"] = "3",
                                  ["bid"] = "2",
                              }
                          });
            connectionMock.Setup(c => c.ExecuteReader(It.Is<QueryFilter>((filter) => filter.Table.TableName.Equals("d"))))
                          .Returns(new List<Dictionary<string, string>>()
                          {
                              new Dictionary<string, string>()
                              {
                                  ["id"] = "4",
                                  ["cid"] = "3",
                              }
                          });
        }

        [TestMethod]
        public void GetAllDependenciesTest()
        {
            WPOManager.Configuration = new WPOConfiguration() { DependencyDepth = -1 };

            D d = wpoManager.GetQuery<D>(session).FirstOrDefault();

            Assert.IsNotNull(d);
            Assert.IsNotNull(d.Parent);
            Assert.IsNotNull(d.Parent.Parent);
            Assert.IsNotNull(d.Parent.Parent.Parent);
        }

        [TestMethod]
        public void GetNoDependencyTest()
        {
            WPOManager.Configuration = new WPOConfiguration() { DependencyDepth = 1 };

            D d = wpoManager.GetQuery<D>(session).FirstOrDefault();

            Assert.IsNotNull(d);
            Assert.IsNull(d.Parent);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetBadDependencySettingsTest()
        {
            WPOManager.Configuration = new WPOConfiguration() { DependencyDepth = 0 };
        }

        [TestMethod]
        public void GetCustomDependencySettingsTest()
        {
            WPOManager.Configuration = new WPOConfiguration() { DependencyDepth = 2 };

            D d = wpoManager.GetQuery<D>(session).FirstOrDefault();

            Assert.IsNotNull(d);
            Assert.IsNotNull(d.Parent);
            Assert.IsNull(d.Parent.Parent);

            WPOManager.Configuration = new WPOConfiguration() { DependencyDepth = 3 };

            d = wpoManager.GetQuery<D>(session2).FirstOrDefault();

            Assert.IsNotNull(d);
            Assert.IsNotNull(d.Parent);
            Assert.IsNotNull(d.Parent.Parent);
            Assert.IsNull(d.Parent.Parent.Parent);
        }

        [TestMethod]
        public void LoadRelatedCollectionTest()
        {
            A a = wpoManager.GetQuery<A>(session).FirstOrDefault();

            Assert.IsNotNull(a);
            Assert.IsNull(a.Children);

            a.LoadDependencies();

            Assert.IsNotNull(a.Children);
            Assert.AreEqual(2, a.Children.First().Id);
            Assert.AreEqual(22, a.Children.Last().Id);
        }

        [TestMethod]
        public void LoadRelatedObjectTest()
        {
            WPOManager.Configuration = new WPOConfiguration() { DependencyDepth = 1 };
            D d = wpoManager.GetQuery<D>(session).FirstOrDefault();

            Assert.IsNotNull(d);
            Assert.IsNull(d.Parent);

            d.LoadDependencies();

            Assert.IsNotNull(d.Parent);
            Assert.IsNotNull(d.Parent.Children);
            Assert.IsNull(d.Parent.Parent);

            d.Parent.LoadDependencies();

            Assert.IsNotNull(d.Parent.Parent);
            Assert.IsNotNull(d.Parent.Parent.Children);
            Assert.IsNull(d.Parent.Parent.Parent);

            d.Parent.Parent.LoadDependencies();

            Assert.IsNotNull(d.Parent.Parent.Parent);
            Assert.IsNotNull(d.Parent.Parent.Parent.Children);
        }
    }
}
