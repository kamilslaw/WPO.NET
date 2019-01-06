
## ORM for .NET Framework 4.6 written in C#
Basic features:
 - MS SQL support
 - Relations to objects mapping:
	 - Basic queries constructing
	 - Handles relationships between tables (one to one, one to many), including cycle references & deep composition
	 - Supports an inheritance in 2 ways: *single table* & *class table*
 - Transaction mechanism
 - *INSERT*, *UPDATE* & *DELETE* operations

***THIS IS THE PROJECT FOR STUDIES, NOT FOR COMMERCIAL USE !!***

## Mapping
Basic data class:
```csharp
[WPOTable("dbo.Products")]  // obligatory attribute for data classes; provides name of the table witch schema
public class Product : WPOBaseObject {  // each data class has to inherit from WPOBaseObject
    public Product(Session s) : base(s) { } // constructor indicates in which session the object exists

    [WPOPrimaryKey("ProductID")]  // indicates primary key and provides column name; there is no support for compound keys
    public int Id { get; set; }

    [WPOColumn("ProductName")]  // indicates that this property has a corresponding column; if not given, a property name is taken 
    [WPOSize(40)]  // indicates field length
    public string Name { get; set; }

    [WPOColumn]
    [WPOSize(7, 2)] // 7 is total field length, 2 is fractional part length
    public decimal Price { get; set; }

}
```

Get a single row from a table with id equals to 1
```csharp
Product prod = wpoManager.GetQuery<Product>(session).GetObjectByKey(1);
```

More complex query (in *WHERE* method the argument must have correct SQL syntax, except *!=* & *==* operators, which are converted)
```csharp
string phrase = "bicycle";
List<Product> products = wpoManager.GetQuery<Product>(session)
                                   .Where("ProductDescription LIKE '" + phrase + "'")
                                   .Where("ProductName LIKE '" + phrase + "'", isAlternative: true)
                                   .OrderBy("price")
                                   .Skip(20)
                                   .Take(10)
                                   .ToList();
```

### Sequences
Obtaining [sequences](https://www.sqlshack.com/sequence-objects-in-sql-server/) values (which can be used as primary keys values)
```csharp
var sequences = new Dictionary<string, int>() {
	["sek1"] = 3,
	["sek2"] = 2
};  // we want to get 3 next values of "sek1" & 2 values of "sek2"
List<ExecuteResult<long> result = connection.GetSequences(sequences).ToList();
/* Sample result:
   [["sek1"] = 1347,
    ["sek1"] = 1348,
    ["sek1"] = 1349,
    ["sek2"] = 44,
    ["sek2"] = 45]
*/
```

### Relationships
```csharp
[WPOTable]
public class Car : WPOBaseObject {
	public Car(Session s) : base(s) { }
	[WPOPrimaryKey]
	public int Id { get; set; }
	[WPOColumn]
	public string Name { get; set; }
	public WPOCollection<Wheel> Wheels { get; set; }
}

[WPOTable]
public class Wheel : WPOBaseObject {
	public Wheel(Session s) : base(s) { }
	[WPOPrimaryKey]
	public int Id { get; set; }
	[WPORelation(RelationType.OneToMany, "id", "carid")]  // relation type, column from connected table & column name
	public Car Parent { get; set; }  
	[WPORelation(RelationType.OneToOne, "id", "netxwheelid")]
	public Wheel Other { get; set; }
}

// ...

WPOManager.Configuration = new WPOConfiguration() { DependencyDepth = 2 }; // set max loaded composition depth (-1 means loading whole object tree)
var wheel = wpoManager.GetQuery<Wheel>(session).FirstOrDefault();
Assert.IsNotNull(wheel);
Assert.IsNotNull(wheel.Parent);
Assert.IsNull(wheel.Other.Parent);
wheel.Other.LoadDependencies(); // load directly nested objects, in this case 'Parent' & 'Other' wheel
Assert.IsNotNull(wheel.Other.Parent);

```

### Inheritance
```csharp
// ALL UNRELATES ATTRIBUTES & CONSTRUCTOR HAVE BEEN OMITTED FOR READABILITY
public class Person : WPOBaseObject {
	public int Id { get; set; }
	public string Name { get; set; }
}
```
#### Single table
```csharp
public class Student : Person {
	public int Age { get; set; }
}
public class Teacher : Person {
	public decimal Salary { get; set; }
}
```
is an equivalent to

| Student       | Teacher  |
| ------------- | -------- |
| Id *[PK]* | Id *[PK]* |
| Name | Name  |
| Age | Salary |

#### Class table
```csharp
[WPOTable(Inheritance = InheritanceType.ClassTable)]
public class Student : Person {
	public int Age { get; set; }
}  
[WPOTable(Inheritance = InheritanceType.ClassTable)]
public class Teacher : Person {
	public decimal Salary { get; set; }
}
```
is an equivalent to

| Person        | Student       | Teacher  |
| ------------- | ------------- | ------   |
| Id *[PK]* | Id *[PK/FK]* | Id *[PK/FK]* |
| Name | Age | Salary |

## Configuration
**WPOManager** is the singleton which manages sessions and allows us to create queries. It is responsible for sessions life cycle, it implements *IDisposable*. Sample class which handles *WPOManager* and creates 1 session:
```csharp
public static class DBManager {
	private static MSSqlConnection connection;
	private static string connectionString = "...";

	public static WPOManager Manager { get; set; }
	public static Session Session { get; set; }

	public static void Initialize()	{
        connection = new MSSqlConnection();
		Manager = WPOManager.GetInstance();
		Session = Manager.GetSession(connection, connectionString);
	}
}

// ...

var pr = DBManager.Manager.GetQuery<Product>(DBManager.Session).GetObjectByKey(model.Id);
var car = new Car(DBManager.Session) { Id = 44, Name = "Audi", Value = pr.Salary * 12 };
DBManager.Session.Commit();
```

## Transaction
Under the transaction (*Session* class) we are able to create, update & delete objects.
```csharp
session.Commit()  // apply changes to database
session.Rollback()  // remove objects which are not commited yet, undo changes in edited objects 
```
Sample transaction:
```csharp
var = wpoManager.GetQuery<Product>(session).GetObjectByKey(44);
product.Price *= 2;
var promotion = new Promotion(session) { Id = 133, Name = "Holidays", Start = DateTime.UtcNow };
var prevPromotion = wpoManager.GetQuery<Promotion>(session).OrderBy("Start").FirstOrDefault();
prevPromotion?.Remove();
session.Commit();
```
