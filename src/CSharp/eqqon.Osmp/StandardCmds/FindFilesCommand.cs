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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace eqqon.Osmp.StandardCmds
{
    [OsmpCommand]
    public class FindFilesCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }
        public override string Name { get { return "find-files"; } }
        public override IEnumerable<string> Aliases { get { yield return "find"; } }
        public override string Description { get { return "Find files and directories matching a pattern."; } }

        public override IEnumerable<CommandParam> MandatoryParamDefs { get { yield break; } }

        public override IEnumerable<CommandParam> OptionalParamDefs
        {
            get { yield return new CommandParam() { Name = "filter", Description = "Filter expression (wildcards * and ?, i.e. *.txt). List all if omitted." }; }
        }

        public override IEnumerable<CommandParam> ReturnValueDefs
        {
            get { yield return new CommandParam() { Name = "paths", Description = "List of all directories and files." }; }
        }

        protected override async Task<CommandResult> ExecuteImplementation()
        {
            int i = 0;
            foreach (var path in Directory.EnumerateFileSystemEntries(".", Params.Get<string>("filter") ?? "*", SearchOption.AllDirectories))
            {
                if (CancellationToken.IsCancellationRequested)
                    return CommandResult.Fail("Cancelled.");
                Stream(new JObject { {"path", path}});
                i++;
            }
            return CommandResult.Success();
        }
    }
}
