using System;

namespace Checkbus.ApiService.Domain.Exceptions.Boarding
{

    public class BoardingRecordNotFoundException : Exception
    {
        public BoardingRecordNotFoundException(string message = "Boarding record not found.") : base(message) { }
    }
}
