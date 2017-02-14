using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.Contracts;

using static WPO.Tests.Utils.WPOTestClasses;

namespace WPO.Tests.MSSqlConnectionTests
{
    /// <summary>
    /// Summary description for GetSequenceTests
    /// </summary>
    [TestClass]
    public class GetSequenceTests : MSSQLTestsBase
    {
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            Contract.Requires(testContext != null);
            TestsInitialization();
        }

        #region Test Methods

        [TestMethod]
        public void EmptySequenceTest()
        {
            // Arrange
            Dictionary<string, int> sequences = new Dictionary<string, int>();

            // Act
            var result = connection.GetSequences(sequences).ToList();
            connection.Close();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void BasicSequenceTest()
        {
            // Arrange
            Dictionary<string, int> sequences = new Dictionary<string, int>()
            {
                ["sek1"] = 1,
                ["sek2"] = 1
            };

            // Act
            var result = connection.GetSequences(sequences).ToList();
            connection.Close();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(result[0].Key, "sek1");
            Assert.AreEqual(result[1].Key, "sek2");
            Assert.AreNotEqual(result[0].Value, 0);
            Assert.AreNotEqual(result[1].Value, 0);
        }

        [TestMethod]
        public void MultipleSequencesTest()
        {
            // Arrange
            int[] sek = { 7, 12, 4 };
            Dictionary<string, int> sequences = new Dictionary<string, int>()
            {
                ["sek1"] = sek[0],
                ["sek2"] = sek[1],
                ["sek3"] = sek[2]
            };

            // Act
            var result = connection.GetSequences(sequences).ToList();
            connection.Close();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(sek.Sum(), result.Count);

            int offset = 0;
            for (int i = 0; i < sek.Sum(); i++)
            {
                Assert.AreEqual(result[i].Key, "sek" + ((offset % 3) + 1));

                sek[offset % 3]--;
                do
                {
                    offset++;
                } while (sek[offset % 3] == 0);
            }
        }

        #endregion Test Methods
    }
}
