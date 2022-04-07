using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eqqon.Osmp;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace OsmpTests
{

    [TestFixture]
    public class EventListTests
    {
        [Test]
        public void Deserialization()
        {
            var e1 = new Standard.EventInfo()
            {
                EventName = "bomb-exploded",
                Description = "Fired only once",
                InstructionSet = "Explosives"
            };
            var e2 = JObject.FromObject(e1).ToObject<Standard.EventInfo>();
            Assert.AreEqual("bomb-exploded", e2.EventName);
            Assert.AreEqual("Fired only once", e2.Description);
            Assert.AreEqual("Explosives", e2.InstructionSet);
            Assert.AreEqual(null, e2.SubscribedUntil);
            var now = DateTime.Now;
            e2.SubscribedUntil = now;
            var e3 = JObject.FromObject(e2).ToObject<Standard.EventInfo>();
            Assert.AreEqual(now, e3.SubscribedUntil.Value);
        }
    }
}
