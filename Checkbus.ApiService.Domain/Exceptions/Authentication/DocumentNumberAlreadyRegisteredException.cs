using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Exceptions.Authentication
{
    public class DocumentNumberAlreadyRegisteredException : Exception
    {
        public DocumentNumberAlreadyRegisteredException(string message = "Document number is already registered in this organization.") : base(message) { }
    }
}
