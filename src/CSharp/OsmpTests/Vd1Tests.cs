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
    public class VCallCreateTests
    {
        [Test]
        public void Deserialization()
        {
            var msg = Vd1.VCallCreate("c1", new[] { "a", "b" }, new[] { "c", "d" }, 47, "test", true);
            var call = msg.Data.ToObject<Vd1.VCall>();
            Assert.NotNull(call);
            Assert.AreEqual("c1", call.CallId);
            Assert.AreEqual(new[] { "a", "b" }, call.Sources);
            Assert.AreEqual(new[] { "c", "d" }, call.Zones);
            Assert.AreEqual(47, call.Priority);
            Assert.AreEqual("test", call.Owner);
            var jresult = new JObject()
            {
                {
                    "call", new JObject()
                    {
                        { "call-id", "testcall1" },
                        { "sources", new JArray("EvacuationText") },
                        { "zones", new JArray("Floor 3", "Floor 4", "Lobby", "Staircase") },
                        { "priority", 42 },
                        { "owner", "Workstation 1" }
                    }
                }
            };
            var result = jresult.ToObject<Vd1.VCallCreateResult>();
            Assert.NotNull(result);
            call = result.Call;
            Assert.NotNull(call);
            Assert.AreEqual("testcall1", call.CallId);
            Assert.AreEqual(new[] { "EvacuationText" }, call.Sources);
            Assert.AreEqual(new[] { "Floor 3", "Floor 4", "Lobby", "Staircase" }, call.Zones);
            Assert.AreEqual(42, call.Priority);
            Assert.AreEqual("Workstation 1", call.Owner);
        }
    }

    [TestFixture]
    public class VCallListTests
    {
        [Test]
        public void Deserialization()
        {
            var call1 = new JObject()
            {
                { "call-id", "c1" },
                { "sources", new JArray("EvacuationText") },
                { "zones", new JArray("Floor 3", "Floor 4", "Lobby", "Staircase") },
                { "priority", 42 },
                { "owner", "Workstation 1" }
            };
            var call2 = new JObject()
            {
                { "call-id", "c2" },
                { "sources", new JArray("a") },
                { "zones", new JArray("b", "c") },
                { "priority", 47 },
                { "owner", "Workstation 2" }
            };
            var jresult = new JObject() { { "calls", new JArray(call1, call2) } };
            var result = jresult.ToObject<Vd1.VCallListResult>();
            Assert.NotNull(result);
            var calls = result.Calls;
            Assert.NotNull(calls);
            Assert.AreEqual(2, calls.Length);
            var c1 = calls[0];
            var c2 = calls[1];
            Assert.AreEqual("c1", c1.CallId);
            Assert.AreEqual("c2", c2.CallId);
        }
    }

    [TestFixture]
    public class VDeviceStatusTests
    {
        [Test]
        public void Deserialization()
        {
            var s1 = new JObject()
                { { "device-id", "lo.64.pa.1.1" }, { "status", "Error" }, { "sub-status", "AmplificationLow" }, };
            var s2 = new JObject() { { "device-id", "lo.64.pa.2.1" }, { "status", "Ok" }, };
            var jresult = new JObject() { { "stati", new JArray(s1, s2) } };
            var result = jresult.ToObject<Vd1.VDeviceStatusResult>();
            Assert.NotNull(result);
            var stati = result.Stati;
            Assert.NotNull(stati);
            Assert.AreEqual(2, stati.Length);
            var s10 = stati[0];
            var s20 = stati[1];
            Assert.AreEqual("lo.64.pa.1.1", s10.DeviceId);
            Assert.AreEqual("Error", s10.Status);
            Assert.AreEqual("AmplificationLow", s10.SubStatus);
            Assert.AreEqual("lo.64.pa.2.1", s20.DeviceId);
            Assert.AreEqual("Ok", s20.Status);
            Assert.AreEqual(null, s20.SubStatus);
        }
    }
}


