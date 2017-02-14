using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;
using System.Linq;

using static WPO.Tests.Utils.WPOTestClasses;

namespace WPO.Tests.MSSqlConnectionTests
{
    [TestClass]
    public class InheritanceTests : MSSQLTestsBase
    {
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            Contract.Requires(testContext != null);
            TestsInitialization();
        }

        [TestMethod]
        public void SingleTableInheritanceTest()
        {
            Employee2 e = wpoManager.GetQuery<Employee2>(session).OrderBy("id").Take(1).Single();

            Employee2 _e = new Employee2(session)
            {
                Id = e.Id + 1,
                Age = 37,
                Company = "Google",
                Name = "Larry Page",
                Salary = 450000,
                Position = "CEO"
            };

            session.Commit();

            Employee2 __e = wpoManager.GetQuery<Employee2>(session2).OrderBy("id").Take(1).Single();

            Assert.AreEqual(_e.GetWPOHashCode(), __e.GetWPOHashCode());
        }

        [TestMethod]
        public void ClassTableInheritanceSelectTest()
        {
            Student s = wpoManager.GetQuery<Student>(session).GetObjectByKey(1);

            Assert.IsNotNull(s);
            Assert.AreEqual(1, s.Id);
            Assert.AreEqual("Nowak", s.Name);
            Assert.AreEqual("AGH", s.University);
        }

        [TestMethod]
        public void ClassTableInheritanceInsertTest()
        {
            Person p = wpoManager.GetQuery<Person>(session).OrderBy("id").Take(1).Single();

            Student _s = new Student(session)
            {
                StudentId = p.Id + 1,
                Age = 25,
                University = "MIT",
                Name = "John Smith"                
            };

            session.Commit();

            Student __s = wpoManager.GetQuery<Student>(session2).OrderBy("id").Take(1).Single();

            Assert.IsNotNull(__s);
            Assert.AreEqual(_s.Age, __s.Age);
            Assert.AreEqual(_s.University, __s.University);
            Assert.AreEqual(_s.Name, __s.Name);
        }

        [TestMethod]
        public void ClassTableInheritanceUpdateTest()
        {
            Person p = wpoManager.GetQuery<Person>(session).OrderBy("id").Take(1).Single();

            Student _s = new Student(session)
            {
                StudentId = p.Id + 1,
                Age = 25,
                University = "MIT",
                Name = "John Smith"
            };

            session.Commit();

            Student __s = wpoManager.GetQuery<Student>(session2).OrderBy("id").Take(1).Single();
            __s.Age += 10;
            __s.University += " Cambridge";
            __s.Name += " 2";

            session2.Commit();

            Student ___s = wpoManager.GetQuery<Student>(session3).OrderBy("id").Take(1).Single();

            Assert.AreEqual(__s.Age, ___s.Age);
            Assert.AreEqual(__s.University, ___s.University);
            Assert.AreEqual(__s.Name, ___s.Name);
        }
    }
}
