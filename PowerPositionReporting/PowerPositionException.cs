using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerPositionReporting
{
    public class PowerPositionException : Exception
    {
        public PowerPositionException(string message, Exception innerException) 
            : base (message, innerException)
        {
        }
    }
}
