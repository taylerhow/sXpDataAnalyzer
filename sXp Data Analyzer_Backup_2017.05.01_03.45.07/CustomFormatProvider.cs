using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sXp_Data_Analyzer
{
    class CustomFormatProvider : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
            {
                return this;
            }

            return null;
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            double value;

            if (!double.TryParse(arg.ToString(), out value))
            {
                throw new ArgumentNullException("arg");
            }

            return string.Format("{0:" + format + "}", value);
        }
    }
}
