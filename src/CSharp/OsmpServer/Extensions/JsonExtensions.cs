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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Osmp
{
    public static class JsonExtensions
    {
        public static T Get<T>(this JObject self, string field)
        {
            if (self == null)
                throw new ArgumentException("self must not be null");
            JToken token;
            if (!self.TryGetValue(field, out token))
                return default(T);
            if (typeof(T) == typeof(TimeSpan))
            {
                TimeSpan ts = TimeSpan.Zero;
                TimeSpan.TryParse(token.Value<string>(), CultureInfo.InvariantCulture, out ts);
                return (T)((object)ts);
            }
            if (typeof (T).IsEnum)
            {
                if (token.Type == JTokenType.Integer)
                    return (T)Enum.ToObject(typeof(T),token.Value<int>());
                else if (token.Type == JTokenType.String)
                    return (T)Enum.Parse(typeof(T), token.Value<string>());
                else
                    return (T)Enum.ToObject(typeof(T), 0);
            }
            return token.Value<T>();
        }

        public static T[] GetArray<T>(this JObject self, string field)
        {
            if (self == null)
                throw new ArgumentException("self must not be null");
            JToken token;
            if (!self.TryGetValue(field, out token))
                return new T[0];
            return token.Values<T>().ToArray();
        }

        public static bool ContainsKey(this JObject self, string field)
        {
            if (self == null)
                throw new ArgumentException("self must not be null");
            JToken token;
            return self.TryGetValue(field, out token);
        }
    }

    namespace Test
    {
        using NUnit.Framework;

        public enum TestEnum { YES=17, NO=27 } 

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
                Assert.AreEqual(new []{"x", "y", "z"}, obj.GetArray<string>("d"));
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
                var o = new JObject { { "ts", TimeSpan.FromMinutes(1) }};
                var json=JsonConvert.SerializeObject(o);
                Console.WriteLine(json);
                var o1 = JsonConvert.DeserializeObject<JObject>(json);
                Assert.AreEqual(TimeSpan.FromMinutes(1), o1.Get<TimeSpan>("ts"));
            }

            [Test]
            public void DateTimeRoundtrip()
            {
                var now = DateTime.Now;
                var o = new JObject { { "datetime",  now} };
                var json = JsonConvert.SerializeObject(o);
                Console.WriteLine(json);
                var o1 = JsonConvert.DeserializeObject<JObject>(json);
                Assert.AreEqual(now, o1.Get<DateTime>("datetime"));
            }

        }
    }
}
