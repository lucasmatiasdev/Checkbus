using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Exceptions.Storage
{
    public class InvalidStorageKeyException : Exception
    {
        public InvalidStorageKeyException(string message = "Invalid storage key provided.") : base(message){}
    }
}
