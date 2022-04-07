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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace eqqon.Osmp.StandardCmds
{
    [OsmpCommand]
    public class DownloadCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }

        public override string Name { get { return "download"; } }

        public override IEnumerable<string> Aliases { get { yield return "dl"; } }

        public override string Description { get { return "Download a file"; } }

        public override IEnumerable<CommandParam> MandatoryParamDefs
        {
            get
            {
                yield return new CommandParam() { Name = "filepath", Description = "Path to the file that is to be transferred." };
            }
        }

        //public override IEnumerable<CommandParam> ReturnValueDefs
        //{
        //    get { return base.ReturnValueDefs; }
        //}

        public override IEnumerable<string> Examples
        {
            get
            {
                yield return "download errorlog.txt";
                yield return @"dl "".\filename contains spaces.txt""";
                yield return @"dl {""filename"":"".\\path\\contains\\backslashes.txt""}";
            }
        }

        protected override async Task<CommandResult> ExecuteImplementation()
        {
            var server = Session;
            var filepath = Params.Get<string>("filepath");
            // note: if it is necessary to restrict the allowed filename / path this should be done here. In order not to have to copy files locally just to 
            // serve them to downloaders we have to allow all local paths, even such that are outside the app's home dir
            return await server.SendFile(filepath, CancellationToken);

            // ------------------------------------------------------------------------------------------
        }
    }

}
