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


namespace Osmp.StandardCmds
{
    [OsmpCommand]
    public class ActiveCmdsCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }
        public override string Name { get { return "active-cmds"; } }
        public override string Description { get { return "Returns list of active commands (excluding own)."; } }

        public override IEnumerable<CommandParam> MandatoryParamDefs { get { yield break; }}

        public override IEnumerable<CommandParam> ReturnValueDefs
        {
            get
            {
                yield return new CommandParam() { Name = "now", Description = "Current server time." };
                yield return new CommandParam() { Name = "cmds", Description = "Array of commands." };
            }
        }

        protected override async Task<CommandResult> ExecuteImplementation()
        {
            var result = new CommandResult();
            result.Data["now"] = DateTime.Now;
            result.Data["cmds"] = new JArray( Session.GetActiveCmds().Where(IsNotThis).Select(ToJObject));
            return result;
        }

        private object ToJObject(AbstractCommand cmd)
        {
            return new JObject() {{"name", cmd.Name}, {"cmd-nr", cmd.CmdNr}, {"start-time", cmd.StartTime}};
        }

        private bool IsNotThis(AbstractCommand cmd)
        {
            return cmd.CmdNr != this.CmdNr;
        }
    }
}
