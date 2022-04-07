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
    public class UploadCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }

        public override string Name { get { return "upload"; } }

        public override IEnumerable<string> Aliases { get { yield return "ul"; } }

        public override string Description { get { return "Upload a file"; } }

        public override IEnumerable<CommandParam> MandatoryParamDefs
        {
            get { yield break; }
        }

        //public override IEnumerable<CommandParam> OptionalParamDefs
        //{
        //    get
        //    {
        //        yield return new CommandParam() { Name = "filepath", Description = "Destination file path of the file that is to be transferred." };
        //    }
        //}

        //public override IEnumerable<CommandParam> ReturnValueDefs
        //{
        //    get { return base.ReturnValueDefs; }
        //}

        protected override async Task<CommandResult> ExecuteImplementation()
        {
            var files= await UploadFiles();
            if (files==null || files.Length==0)
                return CommandResult.Fail("Error: upload failed.");
            // ------------------------------------------------------------------------------------------
            return CommandResult.Success("Upload complete: "+files.Length + " file(s)");
        }

    }

}
