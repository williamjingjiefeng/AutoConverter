using System.Collections.Generic;

namespace AutoConverter.Model
{
    public class Customer
    {
        public string Name { get; private set; }
        public int Age { get; private set; }
        public Preference Preference { get; set; }

        public Account Account { get; set; }

        public Customer()
        {
            Preference = new Preference();
            Account = new Account();
        }

        public Loyalty Loyalty { get; set; }

        public string Mobile { get; set; }


        public List<Child> Children { get; set; }

        public HashSet<Hobby> Hobbies { get; set; }

        public Car[] Cars { get; set; }
    }

    public enum Loyalty
    {
        Level1 = 0,

        Level2 = 1,

        Level3 = 2
    }

    public class Preference
    {
        public string Hobby { get; set; }
    }

    public class CustomerResult
    {
        public string Desc { get; set; }
        public int Age { get; set; }
        public string Leisure { get; set; }

        public Account Account { get; set; }

        public int YearsWithUs { get; set; }
    }

    public class Account
    {
        public int AccountId { get; set; }
        public string AccountNumber { get; set; }
    }
}
