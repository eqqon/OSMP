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
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace eqqon.Osmp.StandardCmds
{
    [OsmpCommand]
    public class LoginCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }
        public override string Name { get { return "login"; } }
        public override string Description { get { return "Request authentication with either username:password or username:public-key"; } }

        public override IEnumerable<CommandParam> MandatoryParamDefs
        {
            get
            {
                yield return new CommandParam() { Name = "username", Description = "User account name" };
                yield return new CommandParam() { Name = "method", Description = "Authentication method (password|public-key)" };
            }
        }

        public override IEnumerable<CommandParam> OptionalParamDefs
        {
            get
            {
                yield return new CommandParam() { Name = "public-key", Description = "RSA public key" };
            }
        }

        protected override async Task<CommandResult> ExecuteImplementation()
        {
            var username = Params.Get<string>("username");
            var method = Params.Get<string>("method");
            switch (method)
            {
                case "password":
                {
                    // lets ask for the password
                    var token = Guid.NewGuid().ToString();
                    var response = await Session.SendCommand("password-request", new JObject {{"token", token} });
                    if (!response.IsOk)
                        return CommandResult.Fail("password-request failed: " + response.Result);
                    var encrypted_pw = response.Data.Get<string>("encrypted-password");
                    if (Session.Login(username, token, encrypted_pw))
                        return CommandResult.Success("Login successful!");
                    break;
                }
                case "public-key":
                {
                    var pkey = Params.Get<string>("public-key");
                    if (string.IsNullOrWhiteSpace(pkey))
                        return CommandResult.Fail("You must supply your public key");
                    // now we need proof that the client also owns the private key
                    var token = Guid.NewGuid().ToString();
                    var response = await Session.SendCommand("sign-request", new JObject {{"token", token}});
                    if (!response.IsOk)
                        return CommandResult.Fail("sign-request failed: " + response.Result);
                    var signature = response.Data.Get<string>("signature");
                    if (Session.LoginWithPublicKey(username, pkey, token, signature))
                        return CommandResult.Success("Login successful!");
                    break;
                }
                default:
                    return CommandResult.Fail("Unknown authentication method: " + method);
            }
            return CommandResult.Fail("Login failed!");
        }

    }
}
