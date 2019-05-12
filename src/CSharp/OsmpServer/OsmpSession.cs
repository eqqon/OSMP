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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Osmp.Extensions;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Osmp
{
    public class OsmpSession : WebSocketBehavior, IDisposable
    {
        public OsmpSession()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(10);
            SessionData = new Dictionary<string, object>();
            _event_abos = new Dictionary<string, EventAbo>();
            _event_timer = new Timer(OnEventTimerTick);
            _consistency_check_timer = new Timer(OnCheckConsistencyTick);
        }

        public int Version = 1;

        public TimeSpan ConnectionTimeout { get; set; }
        public Dictionary<string, object> GlobalEnv { get; set; } // shared by all shell servers
        public Dictionary<string, object> SessionData { get; set; } // this holds data shared between commands of the same client connection
        public DateTime LastReceived { get; set; }
        public Dictionary<string, CommandInfo> Commands { get; set; }
        public Dictionary<string, EventInfo> Events { get; set; }
        private Dictionary<string, EventAbo> _event_abos;
        private Dictionary<int, AbstractCommand> _active_commands = new Dictionary<int, AbstractCommand>();

        public bool IsAuthenticationRequired { get; set; }
        public Dictionary<string, string> UserCredentials = new Dictionary<string, string>();
        public string LoggedInUser { get; private set; }
        public bool IsAuthenticated { get; private set; }

        public Dictionary<string, EventAbo> EventAbos
        {
            get
            {
                lock (_event_abos)
                    return _event_abos.ToDictionary(pair => pair.Key, pair => pair.Value);
            }
        }

        public Action<LogLevel, Action<StringBuilder>> Log = (x, y) => { };
        private Timer _consistency_check_timer;
        private Timer _event_timer;
        private bool _event_timer_set;
        private Queue<AbstractEvent> _events_to_send = new Queue<AbstractEvent>();
        private int _command_counter;

        public string ClientAddress
        {
            get
            {
                var session = Sessions[ID];
                return session.Context.UserEndPoint.Address.ToString();
            }
        }

        public string TransferDir { get; set; }
        public TransferStats TransferStats { get; set; }

        protected override void OnOpen()
        {
            base.OnOpen();
            this.Log(LogLevel.DEBUG, s => s.Append("OPEN "));
            _consistency_check_timer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            var data = new JObject { { "protocol", "Open System Management Protocol" }, { "version", Version }, { "date-time", DateTime.Now }, { "public-key", PublicKey } };
            SendText(JsonConvert.SerializeObject(new JObject { { "type", "event" }, { "id", "session-initiated" }, { "nr", Interlocked.Increment(ref _nr) }, { "data", data } }));
        }

        private int _nr = 0;

        protected override void OnClose(CloseEventArgs e)
        {
            _consistency_check_timer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
            this.Log(LogLevel.DEBUG, s => s.AppendFormat("CLOSE Clean: {0}, Code: {1}, Reason: {2}", e.WasClean, e.Code, e.Reason));
            base.OnClose(e);
            Dispose();
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            LastReceived = DateTime.Now;
            this.Log(LogLevel.DEBUG, s => s.Append("RX: " + e.Data));
            Interlocked.Add(ref TransferStats.RxBytes, e.RawData.Length);
            if (string.IsNullOrWhiteSpace(e.Data))
                return; // just ignore            
            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(e.Data);
                if (obj == null)
                    SendError("Received 'null'!");
                var type = obj.Get<string>("type");
                var msg = new ClientMessage()
                {
                    Type = type,
                    Id = obj.Get<string>("id"),
                    Nr = obj.Get<int>("nr"),
                    Data = obj.Get<JObject>("data"),
                };
                if (IsAuthenticationRequired && !IsAuthenticated && type != "response" && msg.Id != "login")
                {
                    msg.IsError = true;
                    msg.Result = "Authentication required!";
                    Respond(msg);
                    return;
                }
                switch (type)
                {
                    case "cmd":
                        ExecuteCmd(msg);
                        break;
                    case "response":
                        ReceiveServerCmdResponse(obj);
                        break;
                    case "cancel":
                        CancelCommands(msg);
                        break;
                    default:
                        msg.IsError = true;
                        msg.Result = "Invalid message type: " + type;
                        Respond(msg);
                        break;
                }
            }
            catch (Exception ex)
            {
                var msg = "Unable to decode message, wrong formatting? Got this: '" + e.Data + "'\n" + ex.PrettyPrint();
                this.Log(LogLevel.ERROR, s => s.Append(msg));
                try { SendError(msg); }
                catch { }
            }
        }

        private void ReceiveServerCmdResponse(JObject obj)
        {
            LastReceived = DateTime.Now;
            var response = obj.ToObject<OsmpResponse>();
            OsmpMessage cmd = null;
            lock (_waitingCommands)
            {
                if (_waitingCommands.TryGetValue(response.CmdNr, out cmd))
                    _waitingCommands.Remove(response.CmdNr);
            }
            response.Command = cmd;
            if (cmd != null)
                cmd.TaskSource.SetResult(response);
            //if (CmdResponseReceived != null)
            //    CmdResponseReceived(response);
        }

        public object CreateInstance(Type type)
        {
            Debug.Assert(type != null, "type cannot be null!!!!");
            try
            {
                var instance = type.InvokeMember(null, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, null, new object[0]);
                return instance;
            }
            catch (TargetInvocationException e)
            {
                Log(LogLevel.ERROR, s => s.Append("Creating instance of type " + type + " raised an error:\n" + e.PrettyPrint()));
                throw e;
            }
        }

        public async void ExecuteCmd(ClientMessage command)
        {
            LastReceived = DateTime.Now;
            var cmd = command.Id;
            if (string.IsNullOrWhiteSpace(cmd))
            {
                command.IsError = true;
                command.Result = "Invalid command.";
                //ShellProtocol.SendCommandAnswer(this, command);
                //ShellProtocol.SendPrompt(this, Prompt);
                Respond(command);
                return;
            }
            if (!Commands.ContainsKey(cmd))
            {
                command.IsError = true;
                command.Result = string.Format("Error command '{0}' not found", cmd);
                Respond(command);
                //// note: if this empty cmd did cancel a previous command, avoid sending a double prompt
                //if (cancel == null)
                //    ShellProtocol.SendPrompt(this, Prompt);
                return;
            }
            try
            {
                var cmd_instance = CreateInstance(Commands[cmd].Type) as AbstractCommand;
                if (cmd_instance == null)
                {
                    command.IsError = true;
                    command.Result = string.Format("Error command '{0}' not found", cmd);
                }
                else
                {
                    cmd_instance.Session = this;
                    cmd_instance.CmdNr = command.Nr;
                    cmd_instance.Params = command.Data ?? new JObject();
                    cmd_instance.Log = Log;
                    cmd_instance.StartTime = DateTime.Now;
                    StartCmd(cmd_instance);
                    CommandResult result = null;
                    try
                    {
                        result = await cmd_instance.Execute().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        command.IsError = true;
                        command.Result = e.PrettyPrint();
                    }
                    finally
                    {
                        StopCmd(cmd_instance);
                        if (result != null)
                        {
                            if (result.IsSuccess == false)
                                command.IsError = true;
                            command.Result = result.Description;
                            command.ResultData = result.Data;
                        }
                        cmd_instance.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                command.IsError = true;
                command.Result = e.PrettyPrint();
            }
            Respond(command);
        }

        private void Respond(ClientMessage command)
        {
            // Interlocked.Increment(ref _command_counter);
            var msg = new JObject { { "type", "response" }, { "id", command.Id }, { "nr", Interlocked.Increment(ref _command_counter) }, { "cmd-nr", command.Nr }, { "status", command.IsError ? "ERROR" : "OK" } };
            if (!string.IsNullOrWhiteSpace(command.Result))
                msg["result"] = command.Result;
            if (command.ResultData != null && command.ResultData.Count > 0)
                msg["data"] = command.ResultData;
            SendText(JsonConvert.SerializeObject(msg));
        }

        private void SendText(string text)
        {
            Interlocked.Add(ref TransferStats.TxBytes, Encoding.UTF8.GetBytes(text).Length);
            SendAsync(text, (x) => { });
        }

        private void SendError(string message)
        {
            var msg = new JObject { { "nr", Interlocked.Increment(ref _command_counter) }, { "type", "event" }, { "id", "error" }, { "data", new JObject { { "description", message } } }, };
            SendText(JsonConvert.SerializeObject(msg));
        }

        public void SendStream(AbstractCommand cmd, JObject data)
        {
            var msg = new JObject
            {
                { "type", "stream" },
                { "nr", Interlocked.Increment(ref _command_counter) },
                { "cmd-nr", cmd.CmdNr },
                { "id", cmd.Name },
                { "data", data },
            };
            SendText(JsonConvert.SerializeObject(msg));
        }

        private void OnCheckConsistencyTick(object state)
        {
            try
            {
                CheckServerSideCmdTimeout();
                CheckActiveCommands();
                CheckEventSubscriptions();
            }
            catch (Exception e)
            {
                Log(LogLevel.ERROR, s => s.Append("OnCheckConsistencyTick: " + e.PrettyPrint()));
            }
        }

        #region --> ICommandServer interface


        public Task<Dictionary<string, object>> AskQuestion(string question, Dictionary<string, object> @params)
        {
            return null;
        }

        public Task<bool?> AskForConfirmation(string question, string description, string yes_text, string cancel_text, string no_text = null)
        {
            return null;
        }

        public Task<string> AskForPassword(string challenge = "Please enter password")
        {
            return null;
        }

        public Task AskOpenScriptEditor(string script_id, string script_code)
        {
            return null;
        }

        public Task<JObject> AskUploadFile(string upload_id)
        {
            return null;
        }

        public Task<JObject> AskUploadFiles(string upload_id)
        {
            return null;
        }

        public Task<string> AskForUploadPart(string upload_id, int part)
        {
            return null;
        }

        public Task<bool> SendFilePart(string filename, int part, int total_parts, string base64)
        {
            return null;
        }

        public Task<CommandResult> SendFile(string filepath, CancellationToken cancellation_token, bool cleanup = false)
        {
            return null;
        }

        public void NotifyQuestionAnswerReceived(OsmpMessage answer)
        {
        }

        public void SendEventToAllClients(string event_name, Dictionary<string, object> data)
        {

        }

        //public void SendEventsToAllClients(Event[] events)
        //{
        //}

        #endregion

        #region --> Events & Subscriptions

        /// <summary>
        /// Send the same event instance to all clients
        /// </summary>
        /// <param name="ev">The event instance to send</param>
        public void SendEvent(AbstractEvent ev)
        {
            if (ev == null)
                return;
            lock (_event_abos)
            {
                if (!_event_abos.ContainsKey(ev.Name) || _event_abos[ev.Name].ExpireTime < DateTime.Now)
                    return;
            }
            lock (_event_timer)
            {
                _events_to_send.Enqueue(ev);
                SetEventTimer();
            }
        }

        /// <summary>
        /// Create a new event instance for every client and send. This allows to send session dependent data 
        /// and is also very efficient because no event instance is allocated if no client subscribed the event
        /// </summary>
        /// <param name="name">The event name</param>
        /// <param name="event_func">The create func</param>
        public void SendEvent(string name, Func<OsmpSession, AbstractEvent> event_func)
        {
            if (event_func == null)
                return;
            lock (_event_abos)
            {
                if (!_event_abos.ContainsKey(name) || _event_abos[name].ExpireTime < DateTime.Now)
                    return;
            }
            try
            {
                var ev = event_func(this);
                if (ev == null)
                    return;
                lock (_event_timer)
                {
                    _events_to_send.Enqueue(ev);
                    SetEventTimer();
                }
            }
            catch (Exception e)
            {
                Log(LogLevel.ERROR, s => s.Append("SendEvent: " + e.PrettyPrint()));
            }
        }

        private void SendEventLimited(AbstractEvent ev)
        {
            var msg = new JObject
            {
                { "type", "event" },
                { "nr", Interlocked.Increment(ref _command_counter) },
                { "id", ev.Name},
                { "data",  ev.Data},
            };
            SendText(JsonConvert.SerializeObject(msg));
        }

        public void SendEvents(IEnumerable<AbstractEvent> events)
        {
            if (events == null)
                return;
            lock (_event_timer)
            {
                foreach (var ev in events)
                {
                    if (ev == null)
                        continue;
                    lock (_event_abos)
                    {
                        if (!_event_abos.ContainsKey(ev.Name) || _event_abos[ev.Name].ExpireTime < DateTime.Now)
                            continue;
                    }
                    _events_to_send.Enqueue(ev);
                }
                SetEventTimer();
            }
        }

        private void SetEventTimer()
        {
            lock (_event_timer)
            {
                if (_events_to_send.Count == 0)
                    return;
                if (_event_timer_set)
                    return;
                _event_timer_set = true;
                _event_timer.Change(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(-1));
            }
        }

        public void SendEventsLimited(Queue<AbstractEvent> events)
        {
            if (events == null || events.Count == 0)
                return;
            var array = new JArray(events.Select(e => new JObject { { "event", e.Name }, { "data", e.Data } }));
            var msg = new JObject
            {
                { "type", "event" },
                { "nr", Interlocked.Increment(ref _command_counter) },
                { "data", new JObject{ { "events", array}}},
            };
            SendText(JsonConvert.SerializeObject(msg));
        }

        private void OnEventTimerTick(object state)
        {
            try
            {
                lock (_event_timer)
                {
                    _event_timer_set = false;
                    if (_events_to_send.Count == 0)
                        return;
                    var events = _events_to_send;
                    _events_to_send = new Queue<AbstractEvent>();
                    if (events.Count == 1)
                        SendEventLimited(events.Dequeue());
                    else
                        SendEventsLimited(events);
                }
            }
            catch (Exception e)
            {
                Log(LogLevel.ERROR, s => s.Append("OnCheckConsistencyTick: " + e.PrettyPrint()));
            }
        }


        private void CheckEventSubscriptions()
        {
            var now = DateTime.Now;
            lock (_event_abos)
            {
                foreach (var pair in _event_abos.ToArray())
                {
                    var ev = pair.Key;
                    var until = pair.Value.ExpireTime;
                    if (now > until) {
                        // remove outdated abos
                        EventAbo abo = null;
                        if (_event_abos.ContainsKey(ev))
                            abo = _event_abos[ev];
                        _event_abos.Remove(ev); 
                        if (abo != null)
                            abo.Event.OnUnregister();
                    }
                }
            }
        }
        public void SubscribeEvent(EventInfo event_info, TimeSpan timeout)
        {
            if (event_info == null)
                return;
            var t = DateTime.Now;
            var ev = CreateInstance(event_info.Type) as AbstractEvent;
            if (ev == null)
                return;
            lock (_event_abos)
                _event_abos[event_info.Name] = new EventAbo() { Event = ev, RegistrationTime = t, ExpireTime = (DateTime.Now + timeout) };
            InitEvent(ev);
            ev.OnRegister();
        }

        public void UnsubscribeEvent(EventInfo event_info)
        {
            if (event_info == null)
                return;
            EventAbo abo = null;
            lock (_event_abos)
            {
                if (_event_abos.ContainsKey(event_info.Name))
                    abo = _event_abos[event_info.Name];
                _event_abos.Remove(event_info.Name);
            }
            if (abo != null)
                abo.Event.OnUnregister();
        }

        private void InitEvent(AbstractEvent ev)
        {
            ev.Session = this;
        }


        #endregion

        #region --> Active Commands & Cancellation


        private void StartCmd(AbstractCommand command)
        {
            lock (_active_commands)
            {
                _active_commands[command.CmdNr] = command;
            }
        }

        private void StopCmd(AbstractCommand command)
        {
            lock (_active_commands)
            {
                _active_commands.Remove(command.CmdNr);
            }
        }

        private void CheckActiveCommands()
        {
            var cmd_nrs = new JArray();
            var now = DateTime.Now;
            lock (_active_commands)
            {
                foreach (var pair in _active_commands)
                {
                    var nr = pair.Key;
                    var cmd = pair.Value;
                    if (now - cmd.StartTime < TimeSpan.FromSeconds(2))
                        continue;
                    cmd_nrs.Add(nr);
                }
            }
            if (cmd_nrs.Count == 0)
                return;
            var msg = new JObject
            {
                {"type", "event"},
                {"nr", Interlocked.Increment(ref _command_counter)},
                {"id", "session-status"},
                {"data", new JObject {{"active-cmds", cmd_nrs}}},
            };
            SendText(JsonConvert.SerializeObject(msg));
        }

        private void CancelCommands(ClientMessage msg)
        {
            var cmds_token = msg.Data["cmds"];
            if (cmds_token.Type == JTokenType.Array)
            {
                foreach (var cmd_nr in cmds_token.Values<int>())
                    CancelCmd(cmd_nr);
                return;
            }
            if (cmds_token.Type == JTokenType.String && cmds_token.Value<string>() == "*")
            {
                CancelAllCmds();
            }
        }

        private void CancelAllCmds()
        {
            AbstractCommand[] cmds;
            lock (_active_commands)
            {
                cmds = _active_commands.Values.ToArray();
                _active_commands.Clear();
            }
            foreach (var cmd in cmds)
                cmd.Cancel();
        }

        private void CancelCmd(int cmdNr)
        {
            AbstractCommand cmd;
            lock (_active_commands)
            {
                if (!_active_commands.ContainsKey(cmdNr))
                    return;
                cmd = _active_commands[cmdNr];
                _active_commands.Remove(cmdNr);
            }
            cmd.Cancel();
        }

        public AbstractCommand[] GetActiveCmds()
        {
            lock (_active_commands)
            {
                return _active_commands.Values.ToArray();
            }
        }

        #endregion

        #region --> Authentication


        public bool Login(string username, string token, string encrypted_pw)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(encrypted_pw))
                return false;
            if (!UserCredentials.ContainsKey(username))
                return false;
            var pw_plus_token = Encoding.UTF8.GetString(_rsa.Decrypt(Convert.FromBase64String(encrypted_pw), false));
            var pw = pw_plus_token.Replace(token, "");
            if (UserCredentials[username] != pw)
                return false;
            IsAuthenticated = true;
            LoggedInUser = username;
            return true;
        }

        public bool LoginWithPublicKey(string username, string pkey, string token, string signature)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(pkey) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(signature))
                return false;
            if (!UserCredentials.ContainsKey(username))
                return false;
            if (UserCredentials[username] != pkey)
                return false;
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(pkey);
                if (!rsa.VerifyData(Encoding.UTF8.GetBytes(token), CryptoConfig.MapNameToOID("SHA1"), Convert.FromBase64String(signature)))
                    return false;
            }
            IsAuthenticated = true;
            LoggedInUser = username;
            return true;

        }

        public void Logout()
        {
            LoggedInUser = null;
            IsAuthenticated = false;
        }


        public string PublicKey { get { return _rsa.ToXmlString(includePrivateParameters: false); } }

        private RSACryptoServiceProvider _rsa = new RSACryptoServiceProvider();

        public void SetCrypto(RSACryptoServiceProvider rsa)
        {
            _rsa = rsa;
        }


        #endregion

        #region --> Server side commands


        Dictionary<int, OsmpMessage> _waitingCommands = new Dictionary<int, OsmpMessage>();

        public async Task<OsmpResponse> SendCommand(string command, JObject @params = null)
        {
            var msg = new OsmpMessage { Type = "cmd", Id = command, Data = @params };
            var task_source = new TaskCompletionSource<OsmpResponse>();
            msg.SendTimestamp = DateTime.Now;
            msg.TaskSource = task_source;
            msg.Nr = Interlocked.Increment(ref _command_counter);
            var json = JsonConvert.SerializeObject(msg);
            lock (_waitingCommands)
                _waitingCommands[msg.Nr] = msg;
            SendText(json);
            return await task_source.Task.ConfigureAwait(false);
        }

        public void CheckServerSideCmdTimeout()
        {
            // check waiting server side commands for timeout
            var timeout = ConnectionTimeout;
#if DEBUG
            if (Debugger.IsAttached)
                timeout = TimeSpan.FromSeconds(60);
#endif
            OsmpMessage[] commands;
            lock (_waitingCommands)
                commands = _waitingCommands.Values.ToArray();
            var now = DateTime.Now;
            int timed_out = 0;
            int not_timed_out = 0;
            foreach (var msg in commands)
            {
                if (now - msg.SendTimestamp > timeout && now - msg.StreamReceivedTimestamp > timeout)
                {
                    timed_out++;
                    lock (_waitingCommands)
                        _waitingCommands.Remove(msg.Nr);
                    var task_source1 = msg.TaskSource;
                    if (task_source1 != null)
                        task_source1.TrySetResult(new OsmpResponse() { Command = msg, Status = "TIMEOUT", Result = "The message timed out: " + msg.Id });
                }
                else
                    not_timed_out++;
            }
        }

        #endregion

        public void Dispose()
        {
            foreach(var abo in _event_abos.Values)
                abo.Event.OnUnregister();
            _event_abos.Clear();
            if (_event_timer != null)
                _event_timer.Dispose();
            if (_consistency_check_timer != null)
                _consistency_check_timer.Dispose();
            _active_commands.Clear();
            SessionData.Clear();
            //Commands.Clear(); // must not be cleared!
            //Events.Clear(); // must not be cleared!
        }

    }
}
