using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Exceptions.Authentication
{
    public class EmailGenerationExhaustedException : Exception
    {
        public EmailGenerationExhaustedException(string message = "Could not generate a unique email address after the maximum number of attempts.") : base(message) { }
    }
}
