using JsonWriterList.Test.Model;
using System;
using System.Collections.Generic;
using Xunit;

namespace JsonWriterList.Test
{
    public class ListTest
    {
        [Fact]
        public void N001_JsonWriterList_Should_Generate_Valid_Json()
        {
            //Arrange
            var listSource = new List<ClientTest>()
            {
                new ClientTest("Name 1", "Last Name 1", 1),
                new ClientTest("Name 2", "Last Name 2", 2),
                new ClientTest("Name 3", "Last Name 3", 3)
            };
            var jsonWriter = new JsonWriterList<>

        }
    }
}
