using System.Collections.Generic;
using AutoConverter.BusinessObjects.Core.BusinessEntities.V2;
using AutoConverter.Model;

namespace AutoConverter
{
    public class Demo2
    {
        public static void Run()
        {
            var child = new Child
            {
                FirstName = "Adam",
                LastName = "Hwang",
                Age = 12,
                Readings = new Dictionary<string, Book>
                {
                    {"StarWars", new Book{BookName = "StarWars", Isbn = "1234"}},
                    {"Gone With the Wind",  new Book{BookName = "Gone With the Wind", Isbn = "5678"}}
                }
            };

            var employee = new Employee
            {
                FirstName = "Bill",
                LastName = "Hwang",
                Age = 41,
                Children = new List<Child>
                {
                    child
                },
                Sex = SexEnum.Male,
                Account = new Account
                {
                    AccountNumber = "1234"
                }
            };

            var def = new EntityMappingDefinition<Employee, Customer>("");
            def.From(z => z.Account.AccountNumber).To(z => z.Account.AccountNumber);

            var customer = def.Convert(employee);

        }
    }
}
