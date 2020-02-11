using System;
using System.Collections.Generic;
using System.Text;

namespace JsonWriterList.Test.Model
{
    public class ClientTest
    {
        public ClientTest(string name, string lastName, int accountNumber)
        {
            Name = name;
            LastName = lastName;
            AccountNumber = accountNumber;
        }
        public string Name { get; set; }
        public string LastName { get; set; }
        public int AccountNumber { get; set; }

        public override int GetHashCode()
        {
            return AccountNumber;
        }
    }
}
