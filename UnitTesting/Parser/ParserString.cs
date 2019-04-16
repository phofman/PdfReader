using PdfXenon.Standard;
using System;
using System.Text;
using System.IO;
using Xunit;
using UnitTesting;
using System.Collections.Generic;
using System.Globalization;

namespace ParserUnitTesting
{
    public class ParserString : HelperMethods
    {
        [Fact]
        public void OneSectionUTC1()
        {
            CultureInfo provider = CultureInfo.InvariantCulture;

            var x = DateTime.Now.ToString(provider);
            ParseString s = new ParseString(new TokenStringLiteral(0L, "19700102030405+00:00"));
            Assert.True(s.ValueAsDateTime == new DateTime(1970, 1, 2, 3, 4, 5));
        }

        [Fact]
        public void OneSectionUTC2()
        {
            CultureInfo provider = CultureInfo.InvariantCulture;

            var x = DateTime.Now.ToString(provider);
            ParseString s = new ParseString(new TokenStringLiteral(0L, "D:19700102030405+00:00"));
            Assert.True(s.ValueAsDateTime == new DateTime(1970, 1, 2, 3, 4, 5));
        }

        [Fact]
        public void OneSectionPlus1()
        {
            CultureInfo provider = CultureInfo.InvariantCulture;

            var x = DateTime.Now.ToString(provider);
            ParseString s = new ParseString(new TokenStringLiteral(0L, "19700102030405+01:00"));
            Assert.True(s.ValueAsDateTime == new DateTime(1970, 1, 2, 3, 4, 5));
        }

        [Fact]
        public void OneSectionMinus1()
        {
            CultureInfo provider = CultureInfo.InvariantCulture;

            var x = DateTime.Now.ToString(provider);
            ParseString s = new ParseString(new TokenStringLiteral(0L, "19700102030405-01:00"));
            Assert.True(s.ValueAsDateTime == new DateTime(1970, 1, 2, 3, 4, 5));
        }
    }
}
