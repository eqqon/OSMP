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

namespace eqqon.Osmp.StandardCmds.Events
{
    
    [OsmpCommand]
    public class EventListCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }
        public override string Name { get { return "event-list"; } }

        public override IEnumerable<string> Aliases
        {
            get
            {
                yield return "evl";
                yield return "events";
            }
        }

        public override string Description { get { return "List all events"; } }

        public override IEnumerable<CommandParam> MandatoryParamDefs
        {
            get { yield break; }
        }

        //public override IEnumerable<CommandParam> OptionalParamDefs
        //{
        //    get
        //    {
        //        yield return new CommandParam() { Name = "filter", Description = "Filter the list by name." };
        //    }
        //}

        public override IEnumerable<CommandParam> ReturnValueDefs
        {
            get { yield return new CommandParam() { Name = "events", Description = "List of all events which can be subscribed" }; }
        }

        protected override async Task<CommandResult> ExecuteImplementation()
        {
            var events = new JArray();
            var abos = Session.EventAbos;
            foreach (var event_info in Session.Events.Values.OrderBy(x=>x.Name))
            {
                var ev = new JObject
                {
                    {"event-name", event_info.Name},
                    {"instruction-set", event_info.InstructionSet},
                    {"description", event_info.Description},
                };
                EventAbo abo=null;
                if (abos.TryGetValue(event_info.Name, out abo) && abo.ExpireTime > DateTime.Now)
                {
                    ev["subscribed-since"] = abo.RegistrationTime;
                    ev["subscribed-until"] = abo.ExpireTime;
                }
                events.Add(ev);
            }
            return CommandResult.Success(data:new JObject{{"now", DateTime.Now}, {"events", events}, });
        }
    }
}
