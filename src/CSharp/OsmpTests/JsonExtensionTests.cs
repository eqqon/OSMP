using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using eqqon.Osmp;

namespace OsmpTests
{
    namespace Test
    {
        using NUnit.Framework;

        public enum TestEnum { YES = 17, NO = 27 }

        [TestFixture]
        public class JsonExtensionsTest
        {
            [Test]
            public void GetTests()
            {
                var obj = JsonConvert.DeserializeObject<JObject>("{\"a\":\"Hello World!\", \"b\":17, \"null\":null, \"c\":\"27\", \"no\":\"NO\",\"yes\":\"YES\",}");
                Assert.AreEqual(null, obj.Get<string>("non-exist"));
                Assert.AreEqual(null, obj.Get<string>("null"));
                Assert.AreEqual("Hello World!", obj.Get<string>("a"));
                Assert.AreEqual(17, obj.Get<int>("b"));
                Assert.AreEqual(27, obj.Get<int>("c"));
                Assert.AreEqual(TestEnum.NO, obj.Get<TestEnum>("no"));
                Assert.AreEqual(TestEnum.YES, obj.Get<TestEnum>("yes"));
                Assert.AreEqual(TestEnum.NO, obj.Get<TestEnum>("c"));
                Assert.AreEqual(TestEnum.YES, obj.Get<TestEnum>("b"));
            }

            [Test]
            public void GetArray()
            {
                var obj = JsonConvert.DeserializeObject<JObject>("{\"d\":[\"x\", \"y\", \"z\"]}");
                Assert.AreEqual(new String[0], obj.GetArray<string>("non-exist"));
                Assert.AreEqual(new[] { "x", "y", "z" }, obj.GetArray<string>("d"));
            }

            [Test]
            public void ContainsKeyTests()
            {
                var obj = JsonConvert.DeserializeObject<JObject>("{\"a\":\"Hello World!\", \"b\":17, \"null\":null}");
                Assert.AreEqual(false, obj.ContainsKey("non-exist"));
                Assert.AreEqual(true, obj.ContainsKey("null"));
                Assert.AreEqual(true, obj.ContainsKey("a"));
                Assert.AreEqual(true, obj.ContainsKey("b"));
            }

            [Test]
            public void TimespanRoundtrip()
            {
                var o = new JObject { { "ts", TimeSpan.FromMinutes(1) } };
                var json = JsonConvert.SerializeObject(o);
                Console.WriteLine(json);
                var o1 = JsonConvert.DeserializeObject<JObject>(json);
                Assert.AreEqual(TimeSpan.FromMinutes(1), o1.Get<TimeSpan>("ts"));
            }

            [Test]
            public void DateTimeRoundtrip()
            {
                var now = DateTime.Now;
                var o = new JObject { { "datetime", now } };
                var json = JsonConvert.SerializeObject(o);
                Console.WriteLine(json);
                var o1 = JsonConvert.DeserializeObject<JObject>(json);
                Assert.AreEqual(now, o1.Get<DateTime>("datetime"));
            }

        }
    }
}
