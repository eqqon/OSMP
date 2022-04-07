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
using Newtonsoft.Json.Linq;
using NUnit.Framework;


namespace eqqon.Osmp.Tests
{
    [TestFixture]
    public class OsmpServerTests
    {
        private OsmpServer _server;
        private OsmpClient _client;

        [Test]
        public async Task ConnectDisconnect()
        {
            using (var server = new OsmpServer() { Port = 44332, UseSsl = false })
            using (var client = new OsmpClient() { Address = "ws://localhost:44332/osmp/v1" })
            {
                server.Startup();
                await client.Connect();
                Assert.AreEqual(true, client.IsConnected);
                Assert.AreEqual(1, server.GetSessionCount());
                client.Disconnect();
                await Task.Delay(TimeSpan.FromSeconds(1));
                Assert.AreEqual(0, server.GetSessionCount());
                // client notices server side disconnection
                await client.Connect();
                Assert.AreEqual(true, client.IsConnected);
                server.Shutdown();
                Assert.AreEqual(false, client.IsConnected);
            }
        }

        [Test]
        public async Task CommandWithImmediateResponse()
        {
            using (var server = new OsmpServer() { Port = 44332, UseSsl = false })
            using (var client = new OsmpClient() { Address = "ws://localhost:44332/osmp/v1" })
            {
                server.Startup();
                await client.Connect();

                // Send
                var response=await client.Send(new OsmpMessage() { Id = "echo", Type = "cmd", Data = new JObject {{"token", "Hello World!"}}});
                Assert.NotNull(response);
                Assert.AreEqual("OK", response.Status);
                Assert.AreEqual(true, response.IsOk);
                Assert.AreEqual("Hello World!", response.Data.Value<string>("token"));

                // SendCommand
                response = await client.SendCommand("echo", new JObject { { "token", "Winter is coming." } } );client.Error += (s, e) => Assert.Fail(s + "\n" + e.PrettyPrint());
                Assert.AreEqual("Winter is coming.", response.Data.Value<string>("token"));

                // Send with Standard.Echo
                response = await client.Send(Standard.Echo("What was was and never will be - Plato" ));
                Assert.AreEqual("What was was and never will be - Plato", response.DataAs<Standard.EchoResult>().Token);

            }
        }

        [Test]
        public async Task CancelIndefiniteCommand()
        {
            using (var server = new OsmpServer() { Port = 44332, UseSsl = false })
            using (var client = new OsmpClient() { Address = "ws://localhost:44332/osmp/v1" })
            {
                server.Startup();
                client.Error += (s, e) => Assert.Fail(s + "\n" + e.PrettyPrint());
                await client.Connect();
                // Standard.Wait
                var cancellation_source = new CancellationTokenSource();
                var task = client.Send(Standard.Wait(cancellation_source.Token));
                // this task will never complete unless we cancel the command
                Assert.AreEqual(false, task.IsCompleted);
                // ... so cancel it
                cancellation_source.Cancel();
                // wait for response
                var response = await task;
                Assert.AreEqual(true, response.IsOk);
            }
        }

        [Test]
        public async Task CancelAllActiveCommands()
        {
            using (var server = new OsmpServer() { Port = 44332, UseSsl = false })
            using (var client = new OsmpClient() { Address = "ws://localhost:44332/osmp/v1" })
            {
                server.Startup();
                client.Error += (s, e) => Assert.Fail(s + "\n" + e.PrettyPrint());
                await client.Connect();
                // start three Standard.Wait commands 
                client.Send(Standard.Wait());
                client.Send(Standard.Wait());
                client.Send(Standard.Wait());
                await client.Send(Standard.Echo()); // echo immedidatly responds, so doesn't become an active command.
                // now get the active commands (we don't need them, we just want to know)
                var response = await client.Send(Standard.ActiveCmds());
                var active_cmds=response.DataAs<Standard.ActiveCmdsResult>();
                Assert.AreEqual(3, active_cmds.Commands.Count());
                Assert.AreEqual("wait", active_cmds.Commands.First().Name);
                client.CancelAll();
                // now get the active commands again to check
                response = await client.Send(Standard.ActiveCmds());
                active_cmds = response.DataAs<Standard.ActiveCmdsResult>();
                Assert.AreEqual(0, active_cmds.Commands.Count());
            }
        }

        [Test]
        public async Task PasswordAuthentication()
        {
            using (var server = new OsmpServer() { Port = 44332, UseSsl = false, IsAuthenticationRequired = true, UserCredentials = new Dictionary<string, string>{{"ned", "stark"}}})
            using (var client = new OsmpClient() { Address = "ws://localhost:44332/osmp/v1" })
            {
                server.Startup();
                client.Error += (s, e) => Assert.Fail(s + "\n" + e.PrettyPrint());
                client.MessageSent += (s, b) => Console.WriteLine(s);
                client.MessageReceived += (s) => Console.WriteLine(s);
                await client.Connect();
                // commands should fail due to lack of authentication
                var response = await client.Send(Standard.Echo());
                Assert.AreEqual("ERROR", response.Status);
                Assert.AreEqual("Authentication required!", response.Result);
                // try login
                response = await client.Login("jaimie", "lannister");
                Assert.AreEqual("ERROR", response.Status);
                Assert.AreEqual("Login failed!", response.Result);
                response = await client.Login("ned", "strong");
                Assert.AreEqual("ERROR", response.Status);
                Assert.AreEqual("Login failed!", response.Result);
                response = await client.Login("ned", "stark");
                Assert.AreEqual("OK", response.Status);
                Assert.AreEqual("Login successful!", response.Result);
                // now echo must work
                response = await client.Send(Standard.Echo());
                Assert.AreEqual("OK", response.Status);
                // logout
                response = await client.Logout();
                Assert.AreEqual("OK", response.Status);
                // now echo shouldna work again
                response = await client.Send(Standard.Echo());
                Assert.AreEqual("ERROR", response.Status);
                Assert.AreEqual("Authentication required!", response.Result);
            }
        }


        [Test]
        public async Task PublicKeyAuthentication()
        {
            using (var server = new OsmpServer() { Port = 44332, UseSsl = false, IsAuthenticationRequired = true, })
            using (var client = new OsmpClient() { Address = "ws://localhost:44332/osmp/v1" })
            {
                Console.WriteLine(server.PublicKey);
                Console.WriteLine(client.PublicKey);
                server.UserCredentials["alice"] = client.PublicKey;
                server.Startup();
                client.Error += (s, e) => Assert.Fail(s + "\n" + e.PrettyPrint());
                client.MessageSent += (s, b) => Console.WriteLine(s);
                client.MessageReceived += (s) => Console.WriteLine(s);
                await client.Connect();
                // commands should fail due to lack of authentication
                var response = await client.Send(Standard.Echo());
                Assert.AreEqual("ERROR", response.Status);
                Assert.AreEqual("Authentication required!", response.Result);
                // try login
                response = await client.LoginWithPublicKey("jaimie");
                //Console.WriteLine(response.Result);
                Assert.AreEqual("ERROR", response.Status);
                Assert.AreEqual("Login failed!", response.Result);

                response = await client.LoginWithPublicKey("alice");
                Assert.AreEqual("OK", response.Status);
                Assert.AreEqual("Login successful!", response.Result);
                // now echo must work
                response = await client.Send(Standard.Echo());
                Assert.AreEqual("OK", response.Status);
                // logout
                response = await client.Logout();
                Assert.AreEqual("OK", response.Status);
                // now echo shouldna work again
                response = await client.Send(Standard.Echo());
                Assert.AreEqual("ERROR", response.Status);
                Assert.AreEqual("Authentication required!", response.Result);
            }
        }
    }
}
