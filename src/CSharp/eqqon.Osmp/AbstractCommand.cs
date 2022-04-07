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
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace eqqon.Osmp
{
    public abstract class AbstractCommand : IDisposable
    {
        public virtual int Version { get { return 0; } } // <-- this is used to overwrite commands in different modules. the higher version number wins
        public abstract string InstructionSet { get; }
        public abstract string Name { get; }
        public virtual IEnumerable<string> Aliases { get { return new string[0]; } }
        public abstract string Description { get; }
        public virtual string LongDescription { get { return null; } }
        public virtual IEnumerable<string> Examples { get { yield break; } }
        public abstract IEnumerable<CommandParam> MandatoryParamDefs { get; }
        public virtual IEnumerable<CommandParam> OptionalParamDefs { get { return new CommandParam[0]; } }
        public virtual IEnumerable<CommandParam> ReturnValueDefs { get { return new CommandParam[0]; } }
        private CancellationTokenSource _cancellationTokenSource;
        public CancellationToken CancellationToken { get; private set; } // long running commands must check this regularly and cancel accordingly
        public OsmpSession Session { get; set; }
        public Action<LogLevel, Action<StringBuilder>> Log { get; set; }

        public CommandParam[] ParamDefs
        {
            get { return (MandatoryParamDefs ?? new CommandParam[0]).Concat(OptionalParamDefs ?? new CommandParam[0]).Where(def => !string.IsNullOrWhiteSpace(def.Name)).ToArray(); }
        }

        public string LongText
        {
            get
            {
                var s = new StringBuilder();
                s.AppendFormat("Command: '{0}' (v{1})\n", Name, Version);
                if (Aliases != null && Aliases.Any())
                    s.AppendFormat("Aliases: {0}\n\n", string.Join(", ", Aliases.Select(a => "'" + a + "'").ToArray()));
                s.AppendFormat("Action: {0}\n", Description);
                s.AppendLine();
                s.Append("Parameters:");
                if (MandatoryParamDefs != null)
                {
                    foreach (var def in MandatoryParamDefs)
                    {
                        s.Append(" ");
                        s.Append(def.Name);
                    }
                }
                if (OptionalParamDefs != null)
                {
                    int i = 0;
                    foreach (var def in OptionalParamDefs)
                    {
                        s.Append(" [");
                        s.Append(def.Name);
                        i += 1;
                    }
                    s.AppendLine(new string(']', i));
                }
                foreach (var def in ParamDefs)
                {
                    s.AppendFormat("    {0}: {1}\n", def.Name, def.Description);
                }
                if (ParamDefs.Any())
                {
                    s.AppendLine();
                    s.Append("JSON template: ");
                    s.Append(Name);
                    s.Append(" {");
                    s.Append(string.Join(", ", ParamDefs.Select(p => p.Name + ": \"\"").ToArray()));
                    s.AppendLine(" }");
                    //s.Append(Name);
                    //s.Append(" [");
                    //s.Append(string.Join(", ", ParamDefs.Select(p => "\"...\"").ToArray()));
                    //s.AppendLine(" ]");
                }
                s.AppendLine();
                s.Append("Return values: ");
                var retvals = ReturnValueDefs.Select(p => p.Name).ToArray();
                s.AppendLine(retvals.Length == 0 ? "none" : string.Join(" ", retvals));
                foreach (var def in ReturnValueDefs)
                {
                    s.AppendFormat("    {0}: {1}\n", def.Name, def.Description);
                }
                if (!string.IsNullOrWhiteSpace(LongDescription))
                {
                    s.AppendLine();
                    s.AppendLine("Long description:");
                    s.AppendLine(LongDescription);
                }
                if (Examples.Any())
                {
                    s.AppendLine();
                    s.AppendLine("Examples:");
                    foreach (var example in Examples)
                        s.AppendLine(example);
                }
                return s.ToString();
            }
        }

        public JObject Params { get; set; }
        public int CmdNr { get; set; }
        public DateTime StartTime { get; set; }

        public virtual async Task<CommandResult> Execute()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;
            // here is space to do some pre-processing
            return await ExecuteImplementation();
            // here is space to do some post-processing
        }

        protected abstract Task<CommandResult> ExecuteImplementation();

        #region --> Streaming

        public void Stream(string text)
        {
            Session.SendStream(this, new JObject { { "text", text } });
        }

        public void Stream(JObject data)
        {
            Session.SendStream(this, data);
        }

        public void StreamLine(string text)
        {
            Session.SendStream(this, new JObject { { "line", text } });
        }

        public void StreamFormat(string formatstring, params object[] @params)
        {
            Stream(string.Format(formatstring, @params));
        }

        public void StreamProgress(string message, int progress, int total)
        {
            Session.SendStream(this, new JObject { { "type", "progress" }, {"value", progress}, {"total", total} });
        }

        public void StreamProgressIndeterminate(string message)
        {
            Session.SendStream(this, new JObject { { "type", "progress-indeterminate" }, { "message", message }});
        }

        public void StreamProgressComplete()
        {
            Session.SendStream(this, new JObject { { "type", "progress-complete" } });
        }


        #endregion

        protected async Task<string[]> UploadFiles()
        {
            var server = Session;
            var id = Guid.NewGuid().ToString();
            var data = await server.AskUploadFiles(id);
            if (data == null)
                return null;
            var files = data.Get<JObject[]>("files");
            if (files == null)
                return null;
            var uploaded_files = new List<string>();
            foreach (JObject file_data in files)
            {
                if (CancellationToken.IsCancellationRequested)
                    return null;
                if (file_data == null)
                    continue;
                var uploaded_file = await UploadFile(file_data);
                if (uploaded_file == null)
                    continue;
                uploaded_files.Add(uploaded_file);
            }
            return uploaded_files.ToArray();
        }

        protected async Task<string> UploadFile()
        {
            var server = Session;
            var id = Guid.NewGuid().ToString();
            var data = await server.AskUploadFile(id);
            if (data == null)
                return null;
            return await UploadFile(data);
        }

        protected async Task<string> UploadFile(JObject data)
        {
            var server = Session;
            var id = data.Get<string>("id");
            var total_parts = data.Get<int>("total_parts");
            if (total_parts <= 0)
                return null;
            var filename = data.Get<string>("filename");
            if (string.IsNullOrWhiteSpace(filename))
                return null;
            var filepath = Path.Combine(server.TransferDir, filename);
            if (File.Exists(filepath))
            {
                var overwrite = await server.AskForConfirmation("Destination file already exists.",
                    "The file " + filepath + " will be overwritten.", "Overwrite!", "Cancel");
                if (overwrite != true)
                    return null;
            }
            //var filename = Path.GetFileName(filepath);
            using (var f = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                for (int i = 0; i < total_parts; i++)
                {
                    if (CancellationToken.IsCancellationRequested)
                        return null;
                    try
                    {
                        var base64 = await server.AskForUploadPart(id, i);
                        if (string.IsNullOrWhiteSpace(base64))
                            return null;
                        var bytes = Convert.FromBase64String(base64);
                        f.Write(bytes, 0, bytes.Length);
                    }
                    catch (Exception e)
                    {
                        Stream("Error:\r\n" + e.PrettyPrint());
                        return null;
                    }
                }
                f.Flush();
            }
            StreamProgressComplete();
            return filepath;
        }

        #region --> Conversion Helpers

        protected bool ToBool(object value)
        {
            if (value == null)
                return false;
            if (value is string)
            {
                switch ((value as string).ToLower())
                {
                    case "true":
                    case "on":
                    case "ok":
                    case "yes":
                    case "1":
                    case "y":
                        return true;
                    default:
                        return false;
                }
            }
            if (value is bool)
                return (bool)value;
            if (value is int)
                return (int)value == 1;
            return false;
        }

        protected bool ToBool(JToken value)
        {
            if (value == null)
                return false;
            if (value.Type== JTokenType.String)
            {
                switch (value.ToString().ToLower())
                {
                    case "true":
                    case "on":
                    case "ok":
                    case "yes":
                    case "1":
                    case "y":
                        return true;
                    default:
                        return false;
                }
            }
            if (value.Type== JTokenType.Boolean)
                return (bool)value;
            if (value.Type==JTokenType.Integer)
                return (int)value == 1;
            return false;
        }

        protected string[] ToStringArray(object obj)
        {
            if (obj == null)
                return new string[0];
            if (obj is string)
                return new string[] { obj as string }; //Conversion.ToStringArray(obj as string);
            if (obj is object[])
                return (obj as object[]).OfType<string>().ToArray();
            if (obj is string[])
                return obj as string[];
            return new string[0];
        }

        protected Regex ToRegex(object obj)
        {
            if (obj == null || !(obj is string))
                return null;
            var filter = obj as string;
            if (string.IsNullOrWhiteSpace(filter))
                return null;
            return new Regex(filter);
        }

        #endregion

        public void Cancel()
        {
            if (_cancellationTokenSource == null)
                return;
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            try
            {
                DisposeImpl();
            }
            catch (Exception e)
            {
                Log(LogLevel.ERROR, s => s.Append("Error disposing cmd: " + e.PrettyPrint()));
            }
        }

        protected virtual void DisposeImpl()
        {
            
        }
    }


}
