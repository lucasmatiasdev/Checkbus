using System;

namespace Checkbus.ApiService.Domain.Exceptions.Trips
{

    public class EventNotFoundException : Exception
    {
        public EventNotFoundException(string message = "Event not found.") : base(message) { }
    }
}
