using System;
using System.Data.SqlClient;
using CommandAsSql.System;
using Xunit;

/// <summary>
/// Unit test for the CommandAsSql lib
/// </summary>
namespace XUnitTestCommandAsSql
{
    public class UnitTest1
    {
        /// <summary>
        /// Test a regular sql string with an int parameter
        /// </summary>
        [Fact(DisplayName = "Compare Command With Int Parameter")]
        public void Compare_Command_With_Int_Parameter()
        {
            // arrange
            const string Expected = "select * from products where productid = 1";
            var sc = new SqlCommand("select * from products where productid = @id");
            sc.Parameters.AddWithValue("@id", 1); // if you forget the @ here, the test fails. Give your thoughts about this in the GitHub issues section please.

            // act
            var c = sc.CommandAsSql().Replace(Environment.NewLine, string.Empty); // not too happy about removing the newline

            // assert
            Assert.Equal(Expected, c);
        }

        [Fact(DisplayName = "Compare Command With String Parameter")]
        public void Compare_Command_With_String_Parameter()
        {
            // arrange
            const string Expected = "select * from products where productname = 'myname'";
            var sc = new SqlCommand("select * from products where productname = @name");
            sc.Parameters.AddWithValue("@name", "myname"); // if you forget the @ here, the test fails. Give your thoughts about this in the GitHub issues section please.

            // act
            var c = sc.CommandAsSql().Replace(Environment.NewLine, string.Empty); // not too happy about removing the newline

            // assert
            Assert.Equal(Expected, c);
        }

        // todo: add more tests for structured, stored proc etc.
    }
}
