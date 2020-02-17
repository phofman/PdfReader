using System;
using System.Globalization;
using System.Text;

namespace PdfReader
{
    public class ParseString : ParseObjectBase
    {
        public ParseString(TokenStringLiteral token)
        {
            Token = token;
            IsHex = false;
        }

        public ParseString(TokenStringHex token)
        {
            Token = token;
            IsHex = true;
        }

        public bool IsHex { get; private set; }

        public string Value
        {
            get { return IsHex ? Token.Raw : Token.Resolved; }
        }

        public byte[] ValueAsBytes
        {
            get { return Token.ResolvedAsBytes; }
        }

        public string BytesToString(byte[] bytes)
        {
            return Token.BytesToString(bytes);
        }

        private TokenString Token { get; set; }
    }
}
