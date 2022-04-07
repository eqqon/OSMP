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

namespace eqqon.Osmp
{
    public static class Standard
    {
        #region --> Echo

        public static OsmpMessage Echo(object token = null)
        {
            if (token==null)
                return new OsmpMessage() { Id = "echo", Type = "cmd" };
            return new OsmpMessage() { Id = "echo", Type = "cmd", Data = new JObject { {"token", JToken.FromObject(token)} } };
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class EchoResult
        {
            [JsonProperty("token")]
            public object Token { get; set; }
        }

        #endregion

        #region --> Time

        public static OsmpMessage Time()
        {
             return new OsmpMessage() { Id = "time", Type = "cmd", };
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class TimeResult
        {
            [JsonProperty("date-time")]
            public DateTime DateTime { get; set; }
        }

        #endregion

        #region --> Wait

        public static OsmpMessage Wait(CancellationToken? token=null)
        {
            return new OsmpMessage() { Id = "wait", Type = "cmd", CancellationToken = token ?? CancellationToken.None };
        }

        public static OsmpMessage Wait(double seconds, CancellationToken? token=null)
        {
            return new OsmpMessage() { Id = "wait", Type = "cmd", CancellationToken = token ?? CancellationToken.None, Data = new JObject { "seconds", seconds } };
        }

        #endregion

        #region --> ActiveCmds

        public static OsmpMessage ActiveCmds()
        {
            return new OsmpMessage() { Id = "active-cmds", Type = "cmd" };
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class ActiveCmdsResult
        {
            [JsonProperty("now")]
            public DateTime Now { get; set; }

            [JsonProperty("cmds")]
            public IEnumerable<ActiveCommand> Commands { get; set; }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class ActiveCommand
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("cmd-nr")]
            public int CmdNr { get; set; }

            [JsonProperty("start-time")]
            public DateTime StartTime { get; set; }
        }

        #endregion

        #region --> EventList

        public static OsmpMessage EventList()
        {
            return new OsmpMessage() { Id = "event-list", Type = "cmd" };
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class EventListResult
        {
            [JsonProperty("now")]
            public DateTime Now { get; set; }

            [JsonProperty("events")]
            public IEnumerable<EventInfo> Events { get; set; }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class EventInfo
        {
            [JsonProperty("event")]
            public string EventName { get; set; }

            [JsonProperty("instruction-set")]
            public string InstructionSet { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("subscribed-until")]
            public DateTime? SubscribedUntil { get; set; }

            public bool IsSubscribed { get { return SubscribedUntil != null; }}
        }


        #endregion

        #region --> EventSubscribe

        public static OsmpMessage EventSubscribe(string event_name, DateTime? timeout=null)
        {
            var data = new JObject {{"event", event_name}};
            if (timeout != null)
                data["timeout"] = timeout.Value;
            return new OsmpMessage() { Id = "event-subscribe", Type = "cmd", Data=data };
        }

        public static OsmpMessage EventSubscribeAll(DateTime? timeout = null)
        {
            var data = new JObject { { "event", "*" } };
            if (timeout != null)
                data["timeout"] = timeout.Value;
            return new OsmpMessage() { Id = "event-subscribe", Type = "cmd", Data = data };
        }

        #endregion

        #region --> EventUnsubscribe

        public static OsmpMessage EventUnsubscribe(string event_name)
        {
            var data = new JObject { { "event", event_name } };
            return new OsmpMessage() { Id = "event-unsubscribe", Type = "cmd", Data = data };
        }

        public static OsmpMessage EventUnsubscribeAll()
        {
            var data = new JObject { { "event", "*" } };
            return new OsmpMessage() { Id = "event-unsubscribe", Type = "cmd", Data = data };
        }

        #endregion


        //#region --> Login / Logout

        //public static OsmpMessage Login(string username,  string plaintext_password, string server_public_key)
        //{
        //    string encrypted_pw = null;
        //    using (var rsa=new RSACryptoServiceProvider())
        //    {
        //        rsa.FromXmlString(server_public_key);
        //        encrypted_pw=Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(plaintext_password), false));
        //    }
        //    return new OsmpMessage() { Id = "login", Type = "cmd", Data = new JObject { { "username", username }, { "encrypted-password", encrypted_pw } } };
        //}

        //public static OsmpMessage Logout()
        //{
        //    return new OsmpMessage() { Id = "logout", Type = "cmd",  };
        //}

        //#endregion

    }
}
