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
using System.Threading.Tasks;


namespace eqqon.Osmp.StandardCmds
{
    [OsmpCommand]
    public class UploadPartCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }
        public override string Name { get { return "upload-part"; } }

        public override string Description { get { return "Upload a file (in parts)"; } }

        public override IEnumerable<CommandParam> MandatoryParamDefs
        {
            get
            {
                yield return new CommandParam() { Name = "filename", Description = "Name of the file that is transferred." };
                yield return new CommandParam() { Name = "total_parts", Description = "Number of total parts of the file that is being transferred." };
                yield return new CommandParam() { Name = "part_nr", Description = "Number of this part." };
                yield return new CommandParam() { Name = "content", Description = "File (or part) in base64 encoding." };
            }
        }

        protected override async Task<CommandResult> ExecuteImplementation()
        {
            var transfer_dir =  Session.GlobalEnv.Get<string>("transfer_dir") ?? "transfers";
            var filename = Params.Get<string>("filename");
            if (string.IsNullOrWhiteSpace(filename) || filename.Contains(".."))
                return CommandResult.Fail("ERROR: invalide filename: "+filename);
            var content = Params.Get<string>("content");
            if (string.IsNullOrWhiteSpace(content))
                return CommandResult.Fail("ERROR: content is empty");
            var part_number = Params.Get<int>("part_nr");
            var total_parts = Params.Get<int>("total_parts");
            if (total_parts<=0 || total_parts < part_number)
                return CommandResult.Fail("Incorrect total part number: "+total_parts);
            var sdata = Session.SessionData;
            if (part_number == 0)
            {
                // start a new upload
                if (!Directory.Exists(transfer_dir))
                    Directory.CreateDirectory(transfer_dir);
                var f = new FileStream(Path.Combine(transfer_dir, filename), FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                sdata[filename] = new UploadBuffer() {Filename = filename, PartCount = 0, TotalParts = total_parts, Stream = f};
            }
            var buffer = sdata.Get<UploadBuffer>(filename);
            if (buffer==null)
                return CommandResult.Fail("File buffer not found, please restart transfer.");
            if (buffer.PartCount!=part_number)
                return CommandResult.Fail("Discontinuous part number, please restart transfer.");
            try
            {
                var bytes = Convert.FromBase64String(content);
                buffer.Stream.Write(bytes, 0, bytes.Length);
                buffer.PartCount += 1;
                if (buffer.PartCount == buffer.TotalParts)
                {
                    buffer.Stream.Flush();
                    buffer.Stream.Close();
                    sdata[filename] = null;
                }
            }
            catch (Exception e)
            {
                return CommandResult.Fail("ERROR: writing file: " + e.PrettyPrint());
            }
            // ------------------------------------------------------------------------------------------
            return CommandResult.Success("OK");
        }
    }

    public class UploadBuffer
    {
        public string Filename { get; set; }
        public int TotalParts { get; set; }
        public int PartCount { get; set; }
        public bool IsComplete { get { return PartCount == TotalParts; } }
        //public StreamWriter StreamWriter { get { return new StreamWriter(Stream);} }
        public Stream Stream;
    }
}
