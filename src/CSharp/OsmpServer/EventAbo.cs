using System;

namespace Osmp
{
    public class EventAbo
    {
        public AbstractEvent Event { get; set; }
        public DateTime RegistrationTime { get; set; }
        public DateTime ExpireTime { get; set; }
    }
}