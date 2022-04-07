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
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;


namespace eqqon.Osmp
{
    public class OsmpClient : IDisposable
    {
        public OsmpClient()
        {
            LastReceived = DateTime.MinValue;
            ConnectionTimeout = TimeSpan.FromSeconds(10);
            Address = "ws://localhost:443/osmp/v1";
            CertValidationCallback = (o, cert, chain, sslPolicyErrors) => true;
        }

        /// <summary>
        /// Set this callback to validate the server certificate.  
        /// If not set all certificates (even invalid ones) are accepted.
        /// </summary>
        public RemoteCertificateValidationCallback CertValidationCallback { get; set; }

        /// <summary>
        /// Last time the client received a message from the server
        /// </summary>
        public DateTime LastReceived { get; private set; }

        /// <summary>
        /// Connection and command timeout. This must be the same as in OsmpServer. Changing it is not recommended
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; }

        private Timer _timer; // timeout checking timer

        /// <summary>
        /// Public key of the OsmpServer. Will be set automatically on connect. 
        /// If preset before connect connecting to a server which sends a different 
        /// public key will result in an error on connect.
        /// </summary>
        public string ServerPublicKey { get; set; }

        private WebSocket _client;
        public WebSocket WebSocket { get { return _client; } }

        /// <summary>
        /// The server address that will be used by Connect
        /// </summary>
        public string Address { get; set; }

        private TaskCompletionSource<bool> _connectCompletionSource;
        private DateTime _connectTimeStamp;

        Dictionary<int, OsmpMessage> _waitingCommands = new Dictionary<int, OsmpMessage>();


        private int _msg_counter = 0; // incremental message counter

        /// <summary>
        /// Raised whenever an exception or other error occurs
        /// </summary>
        public event Action<string, Exception> Error; // (error_msg, exception)=>

        /// <summary>
        /// Raised whenever the client sends something. 
        /// </summary>
        public event Action<string, bool> MessageSent; // (msg, success)=>

        /// <summary>
        /// Raised whenever the client receives something. 
        /// </summary>
        public event Action<string> MessageReceived; // msg =>

        /// <summary>
        /// Raised whenever the connection state changed
        /// </summary>
        public event Action ConnectionStateChanged;

        public event Action<OsmpResponse> CmdResponseReceived;
        public event Action<OsmpStream> CmdStreamReceived;
        public event Action<OsmpEvent[]> EventsReceived;

        /// <summary>
        /// Connect to the OsmpServer through the configured Address
        /// </summary>
        /// <returns>True on success</returns>
        public async Task<bool> Connect()
        {
            var cl = _client;
            if (cl != null && cl.IsAlive)
                return true;
            if (cl != null)
                cl.Close();
            _client = new WebSocket(Address) { EmitOnPing = true };
            // always trust the cert, even if it is self signed
            if (_client.IsSecure)
                _client.SslConfiguration.ServerCertificateValidationCallback = CertValidationCallback;
            _client.OnMessage += OnReceive;
            _client.OnError += OnError;
            _client.OnClose += OnClose;
            Task task = null;
            lock (_waitingCommands)
            {
                if (_connectCompletionSource == null)
                {
                    _connectCompletionSource = new TaskCompletionSource<bool>();
                     task=Task.Run(() =>_client.Connect());
                    _connectTimeStamp = DateTime.Now;
                }
            }
            if (task != null)
                await task;
            if (_timer == null)
            {
                _timer = new Timer(OnTimerTick);
                _timer.Change(TimeSpan.FromSeconds(1), ConnectionTimeout);
            }
            return await _connectCompletionSource.Task.ConfigureAwait(false);
        }

        private void OnTimerTick(object state)
        {
            CheckTimout();
        }

        private void CheckTimout()
        {
            try
            {
                var timeout = ConnectionTimeout;
                //#if DEBUG
                //                if (Debugger.IsAttached)
                //                    timeout = TimeSpan.FromSeconds(60);
                //#endif
                // check commands already sent, waiting for answer
                TaskCompletionSource<bool> task_source = null;
                bool time_out = false;
                lock (_waitingCommands)
                {
                    task_source = _connectCompletionSource;
                    var stamp = _connectTimeStamp;
                    if (task_source != null && DateTime.Now - stamp > timeout)
                    {
                        time_out = true;
                        _connectCompletionSource = null;
                    }
                }
                if (time_out == true && task_source != null)
                    task_source.TrySetResult(false);
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
                            task_source1.TrySetResult(new OsmpResponse()
                            {
                                Command = msg,
                                Status = "TIMEOUT",
                                Result = "The message timed out: " + msg.Id
                            });
                    }
                    else
                        not_timed_out++;
                }
                //if (timed_out > not_timed_out)
                //{
                //    IsConnected = false;
                //    WebSocket.CloseAsync();
                //}
                //lock (_waitingCommands)
                //{
                //    this.Log(Level.DEBUG, s => s.AppendFormat("[{2}]: Messages waiting for connect: {0} / for anser: {1}", CommandQueue.Count, _waitingCommands.Count, PeerAddress));
                //}
            }
            catch (Exception e)
            {
                if (Error != null)
                    Error("Timeoutcheck failed", e);
            }
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            if (ConnectionStateChanged != null)
                ConnectionStateChanged();
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            if (Error != null)
                Error(e.Message, e.Exception);
            if (ConnectionStateChanged != null)
                ConnectionStateChanged();
        }

        /// <summary>
        /// Send command with optional parameters to server and wait for the response
        /// </summary>
        /// <param name="command">The command name</param>
        /// <param name="data">The command parameters </param>
        /// <returns>The server's response</returns>
        public async Task<OsmpResponse> SendCommand(string command, JObject data = null)
        {
            var msg = new OsmpMessage { Type = "cmd", Id = command, Data = data };
            return await Send(msg);
        }

        /// <summary>
        /// Send the given message to the server and wait for the response
        /// </summary>
        /// <param name="msg">The message can be a command, response, event, stream or cancel message</param>
        /// <returns>The server's response</returns>
        public async Task<OsmpResponse> Send(OsmpMessage msg)
        {
            if (!IsConnected)
                return new OsmpResponse() { Result = "Not connected!", Status = "FAIL", Command = msg };
            var task_source = new TaskCompletionSource<OsmpResponse>();
            msg.SendTimestamp = DateTime.Now;
            msg.TaskSource = task_source;
            msg.Nr = Interlocked.Increment(ref _msg_counter);
            var json = JsonConvert.SerializeObject(msg);
            lock (_waitingCommands)
                _waitingCommands[msg.Nr] = msg;
            if (msg.CancellationToken != CancellationToken.None)
                msg.CancellationToken.Register(() => Cancel(msg));
            //_client.SendAsync(json, success =>
            //{
            //    if (!success)
            //        task_source.TrySetResult(new OsmpResponse() { Result = "Send failed", Status = "FAIL", Command = msg });
            //    if (MessageSent != null)
            //        MessageSent(json, success);
            //});
            try
            {
                _client.Send(json);
                MessageSent?.Invoke(json, true);
            }
            catch (Exception e)
            {
                MessageSent?.Invoke(json, false);
                task_source.TrySetResult(new OsmpResponse() { Result = "Send failed", Status = "FAIL", Command = msg });
            }
            return await task_source.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Send the given text to the server. This is fire and forget, there is no way to wait for a response.
        /// Note: this method is used for testing purposes mainly and it is not recommended to use it.
        /// </summary>
        /// <param name="text">the text message to send</param>
        public void SendText(string text) // fire and forget, don't process response
        {
            if (!IsConnected)
                return;
            var cl = _client;
            if (cl == null)
                return;
            //cl.SendAsync(text, success =>
            //{
            //    if (MessageSent != null)
            //        MessageSent(text, success);
            //});
            try
            {
                _client.Send(text);
                MessageSent?.Invoke(text, true);
            }
            catch (Exception e)
            {
                MessageSent?.Invoke(text, false);
            }
        }

        /// <summary>
        /// Connection status
        /// </summary>
        public bool IsConnected
        {
            get
            {
                var cl = _client;
                if (cl == null)
                    return false;
                return cl.IsAlive;
            }
        }

        private void OnReceive(object sender, MessageEventArgs e)
        {
            LastReceived = DateTime.Now;
            if (e.IsPing)
            {
                //Console.WriteLine("Ping");
                return;
            }
            else if (e.IsText)
            {
                Task.Run(() =>
                {
                    if (MessageReceived != null)
                        MessageReceived(e.Data);
                    ReceiveText(e.Data);
                });
                return;
            }
            else if (e.IsBinary)
            {
                // ignore
                //Console.WriteLine();
                if (Error != null)
                    Error("Binary data is not supported: " + Encoding.UTF8.GetString(e.RawData), null);
                return;
            }
        }

        /// <summary>
        /// Note: this is used by unit tests to inject receive messages
        /// </summary>
        /// <param name="text"></param>
        public void SimulateReceiveText(string text) { ReceiveText(text); }

        private void ReceiveText(string text)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(text);
                var type = obj.Value<string>("type");
                switch (type)
                {
                    case "cmd":
                        var server_cmd = obj.ToObject<OsmpMessage>();
                        ReceiveServerCmd(server_cmd);
                        return;
                    case "response":
                        var response = obj.ToObject<OsmpResponse>();
                        ReceiveCmdResponse(response);
                        return;
                    case "event":
                        var msg = obj.ToObject<OsmpMessage>();
                        ReceiveEvent(msg);
                        return;
                    case "stream":
                        var stream = obj.ToObject<OsmpStream>();
                        ReceiveStream(stream);
                        return;
                    default:
                        if (Error != null)
                            Error("Unknown msg type: " + type, null);
                        return;
                }
            }
            catch (Exception e)
            {
                if (Error != null)
                    Error("Error in receive", e);
            }
        }

        private void ReceiveServerCmd(OsmpMessage msg)
        {
            switch (msg.Id)
            {
                case "password-request":
                    HandlePasswordRequest(msg);
                    break;
                case "sign-request":
                    HandleSignRequest(msg);
                    break;
                default:
                    Respond(msg, status: "ERROR", result: "Unknown server command: " + msg.Id);
                    break;
            }
        }

        private void HandlePasswordRequest(OsmpMessage msg)
        {
            var token = msg.Data.Value<string>("token");
            var username = msg.Data.Value<string>("username");
            if (string.IsNullOrWhiteSpace(token))
            {
                Respond(msg, status: "ERROR", result: "Token is null or empty!");
                return;
            }
            string encrypted_pw = null;
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(ServerPublicKey);
                encrypted_pw = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes("" + _pw + token), false));
            }
            Respond(msg, data: new JObject { { "encrypted-password", encrypted_pw } });
        }

        private void HandleSignRequest(OsmpMessage msg)
        {
            var token = msg.Data.Value<string>("token");
            if (string.IsNullOrWhiteSpace(token))
            {
                Respond(msg, status: "ERROR", result: "Token is null or empty!");
                return;
            }
            var signature = Convert.ToBase64String(_rsa.SignData(Encoding.UTF8.GetBytes(token), CryptoConfig.MapNameToOID("SHA1")));
            Respond(msg, data: new JObject { { "signature", signature } });
        }

        private void Respond(OsmpMessage server_cmd, JObject data = null, string status = "OK", string result = null)
        {
            var msg = new JObject { { "type", "response" }, { "id", server_cmd.Id }, { "nr", Interlocked.Increment(ref _msg_counter) }, { "cmd-nr", server_cmd.Nr }, { "status", status } };
            if (!string.IsNullOrWhiteSpace(result))
                msg["result"] = result;
            if (data != null && data.Count > 0)
                msg["data"] = data;
            SendText(JsonConvert.SerializeObject(msg));
        }

        private void ReceiveCmdResponse(OsmpResponse response)
        {
            LastReceived = DateTime.Now;
            OsmpMessage cmd = null;
            lock (_waitingCommands)
            {
                if (_waitingCommands.TryGetValue(response.CmdNr, out cmd))
                    _waitingCommands.Remove(response.CmdNr);
            }
            response.Command = cmd;
            if (cmd != null)
                cmd.TaskSource.TrySetResult(response);
            if (CmdResponseReceived != null)
                CmdResponseReceived(response);
        }

        private void ReceiveStream(OsmpStream stream)
        {
            LastReceived = DateTime.Now;
            OsmpMessage cmd = null;
            lock (_waitingCommands)
            {
                if (_waitingCommands.TryGetValue(stream.CmdNr, out cmd))
                    stream.Command = cmd;
            }
            if (CmdStreamReceived != null)
                CmdStreamReceived(stream);
        }

        private void ReceiveEvent(OsmpMessage msg)
        {
            if (string.IsNullOrEmpty(msg.Id))
            {
                ReceivedMultipleEvents(msg);
                return;
            }
            switch (msg.Id)
            {
                case "session-initiated":
                    var server_pkey = msg.Data.Value<string>("public-key");
                    if (ServerPublicKey == null)
                        ServerPublicKey = server_pkey;
                    else if (ServerPublicKey != server_pkey)
                    {
                        if (Error != null)
                            Error("Warning: server's actual public key differs from configured key!", null);
                    }
                    NotifyConnectionEstablished();
                    break;
                case "session-status":
                    GotSessionStatus(msg);
                    return;
            }
            if (EventsReceived != null)
                EventsReceived(new[] { new OsmpEvent { Id = msg.Id, Data = msg.Data } });
        }

        private void GotSessionStatus(OsmpMessage msg)
        {
            var now = DateTime.Now;
            lock (_waitingCommands)
            {
                foreach (var cmd_nr in msg.Data["active-cmds"].Values<int>())
                {
                    if (!_waitingCommands.ContainsKey(cmd_nr))
                        continue;
                    var cmd = _waitingCommands[cmd_nr];
                    cmd.StreamReceivedTimestamp = now;
                    cmd.ReceiveTimestamp = now;
                }
            }
        }

        private void ReceivedMultipleEvents(OsmpMessage msg)
        {
            if (EventsReceived == null)
                return;
            var events = msg.Data["events"].Values<JObject>()
                    .Select(o => new OsmpEvent { Id = o.Value<string>("event"), Data = o.Value<JObject>("data") }).ToArray();
            EventsReceived(events);
        }

        private void NotifyConnectionEstablished()
        {
            if (ConnectionStateChanged != null)
                ConnectionStateChanged();
            var task_source = _connectCompletionSource;
            _connectCompletionSource = null;
            if (task_source != null)
                task_source.TrySetResult(true); // connect succeded!
        }

        /// <summary>
        /// Cancel a long running command
        /// </summary>
        /// <param name="msg">The originally sent command to be cancelled</param>
        public void Cancel(OsmpMessage msg)
        {
            Cancel(new[] { msg.Nr });
        }

        /// <summary>
        /// Cancel the commands with given cmnd-nrs
        /// </summary>
        /// <param name="cmds">Array of command numbers</param>
        public void Cancel(int[] cmds)
        {
            var msg = new JObject
            {
                { "type", "cancel" },
                { "nr", Interlocked.Increment(ref _msg_counter) },
                { "data", new JObject { {"cmds", new JArray(cmds)} } },
            };
            SendText(JsonConvert.SerializeObject(msg));
        }

        /// <summary>
        /// Cancel all active commands
        /// </summary>
        public void CancelAll()
        {
            var msg = new JObject
            {
                { "type", "cancel" },
                { "nr", Interlocked.Increment(ref _msg_counter) },
                { "data", new JObject { {"cmds", "*"} } },
            };
            SendText(JsonConvert.SerializeObject(msg));
        }

        #region --> RSA Crypto & Authentication

        /// <summary>
        /// The client's public key. To change it use LoadKeypair
        /// </summary>
        public string PublicKey { get { return _rsa.ToXmlString(includePrivateParameters: false); } }

        private RSACryptoServiceProvider _rsa = new RSACryptoServiceProvider();

        /// <summary>
        /// Load the given private and public key pair for authentication
        /// </summary>
        /// <param name="key_pair_xml">The public/private key pair</param>
        public void LoadKeypair(string key_pair_xml)
        {
            _rsa.FromXmlString(key_pair_xml);
        }

        private string _pw = null; // temporary cache of the plaintext password for Login()

        /// <summary>
        /// Login with username and plaintext password
        /// </summary>
        public async Task<OsmpResponse> Login(string username, string plaintext_password)
        {
            if (string.IsNullOrWhiteSpace(ServerPublicKey))
                throw new InvalidOperationException("OsmpClient.ServerPublicKey must be configured!");
            _pw = plaintext_password;
            var response = await Send(new OsmpMessage()
            {
                Id = "login",
                Type = "cmd",
                Data = new JObject { { "username", username }, { "method", "password" } }
            });
            _pw = null;
            return response;
        }

        /// <summary>
        /// Login with username and public key
        /// </summary>
        public async Task<OsmpResponse> LoginWithPublicKey(string username)
        {
            var response = await Send(new OsmpMessage()
            {
                Id = "login",
                Type = "cmd",
                Data = new JObject { { "username", username }, { "method", "public-key" }, { "public-key", PublicKey } }
            });
            return response;
        }

        /// <summary>
        /// Logout the currently authorized user
        /// </summary>
        public async Task<OsmpResponse> Logout()
        {
            return await Send(new OsmpMessage() { Id = "logout", Type = "cmd", });
        }

        #endregion

        /// <summary>
        /// Close the connection to the server
        /// </summary>
        public void Disconnect()
        {
            var cl = _client;
            if (cl == null)
                return;
            cl.Close(CloseStatusCode.Normal);
        }

        public void Dispose()
        {
            if (_timer != null)
                _timer.Dispose();
            var cl = _client;
            if (cl == null)
                return;
            cl.Close(CloseStatusCode.Normal);
            _client = null;
        }

    }

    [JsonObject(MemberSerialization.OptIn)]
    public class OsmpMessage
    {
        public OsmpMessage()
        {
            CreateTimestamp = DateTime.Now;
            CancellationToken = CancellationToken.None;
        }

        /// <summary>
        /// Message type ( cmd | response | stream | event |  cancel )
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Incremental message number. Set by client on send automatically
        /// </summary>
        [JsonProperty("nr")]
        public int Nr { get; set; }

        /// <summary>
        /// Command/Event name
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } // cmd id

        /// <summary>
        /// The message payload
        /// </summary>
        [JsonProperty("data")]
        public JObject Data { get; set; }

        public DateTime CreateTimestamp { get; set; }
        public DateTime SendTimestamp { get; set; }
        public DateTime ReceiveTimestamp { get; set; }
        public DateTime StreamReceivedTimestamp { get; set; }

        /// <summary>
        /// TaskSource can be used to wait for the response. Await the response like this: 
        /// var response = await TaskSource.Task;
        /// </summary>
        public TaskCompletionSource<OsmpResponse> TaskSource { get; set; }

        /// <summary>
        /// CancellationToken is used to react to cancellation of the command. 
        /// </summary>
        public CancellationToken CancellationToken { get; set; }


        /// <summary>
        /// Serialize the given object into the message data.
        /// This is the opposite of DataAs
        /// </summary>
        /// <param name="data"></param>
        public void SetData(object data)
        {
            Data = JObject.FromObject(data);
        }

        /// <summary>
        /// Deserialize the message data into a new object of given type T
        /// This is the opposite of SetData
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T DataAs<T>()
        {
            return Data.ToObject<T>();
        }
    }

    /// <summary>
    /// OsmpResponse represents a response that is sent to the client from the server as the result of a command. 
    /// The original command is referenced. The result payload is stored in the inherited Data property.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class OsmpResponse : OsmpMessage
    {
        /// <summary>
        /// The message number of the command that caused this response
        /// </summary>
        [JsonProperty("cmd-nr")]
        public int CmdNr { get; set; }

        /// <summary>
        /// The response status ( OK | ERROR | TIMEOUT )
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Result message of this response or error message if not OK
        /// </summary>
        [JsonProperty("result")]
        public string Result { get; set; }

        /// <summary>
        /// The original command message that led to this response
        /// </summary>
        public OsmpMessage Command { get; set; }

        public bool IsOk { get { return Status == "OK"; } }

    }

    /// <summary>
    /// OsmpStream is a stream package that is usually sent from the server to the client. 
    /// It references the command that started the stream. The payload is stored in the inherited Data field.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class OsmpStream : OsmpMessage
    {
        /// <summary>
        /// The message number of the command that caused this response
        /// </summary>
        [JsonProperty("cmd-nr")]
        public int CmdNr { get; set; }

        // the original command message that caused this stream
        public OsmpMessage Command { get; set; }
    }

    /// <summary>
    /// Represents an event that is sent from the server to the client
    /// </summary>
    public class OsmpEvent
    {
        /// <summary>
        /// The event name
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The event payload
        /// </summary>
        public JObject Data { get; set; }
    }
}
