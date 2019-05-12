/*
 * You may use Open System Management Protocol and its Reference Implementation free of charge as long as you honor 
 * the protocol specification. You may not use, license, distribute or advertise the protocol or any derivations of 
 * it under a different name. 

 * The Open System Management Protocol is Copyright © by Eqqon GmbH

 * THE PROTOCOL AND ITS REFERENCE IMPLEMENTATION ARE PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN 
 * ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE PROTOCOL OR ITS REFERENCE 
 * IMPLEMENTATION OR THE USE OR OTHER DEALINGS IN THE PROTOCOL OR REFERENCE IMPLEMENTATION.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Osmp
{
    public static class Vd1
    {
        #region --> VCallCreate

        public static OsmpMessage VCallCreate(string call_id, string[] sources, string[] zones, int priority = 41, string owner = null, bool start = false)
        {
            var call_data = new JObject {{"call-id", call_id}, {"priority", priority}, {"owner", owner},};
            if (sources!=null)
                call_data["sources"] = new JArray(sources.OfType<object>());
            if (zones != null)
                call_data["zones"] = new JArray(zones.OfType<object>());
            if (start)
                call_data["start"] = true;
            return new OsmpMessage() { Id = "vcall-create", Type = "cmd", Data = call_data };
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class VCallCreateResult
        {
            [JsonProperty("call")]
            public VCall Call { get; set; }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class VCall
        {
            [JsonProperty("call-id")]
            public string CallId { get; set; }

            [JsonProperty("sources")]
            public string[] Sources { get; set; }

            [JsonProperty("zones")]
            public string[] Zones { get; set; }

            [JsonProperty("priority")]
            public int Priority { get; set; }

            [JsonProperty("owner")]
            public string Owner { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("last-started")]
            public string LastStarted { get; set; }
        }

        [TestFixture]
        public class VCallCreateTests
        {
            [Test]
            public void Deserialization()
            {
                var msg = VCallCreate("c1", new[] {"a", "b"}, new[] {"c", "d"}, 47, "test", true);
                var call=msg.Data.ToObject<VCall>();
                Assert.NotNull(call);
                Assert.AreEqual("c1", call.CallId);
                Assert.AreEqual(new[] { "a", "b" }, call.Sources);
                Assert.AreEqual(new[] { "c", "d" }, call.Zones);
                Assert.AreEqual(47, call.Priority);
                Assert.AreEqual("test", call.Owner);
                var jresult=new JObject() { {"call", new JObject() {
                    { "call-id", "testcall1" }, 
                    { "sources", new JArray("EvacuationText") }, 
                    { "zones", new JArray("Floor 3", "Floor 4", "Lobby", "Staircase") }, 
                    { "priority", 42 }, 
                    { "owner", "Workstation 1"}} } };
                var result = jresult.ToObject<VCallCreateResult>();
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

        #endregion

        #region --> VCallEdit

        public static OsmpMessage VCallEdit(string call_id, string[] sources=null, string[] zones=null)
        {
            var call_data = new JObject { { "call-id", call_id },  };
            if (sources != null)
                call_data["sources"] = new JArray(sources.OfType<object>());
            if (zones != null)
                call_data["zones"] = new JArray(zones.OfType<object>());
            return new OsmpMessage() { Id = "vcall-create", Type = "cmd", Data = call_data };
        }

        public static OsmpMessage VCallEdit(string call_id, int priority)
        {
            var call_data = new JObject { { "call-id", call_id }, { "priority", priority } };
            return new OsmpMessage() { Id = "vcall-create", Type = "cmd", Data = call_data };
        }

        public static OsmpMessage VCallEdit(string call_id, string owner)
        {
            var call_data = new JObject { { "call-id", call_id }, { "owner", owner } };
            return new OsmpMessage() { Id = "vcall-create", Type = "cmd", Data = call_data };
        }

        #endregion

        #region --> VCallPlay

        public static OsmpMessage VCallPlay(string call_id)
        {
            var call_data = new JObject { { "call-id", call_id }, };
            return new OsmpMessage() { Id = "vcall-play", Type = "cmd", Data = call_data };
        }

        #endregion

        #region --> VCallStop

        public static OsmpMessage VCallStop(string call_id)
        {
            var call_data = new JObject { { "call-id", call_id }, };
            return new OsmpMessage() { Id = "vcall-stop", Type = "cmd", Data = call_data };
        }

        public static OsmpMessage VCallStopAll()
        {
            var call_data = new JObject { { "call-id", "*" }, };
            return new OsmpMessage() { Id = "vcall-stop", Type = "cmd", Data = call_data };
        }

        #endregion

        #region --> VCallList

        public static OsmpMessage VCallList(string filter=null)
        {
            var data = new JObject {};
            if (filter != null)
                data["filter"] = filter;
            return new OsmpMessage() { Id = "vcall-list", Type = "cmd", Data = data };
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class VCallListResult
        {
            [JsonProperty("calls")]
            public VCall[] Calls { get; set; }
        }


        [TestFixture]
        public class VCallListTests
        {
            [Test]
            public void Deserialization()
            {
                var call1 = new JObject() {
                    { "call-id", "c1" },
                    { "sources", new JArray("EvacuationText") },
                    { "zones", new JArray("Floor 3", "Floor 4", "Lobby", "Staircase") },
                    { "priority", 42 },
                    { "owner", "Workstation 1"}};
                var call2 = new JObject() {
                    { "call-id", "c2" },
                    { "sources", new JArray("a") },
                    { "zones", new JArray("b", "c") },
                    { "priority", 47 },
                    { "owner", "Workstation 2"}};
                var jresult = new JObject() { {"calls", new JArray(call1, call2)}  };
                var result = jresult.ToObject<VCallListResult>();
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

        #endregion

        #region --> VCallStatus

        public static OsmpMessage VCallStatus(string call_id)
        {
            var call_data = new JObject { { "call-id", call_id }, };
            return new OsmpMessage() { Id = "vcall-status", Type = "cmd", Data = call_data };
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class VCallStatusResult
        {
            [JsonProperty("call")]
            public VCall Call { get; set; }
        }

        #endregion

        #region --> VDeviceStatus

        public static OsmpMessage VDeviceStatus(string device_id, bool ignore_not_installed=true, string include_filter = null, string exclude_filter=null)
        {
            var data = new JObject { { "device-id", device_id }, {"ignore-not-installed", ignore_not_installed} };
            if (!string.IsNullOrWhiteSpace(include_filter))
                data["include-filter"] = include_filter;
            if (!string.IsNullOrWhiteSpace(exclude_filter))
                data["exclude-filter"] = exclude_filter;
            return new OsmpMessage() { Id = "vdevice-status", Type = "cmd", Data = data };
        }

        public static OsmpMessage VDeviceStatusAll(bool ignore_not_installed = true, string include_filter = null, string exclude_filter = null)
        {
            return VDeviceStatus("*", ignore_not_installed, include_filter, exclude_filter);
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class VDeviceStatusResult
        {
            [JsonProperty("stati")]
            public VDeviceStatusObj[] Stati { get; set; }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class VDeviceStatusObj
        {
            [JsonProperty("device-id")]
            public string DeviceId { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("sub-status")]
            public string SubStatus { get; set; }
        }

        [TestFixture]
        public class VDeviceStatusTests
        {
            [Test]
            public void Deserialization()
            {
                var s1 = new JObject() {{ "device-id", "lo.64.pa.1.1" }, { "status", "Error"}, { "sub-status", "AmplificationLow"},};
                var s2 = new JObject() { { "device-id", "lo.64.pa.2.1" }, { "status", "Ok" }, };
                var jresult = new JObject() { { "stati", new JArray(s1, s2) } };
                var result = jresult.ToObject<VDeviceStatusResult>();
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

        #endregion

        #region --> VDeviceStatusFilter

        public static OsmpMessage VDeviceStatusFilter()
        {
            return new OsmpMessage() { Id = "vdevice-status-filter", Type = "cmd" };
        }

        public static OsmpMessage VDeviceStatusFilter(bool ignore_not_installed, string include_filter, string exclude_filter)
        {
            var data = new JObject { { "ignore-not-installed", ignore_not_installed }, { "include-filter" , include_filter}, { "exclude-filter", exclude_filter } };
            return new OsmpMessage() { Id = "vdevice-status-filter", Type = "cmd", Data = data };
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class VDeviceStatusFilterResult
        {
            [JsonProperty("ignore-not-installed")]
            public bool IgnoreNotInstalled { get; set; }

            [JsonProperty("include-filter")]
            public string IncludeFilter { get; set; }

            [JsonProperty("exclude-filter")]
            public string ExcludeFilter { get; set; }
        }

        #endregion

    }

}
