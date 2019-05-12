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
    public class WaitCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }
        public override string Name { get { return "wait"; } }
        public override string Description { get { return "Wait indefinitely (until cancelled) or the given amount of seconds"; } }

        public override IEnumerable<CommandParam> MandatoryParamDefs { get { yield break; } }

        public override IEnumerable<CommandParam> OptionalParamDefs
        {
            get { yield return new CommandParam() { Name = "seconds", Description = "Seconds to wait (double)." }; }
        }

        public override IEnumerable<CommandParam> ReturnValueDefs
        {
            get { yield break; }
        }

        protected override async Task<CommandResult> ExecuteImplementation()
        {
            //Session.Stream(new JObject("Waiting\n"));
            double seconds = double.MaxValue;
            if (Params.ContainsKey("seconds"))
            {
                seconds = Params.Get<double>("seconds");
                if (seconds < 0)
                    return CommandResult.Fail("Parameter 'seconds' must not be negative.");
                if (seconds <= 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(seconds)).ConfigureAwait(false);
                    return CommandResult.Success("Done.");
                }
            }
            else
                StreamProgressIndeterminate("Waiting indefinitely");
            int i = 0;
            //int j = 0;
            while (CancellationToken.IsCancellationRequested==false && i < seconds)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken).ConfigureAwait(false);
                }
                catch (Exception) { /*ignore*/ }
                if (CancellationToken.IsCancellationRequested)
                    return CommandResult.Success("Cancelled.");
                //if (j > 9)
                //{
                //    j = 0;
                //    //Session.Stream("\n");
                //}
                //j++;
                i++;
                Stream(new JObject() { {"value",i} });
                if (Params.ContainsKey("seconds"))
                    StreamProgress("Waiting ...", i, (int)seconds);
            }
            StreamProgressComplete();
            return CommandResult.Success("Done.");
        }
    }
}
