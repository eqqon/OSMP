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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace eqqon.Osmp
{
    public class OsmpServer : IDisposable
    {
        public OsmpServer()
        {
            Port = 443;
            ServiceUri= "/osmp/v1";
            UseSsl = true;
            TransferStats =new TransferStats();
            CommandAssemblies = new HashSet<Assembly>() {this.GetType().Assembly};
            EnabledInstructionSets = new HashSet<string>() { "standard" };
            Commands = new Dictionary<string, CommandInfo>();
            Events = new Dictionary<string, EventInfo>();
            GlobalEnv = new Dictionary<string, object>()
            {
                //{ "uri_service", UriService=new UriService() },
                //{ "cookies", Cookies=new CookieManager() },
            };
        }

        private WebSocketServer _server;
        private bool _is_up;
        //public CookieManager Cookies { get; private set; }
        //public UriService UriService { get; private set; }
        public Dictionary<string, object> GlobalEnv { get; set; }
        public HashSet<string> EnabledInstructionSets { get; private set; }
        public Dictionary<string, CommandInfo> Commands { get; private set; }
        public Dictionary<string, EventInfo> Events { get; private set; }
        public HashSet<Assembly> CommandAssemblies { get; private set; }
        public TransferStats TransferStats { get; private set; }
        public bool IsUp { get { return _is_up; } }
        public int Port { get; set; }
        public bool IsAuthenticationRequired { get; set; }
        public Dictionary<string, string> UserCredentials = new Dictionary<string, string>();
        public string ServiceUri { get; set; }


        public Action<OsmpServer> StatusChanged;
        public Action<LogLevel, Action<StringBuilder>> Log = (x, y) => { };

        public void EnableInstructionSet(string group)
        {
            EnabledInstructionSets.Add(group);
            UpdateCommandsAndEvents();
        }

        public void Startup()
        {
            if (_is_up)
                return;
            _is_up = true;
            try
            {
                UpdateCommandsAndEvents();
                _server = new WebSocketServer(Port, UseSsl) { };
                if (UseSsl)
                {
                    var rawcert = AssemblyExtensions.GetEmbeddedResourceBytes(typeof(OsmpServer).Assembly, "inton.pfx");
                    _server.SslConfiguration.ServerCertificate = new X509Certificate2(rawcert, "inton");
                }
                _server.AddWebSocketService(ServiceUri, CreateSession);
                _server.Start();
            }
            catch (Exception e)
            {
                this.Log(LogLevel.ERROR, s => s.AppendFormat("Startup failed\n" + e.PrettyPrint()));
                Shutdown(); // make sure to shut the server down if startup failed partially
            }
            if (StatusChanged != null)
                StatusChanged(this);
        }

        private WebSocketBehavior CreateSession()
        {
            var session = new OsmpSession()
            {
                Log = Log,
                Version = 1,
                Commands = Commands,
                Events = Events,
                GlobalEnv = GlobalEnv,
                TransferStats = TransferStats,
                IsAuthenticationRequired = IsAuthenticationRequired,
                UserCredentials = UserCredentials,
            };
            session.SetCrypto(_rsa);
            return session;
        }

        public void Shutdown()
        {
            _is_up = false;
            var srv = _server;
            if (srv != null)
            {
                try
                {
                    srv.Stop(CloseStatusCode.Normal, "Server Shutdown");
                }
                catch (Exception e)
                {
                    this.Log(LogLevel.ERROR, s => s.AppendFormat("Shutdown failed\n" + e.PrettyPrint()));
                }
            }
            if (StatusChanged != null)
                StatusChanged(this);
        }

        public int GetSessionCount()
        {
            return _server.WebSocketServices.SessionCount;
        }

        private void UpdateCommandsAndEvents()
        {
            // cmds
            Commands.Clear();
            foreach (var type in GetCommandTypes())
            {
                var command = CreateInstance(type) as AbstractCommand;
                if (command == null)
                    continue;
                if (!EnabledInstructionSets.Contains(command.InstructionSet))
                    continue;
                RegisterCommand(command.Name, command, type);
                if (command.Name.Contains("-"))
                    RegisterCommand(command.Name.Replace("-", ""), command, type, is_alias: true); // allow to omit all dashes from a command like: do-this as well as dothis
                foreach (var name in command.Aliases)
                {
                    RegisterCommand(name, command, type, is_alias: true);
                    if (name.Contains("-"))
                        RegisterCommand(name.Replace("-", ""), command, type, is_alias: true); // allow to omit all dashes from a command like: do-this as well as dothis

                }
            }
            // events
            Events.Clear();
            foreach (var type in GetEventTypes())
            {
                var evt = CreateInstance(type) as AbstractEvent;
                if (evt == null)
                    continue;
                if (!EnabledInstructionSets.Contains(evt.InstructionSet))
                    continue;
                RegisterEvent(evt.Name, evt, type);
            }
        }

        private object CreateInstance(Type type)
        {
            Debug.Assert(type != null, "type cannot be null!!!!");
            try
            {
                var instance= type.InvokeMember( null, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, null, new object[0]);
                return instance;
            }
            catch (TargetInvocationException e)
            {
                Log(LogLevel.ERROR, s=>s.Append("Creating instance of type " + type + " raised an error:\n"+ e.PrettyPrint()));
                throw e;
            }
        }

        private IEnumerable<Type> GetCommandTypes()
        {
            var types = new HashSet<Type>();
            foreach (var a in CommandAssemblies.ToArray())
            {
                foreach(var type in a.FindTypesByAttribute<OsmpCommandAttribute>())
                    if (type.IsSubclassOf(typeof(AbstractCommand)))
                        types.Add(type);
            }
            return types;
        }

        private IEnumerable<Type> GetEventTypes()
        {
            var types = new HashSet<Type>();
            foreach (var a in CommandAssemblies.ToArray())
            {
                foreach (var type in a.FindTypesByAttribute<OsmpEventAttribute>())
                    if (type.IsSubclassOf(typeof(AbstractEvent)))
                        types.Add(type);
            }
            return types;
        }

        private void RegisterCommand(string name, AbstractCommand c, Type type, bool is_alias = false)
        {
            if (Commands.ContainsKey(name))
            {
                var existing = Commands[name];
                if (existing.Version > c.Version) // if version is equal, overwrite (due to compatibility). to ensure against overwriting, increase version number of the command.
                    return;
            }
            Commands[name] = new CommandInfo
            {
                Name = name,
                InstructionSet = c.InstructionSet,
                Version = c.Version,
                Type = type,
                IsAlias = is_alias,
            };
        }

        private void RegisterEvent(string name, AbstractEvent c, Type type)
        {
            if (Events.ContainsKey(name))
            {
                var existing = Events[name];
                if (existing.Version > c.Version) // if version is equal, overwrite (due to compatibility). to ensure against overwriting, increase version number of the command.
                    return;
            }
            Events[name] = new EventInfo
            {
                Name = name,
                InstructionSet = c.InstructionSet,
                Version = c.Version,
                Type = type,
                Description = c.Description,
            };
        }

        private OsmpSession[] GetSessions()
        {
            return _server.WebSocketServices[ServiceUri].Sessions.Sessions.OfType<OsmpSession>().ToArray();
        }

        public void SendEvent(AbstractEvent ev)
        {
            foreach (var session in GetSessions())
                session.SendEvent(ev);
        }

        public void SendEvent(string name, Func<OsmpSession, AbstractEvent> event_func)
        {
            foreach(var session in GetSessions())
                session.SendEvent(name, event_func);
        }

        #region --> RSA Crypto

        public string PublicKey { get { return _rsa.ToXmlString(includePrivateParameters: false); } }
        public string JsonConfiguration { get; set; }
        public bool UseSsl { get; set; }

        private RSACryptoServiceProvider _rsa = new RSACryptoServiceProvider();

        public void LoadKeypair(string key_pair_xml)
        {
            _rsa.FromXmlString(key_pair_xml);
            var was_up = _is_up;
            Shutdown();
            if (was_up)
                Startup();
        }

        #endregion

        public void Dispose()
        {
            Shutdown();
            _rsa.Dispose();
        }

    }


    public class ClientMessage : OsmpMessage
    {
        //public string Type { get; set; } // msg type

        //public int Nr { get; set; } // msg sequence number

        //public string Id { get; set; } // cmd id

        //public JObject Data { get; set; } // cmd params

        public JObject ResultData { get; set; } // response data

        public bool IsError { get; set; } // response status

        public string Result { get; set; } // response status message
    }
}
