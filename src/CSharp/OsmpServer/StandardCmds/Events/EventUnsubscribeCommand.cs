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

namespace Osmp.StandardCmds.Events
{
    
    [OsmpCommand]
    public class EventUnsubscribeCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }
        public override string Name { get { return "event-unsubscribe"; } }

        public override IEnumerable<string> Aliases
        {
            get
            {
                yield return "evu";
            }
        }

        public override string Description { get { return "Unsubscribe a given event or all events"; } }

        public override IEnumerable<CommandParam> MandatoryParamDefs
        {
            get { yield return new CommandParam() { Name = "event", Description = "The event to unsubscribe (* for all events)" }; }
        }

        public override IEnumerable<CommandParam> OptionalParamDefs
        {
            get { yield break; }
        }

        public override IEnumerable<CommandParam> ReturnValueDefs
        {
            get { yield break; }
        }

        protected override async Task<CommandResult> ExecuteImplementation()
        {
            var @params = Params;
            var ev = @params.Get<string>("event");
            if (string.IsNullOrWhiteSpace(ev))
                return CommandResult.Fail("No event specified.");            
            if (ev == "*")
            {
                foreach (var event_info in Session.Events.Values.OrderBy(x => x.Name).ToArray())
                {
                    Session.UnsubscribeEvent(event_info);
                }
                return CommandResult.Success("Unsubscribed " + Session.Events.Count + " events.");
            }
            else
            {
                if (!Session.Events.ContainsKey(ev))
                    return CommandResult.Fail("Event not found: "+ev);
                var event_info = Session.Events[ev];
                Session.UnsubscribeEvent(event_info);
                return CommandResult.Success("Unsubscribed event "+ev);
            }
            return CommandResult.Fail("Unexpected error.");
        }
    }
}
