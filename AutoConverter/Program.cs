using System;
using AutoConverter.BusinessObjects.Core.BusinessEntities.V2;

namespace AutoConverter
{
    class Program
    {
        private static void Main(string[] args)
        {
            var def = new EntityMappingDefinition<CustomerResult, Customer>("Customer");

            // simple field mapping: same type on either end
            def.From(z => z.Desc).To(z => z.Name);
            def.From(z => z.Age).To(z => z.Age);

            // To() end supports multiple layer of member expression such as z.Preference.Hobby
            def.From(z => z.Leisure).To(z => z.Preference.Hobby);

            // complex field mapping: intermediate transformation via Then(), in which transformation func GetLoyalty is called.
            def.From(z => z.YearsWithUs).Then(GetLoyalty).To(z => z.Loyalty);

            // define string representation through predefined Stringify() func. The end result will be a dictionary.
            def.From(z => z.Account).To(z => z.Account).Stringify(z =>
                $"AccountId:{z.AccountId}, AccountNumber:{z.AccountNumber}");

            var customerResult = new CustomerResult
            {
                Desc = "Joe",
                Leisure = "GO",
                Age = 73,
                YearsWithUs = 8,
                Account = new Account
                {
                    AccountId = 123,
                    AccountNumber = "978654321"
                },
            };

            // covert from customerResult to customer
            var customer = def.Convert(customerResult);

            var stringResult = def.Stringify(customerResult);

            // show how we copy the same type of object
            var copyDefinition = new EntityMappingDefinition<Customer, Customer>("customer");
            copyDefinition.From(z => z.Name).To(z => z.Name);
            copyDefinition.From(z => z.Age).To(z => z.Age);

            var newCustomer = new Customer
            {
                Loyalty = Loyalty.Level1,
                Account = new Account
                {
                    AccountId = 125,
                    AccountNumber = "123456789"
                }
            };

            // copy from converted customer to a new customer object we just initialized and see how properties are merged
            copyDefinition.Copy(customer, newCustomer);

            Console.ReadLine();
        }

        private static Loyalty GetLoyalty(int yearsWithUs)
        {
            if (yearsWithUs > 10)
            {
                return Loyalty.Level1;
            }

            return yearsWithUs > 5 ? Loyalty.Level2 : Loyalty.Level3;
        }
    }
}
