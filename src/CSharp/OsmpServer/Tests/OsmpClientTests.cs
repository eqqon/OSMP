using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Osmp.Extensions;

namespace Osmp.Tests
{
    [TestFixture]
    public class OsmpClientTests
    {
        [Test]
        public void ReceiveMultipleEvents()
        {
            var rx =
                "{“type”:“event”,“nr”:4,“data”:{“events”:[{“event”:“vcall-status-changed”,“data”:{“date-time”:\"2017-12-20T09:33:34.6885268+01:00\",“call-id”:“testcall1”,“status”:“Disconnected”,“last-started”:\"2017-12-20T09:33:03.9487686+01:00\"}},{“event”:“vdevice-status-changed”,“data”:{“date-time”:\"2017-12-20T09:33:34.6935271+01:00\",“stati”:[{“device-id”:\"lo.200.ho.0\",“status”:“Online”},{“device-id”:\"lo.201.sc.0\",“status”:“Online”},{“device-id”:\"lo.66.sc.0\",“status”:“Error”},{“device-id”:\"lo.66.ds.1.1\",“status”:“Ok”},{“device-id”:\"lo.66.ds.2.1\",“status”:“Error”,“sub-status”:“MicDefect”},{“device-id”:\"lo.66.pa.41.1\",“status”:“Ok”},{“device-id”:\"lo.66.pa.42.1\",“status”:“Ok”},{“device-id”:\"lo.66.pa.43.1\",“status”:“Ok”},{“device-id”:\"lo.66.pa.44.1\",“status”:“Ok”},{“device-id”:\"lo.66.pa.1.1\",“status”:“Ok”},{“device-id”:\"lo.66.pa.2.1\",“status”:“Error”,“sub-status”:“AmplificationLow”},{“device-id”:\"lo.66.pa.3.1\",“status”:“Ok”,“sub-status”:“DistortionHigh”},{“device-id”:\"lo.66.pa.4.1\",“status”:“Ok”,“sub-status”:“AmplificationHigh”},{“device-id”:\"lo.55.sc.0\",“status”:“Error”},{“device-id”:\"lo.55.ds.1.1\",“status”:“Ok”},{“device-id”:\"lo.55.ds.2.1\",“status”:“Error”,“sub-status”:“MicDefect”},{“device-id”:\"lo.55.pa.41.1\",“status”:“Ok”},{“device-id”:\"lo.55.pa.42.1\",“status”:“Ok”},{“device-id”:\"lo.55.pa.43.1\",“status”:“Ok”},{“device-id”:\"lo.55.pa.44.1\",“status”:“Ok”},{“device-id”:\"lo.55.pa.1.1\",“status”:“Ok”},{“device-id”:\"lo.55.pa.2.1\",“status”:“Error”,“sub-status”:“AmplificationLow”},{“device-id”:\"lo.55.pa.3.1\",“status”:“Ok”,“sub-status”:“DistortionHigh”},{“device-id”:\"lo.55.pa.4.1\",“status”:“Ok”,“sub-status”:“AmplificationHigh”}]}}]}}";
            rx = rx.Replace('“', '"').Replace('”', '"');
            var client = new OsmpClient();
            OsmpEvent[] events = null;
            client.Error += (msg, e) => Assert.Fail(msg + "\n" + e.PrettyPrint());
            client.EventsReceived += (ev) => events = ev; // note: the event needs to be handled or the client won't even bother parsing the message.
            client.SimulateReceiveText(rx);
            Assert.NotNull(events);
        }
    }
}
