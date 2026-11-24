using System;

namespace Checkbus.ApiService.Domain.Exceptions.Boarding
{

    public class BoardingAlreadyValidatedException : Exception
    {
        public BoardingAlreadyValidatedException(string message = "This reservation's boarding record was already resolved.") : base(message) { }
    }
}
