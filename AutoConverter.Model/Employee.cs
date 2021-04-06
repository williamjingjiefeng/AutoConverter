namespace AutoConverter.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    public class Book
    {
        public string BookName { get; set; }

        public string Isbn { get; set; }
    }

    public class Child
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int Age { get; set; }

        public Dictionary<string, Book> Readings { get; set; }
    }

    public class Employee
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int Age { get; set; }

        public SexEnum Sex { get; set; }

        public List<Child> Children { get; set; }

        public HashSet<Hobby> Hobbies { get; set; }

        public Car[] Cars { get; set; }

        public Account Account { get; set; }
    }

    [Flags]
    public enum SexEnum
    {
        Unknown = 0,
        Male = 1,
        Female = 2
    }

    public class Manager : Employee
    {
        public Dictionary<string, object> Focuses { get; set; }

        public DateTimeOffset? Since { get; set; }

        public IEnumerable<Employee> Employees { get; set; }

        public Expression<Func<Car, int>> CarYearExpression { get; set; }
    }

    public class Hobby
    {
        public string Name { get; set; }
    }

    public class Car
    {
        public int Year { get; set; }
    }
}
