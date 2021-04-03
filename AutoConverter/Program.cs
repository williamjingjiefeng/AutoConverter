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

            // define string representation through predefined Stringfy() func. The end result will be a dictionary.
            def.From(z => z.Account).To(z => z.Account).Stringfy(z =>
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

            var result = def.Convert(customerResult);

            var stringResult = def.Stringfy(customerResult);

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
