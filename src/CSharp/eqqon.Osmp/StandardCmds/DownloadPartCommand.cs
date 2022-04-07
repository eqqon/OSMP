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
using Newtonsoft.Json.Linq;


namespace eqqon.Osmp.StandardCmds
{
    [OsmpCommand]
    public class DownloadPartCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }

        public override string Name { get { return "download-part"; } }

        public override string Description { get { return "Download a file (in parts)"; } }

        public override IEnumerable<CommandParam> MandatoryParamDefs
        {
            get
            {
                yield return new CommandParam() { Name = "filename", Description = "Name of the file that is transferred." };
                yield return new CommandParam() { Name = "part_nr", Description = "Number of this part." };
            }
        }

        //public override IEnumerable<CommandParam> ReturnValueDefs
        //{
        //    get { return base.ReturnValueDefs; }
        //}

        protected override async Task<CommandResult> ExecuteImplementation()
        {
            var server = Session;
            var filename = Params.Get<string>("filename");
            // note: if it is necessary to restrict the allowed filename / path this should be done here. In order not to have to copy files locally just to 
            // serve them to downloaders we have to allow all local paths, even such that are outside the app's home dir
            if (string.IsNullOrWhiteSpace(filename)) 
                return CommandResult.Fail("ERROR: invalide filename: "+filename);
            var part_nr = Params.Get<int>("part_nr");
            var sdata = server.SessionData;
            if (part_nr == 0) // start a new download
            {
                
                if (!File.Exists(filename))
                    return CommandResult.Fail("File not found: " + filename);
                var f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var length = f.Length;
                var total_parts=(int)Math.Ceiling(length/(double) DownloadBuffer.BufferSize);
                sdata[filename] = new DownloadBuffer() {Filename = filename, PartCount = 0, TotalParts = total_parts, Stream = f};
            }
            var buffer = sdata.Get<DownloadBuffer>(filename);
            if (buffer==null)
                return CommandResult.Fail("File buffer not found, please restart transfer.");
            if (buffer.PartCount != part_nr)
                return CommandResult.Fail("Discontinuous part number, please restart transfer.");
            if (buffer.TotalParts <= part_nr)
                return CommandResult.Fail("Invalid part number, please restart transfer.");
            var data = new JObject { {"total_parts", buffer.TotalParts}};
            try
            {
                var bytes=new byte[DownloadBuffer.BufferSize];
                var read = buffer.Stream.Read(bytes, 0, bytes.Length);
                data["content"] = Convert.ToBase64String(bytes, 0, read);
                buffer.PartCount += 1;
                if (buffer.PartCount == buffer.TotalParts)
                {
                    buffer.Stream.Close();
                    sdata[filename] = null;
                }
            }
            catch (Exception e)
            {
                return CommandResult.Fail("ERROR: writing file: " + e.PrettyPrint());
            }
            // ------------------------------------------------------------------------------------------
            return CommandResult.Success("OK", data);
        }
    }

    public class DownloadBuffer : IDisposable
    {
        public string Id { get; set; }
        public const int BufferSize = (64 * 1024);
        public string Filename { get; set; }
        public int TotalParts { get; set; }
        public int PartCount { get; set; }
        public bool IsComplete { get { return PartCount == TotalParts; } }

        public Stream Stream;

        public bool IsDisposed;
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            if (Stream == null)
                return;
            try
            {
                Stream.Dispose();
            }
            catch (Exception) { /* ignore */ }
        }
    }

}
