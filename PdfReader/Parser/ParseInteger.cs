using System.Text;

namespace PdfReader
{
    public class ParseInteger : ParseObjectBase
    {
        public ParseInteger(TokenInteger token)
        {
            Value = token.Value;
        }

        public ParseInteger(int value)
        {
            Value = value;
        }

        public int Value { get; private set; }
    }
}
