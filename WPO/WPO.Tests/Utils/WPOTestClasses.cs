using System;
using WPO.Attributes;
using WPO.Enums;

namespace WPO.Tests.Utils
{
    public static class WPOTestClasses
    {
        [WPOTable("dbo.Products")]
        public class Product : WPOBaseObject
        {
            public Product(Session s) : base(s) { }

            [WPOPrimaryKey("ProductID")]
            public int Id { get; set; }

            [WPOColumn("ProductName")]
            [WPOSize(40)]
            public string Name { get; set; }

            [WPOColumn("Price")]
            [WPOSize(7, 2)]
            public decimal Price { get; set; }

            [WPOColumn("ProductDescription")]
            [WPOSize(200)]
            public string ProductDescription { get; set; }

        }

        [WPOTable("dbo.Brand")]
        public class Brand : WPOBaseObject, IEquatable<Brand>
        {
            public Brand(Session s) : base(s) { }

            [WPOPrimaryKey("Id", true, "BrandId")]
            public long Id { get; set; }

            [WPOColumn("Name")]
            [WPOSize(40)]
            public string Name { get; set; }

            [WPOColumn("IsNew")]
            public bool? IsNew { get; set; }

            [WPOColumn("Type")]
            public short CustomType { get; set; }

            [WPOColumn("Date")]
            public DateTime? Date { get; set; }

            public override bool Equals(object obj) => Equals(obj as Brand);

            public bool Equals(Brand other) => Id == other.Id &&
                                               Name == other.Name &&
                                               IsNew == other.IsNew &&
                                               CustomType == other.CustomType &&
                                               Date == other.Date;

            public override int GetHashCode() => base.GetHashCode();
        }


        [WPOTable]
        public class Simple1 : WPOBaseObject
        {
            public Simple1(Session s) : base(s) { }

            [WPOPrimaryKey]
            public int Id { get; set; }

            [WPOColumn]
            [WPOSize(40)]
            public string Name { get; set; }

            public WPOCollection<Simple2> Children2 { get; set; }
        }

        [WPOTable]
        public class Simple2 : WPOBaseObject
        {
            public Simple2(Session s) : base(s) { }

            [WPOPrimaryKey]
            public int Id { get; set; }

            [WPORelation(RelationType.OneToMany, "id", "simple1id")]
            public Simple1 Parent { get; set; }

            public WPOCollection<Simple3> Children3 { get; set; }

            public WPOCollection<Simple4> Children4 { get; set; }
        }

        [WPOTable]
        public class Simple3 : WPOBaseObject
        {
            public Simple3(Session s) : base(s) { }

            [WPOPrimaryKey]
            public int Id { get; set; }

            [WPORelation(RelationType.OneToMany, "id", "simple2id")]
            public Simple2 Parent { get; set; }
        }

        [WPOTable]
        public class Simple4 : WPOBaseObject
        {
            public Simple4(Session s) : base(s) { }

            [WPOPrimaryKey]
            public int Id { get; set; }

            [WPORelation(RelationType.OneToMany, "id", "simple2id")]
            public Simple2 Parent { get; set; }

            [WPORelation(RelationType.OneToMany, "id", "simple4id")]
            public Simple4 Other { get; set; }
        }

        [WPOTable]
        public class Simple5 : WPOBaseObject
        {
            public Simple5(Session s) : base(s) { }

            [WPOPrimaryKey]
            public int Id { get; set; }

            [WPORelation(RelationType.OneToOne, "id", "simple5id")]
            public Simple5 Other { get; set; }
        }


        [WPOTable]
        public class Table1 : WPOBaseObject
        {
            public Table1(Session s) : base(s) { }

            [WPOPrimaryKey]
            public int Id { get; set; }

            [WPOColumn("customname")]
            [WPOSize(40)]
            public string Name { get; set; }

            [WPOColumn]
            [WPOSize(7,2)]
            public decimal Value { get; set; }
        }


        [WPOTable]
        public class A : WPOBaseObject
        {
            public A(Session s) : base(s) { }

            [WPOPrimaryKey]
            public int Id { get; set; }

            public WPOCollection<B> Children { get; set; }
        }

        [WPOTable]
        public class B : WPOBaseObject
        {
            public B(Session s) : base(s) { }

            [WPOPrimaryKey]
            public int Id { get; set; }

            [WPORelation(RelationType.OneToMany, "id", "aid")]
            public A Parent { get; set; }

            public WPOCollection<C> Children { get; set; }
        }

        [WPOTable]
        public class C : WPOBaseObject
        {
            public C(Session s) : base(s) { }

            [WPOPrimaryKey]
            public int Id { get; set; }

            [WPORelation(RelationType.OneToMany, "id", "bid")]
            public B Parent { get; set; }

            public WPOCollection<D> Children { get; set; }
        }

        [WPOTable]
        public class D : WPOBaseObject
        {
            public D(Session s) : base(s) { }

            [WPOPrimaryKey]
            public int Id { get; set; }

            [WPORelation(RelationType.OneToMany, "id", "cid")]
            public C Parent { get; set; }            
        }


        [WPOTable]
        public class Person : WPOBaseObject
        {
            public Person(Session s) : base(s) { }

            [WPOPrimaryKey]
            public int Id { get; set; }

            [WPOColumn]
            public string Name { get; set; }

            [WPOColumn]
            public int Age { get; set; }
        }

        [WPOTable(Inheritance = InheritanceType.SingleTable)]
        public class Employee : Person
        {
            public Employee(Session s) : base(s) { }
                        
            [WPOColumn]
            [WPOSize(7,2)]
            public decimal Salary { get; set; }

            [WPOColumn]
            [WPOSize(40)]
            public string Company { get; set; }
        }

        [WPOTable(Inheritance = InheritanceType.SingleTable)]
        public class Employee2 : Employee
        {
            public Employee2(Session s) : base(s) { }

            [WPOColumn]
            [WPOSize(20)]
            public string Position { get; set; }
        }

        [WPOTable(Inheritance = InheritanceType.ClassTable)]
        public class Student : Person
        {
            public Student(Session s) : base(s) { }

            [WPOPrimaryKey]
            public int StudentId { get; set; }
            
            [WPOColumn]
            [WPOSize(40)]
            public string University { get; set; }
        }
    }
}
