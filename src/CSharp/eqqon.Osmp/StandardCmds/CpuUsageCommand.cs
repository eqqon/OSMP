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
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;


namespace eqqon.Osmp.StandardCmds
{
    // not possible on .net standard
    /*
    [OsmpCommand]
    public class CpuUsageCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }
        public override string Name { get { return "cpu-usage"; } }

        public override IEnumerable<string> Aliases { get { yield return "cpu"; } }

        public override string Description { get { return "Return the overall system cpu usage and for the controller."; } }

        public override IEnumerable<CommandParam> MandatoryParamDefs
        {
            get { yield break; }
        }

        //public override IEnumerable<CommandParam> OptionalParamDefs
        //{
        //    get
        //    {
        //        yield return new CommandParam() { Name = "filter", Description = "Filter the list by given filter expression" };
        //    }
        //}

        protected override async Task<CommandResult> ExecuteImplementation()
        {
            var server = Session;
            try
            {
                StreamLine("Printing CPU usage (Hit Enter to cancel)");
                var cpuCounter = new PerformanceCounter
                {
                    CategoryName = "Processor",
                    CounterName = "% Processor Time",
                    InstanceName = "_Total"
                };
                cpuCounter.NextValue();
                await Task.Delay(1000);
                while (!CancellationToken.IsCancellationRequested)
                {
                    var value = (int)Math.Round(cpuCounter.NextValue(),0);
                    Stream( new JObject { {"value", value}});
                    await Task.Delay(1000);
                }
                // the linux way!
                //server.StreamLine("Inton Controller CPU Usage:");
                //await BashCmd("ps x -o pcpu,comm | grep IntonLoader.exe", server);
                //server.StreamLine("Overall System CPU Usage:");
                //await BashCmd("ps x -o pcpu | tail -n+2| awk '{s+=$1} END {print s}'", server);
            }
            catch (Exception e)
            {
                return CommandResult.Fail("Restart failed: " + e.PrettyPrint());
            }
            return CommandResult.Success("Cancelled.");
        }
    }
    */
}
