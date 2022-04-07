using System;
using System.Collections.Generic;
using System.IO;
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

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace eqqon.Osmp
{

    public abstract class AbstractEvent
    {
        public virtual int Version { get { return 0; } } // <-- this is used to overwrite events in different modules. the higher version number wins
        public abstract string InstructionSet { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public virtual string LongDescription { get { return null; } }

        public virtual IEnumerable<CommandParam> ParamDefs
        {
            get
            {
                yield return new CommandParam() { Name = "date-time", Type = "string", Description = "Time stamp of the change" };
            }
        }

        public JObject Data=new JObject { { "date-time", DateTime.Now } };
        public OsmpSession Session { get; set; }

        public void OnRegister() { OnRegisterImpl(); }

        public void OnUnregister() { OnUnregisterImpl(); }

        protected virtual void OnRegisterImpl() { }
        protected virtual void OnUnregisterImpl() { }

        //public string LongText
        //{
        //    get
        //    {
        //        var s = new StringBuilder();
        //        s.AppendFormat("Command: '{0}' (v{1})\n", Name, Version);
        //        if (Aliases != null && Aliases.Any())
        //            s.AppendFormat("Aliases: {0}\n\n", string.Join(", ", Aliases.Select(a => "'" + a + "'").ToArray()));
        //        s.AppendFormat("Action: {0}\n", Description);
        //        s.AppendLine();
        //        s.Append("Parameters:");
        //        if (MandatoryParamDefs != null)
        //        {
        //            foreach (var def in MandatoryParamDefs)
        //            {
        //                s.Append(" ");
        //                s.Append(def.Name);
        //            }
        //        }
        //        if (OptionalParamDefs != null)
        //        {
        //            int i = 0;
        //            foreach (var def in OptionalParamDefs)
        //            {
        //                s.Append(" [");
        //                s.Append(def.Name);
        //                i += 1;
        //            }
        //            s.AppendLine(new string(']', i));
        //        }
        //        foreach (var def in ParamDefs)
        //        {
        //            s.AppendFormat("    {0}: {1}\n", def.Name, def.Description);
        //        }
        //        if (ParamDefs.Any())
        //        {
        //            s.AppendLine();
        //            s.Append("JSON template: ");
        //            s.Append(Name);
        //            s.Append(" {");
        //            s.Append(string.Join(", ", ParamDefs.Select(p => p.Name + ": \"\"").ToArray()));
        //            s.AppendLine(" }");
        //            //s.Append(Name);
        //            //s.Append(" [");
        //            //s.Append(string.Join(", ", ParamDefs.Select(p => "\"...\"").ToArray()));
        //            //s.AppendLine(" ]");
        //        }
        //        s.AppendLine();
        //        s.Append("Return values: ");
        //        var retvals = ReturnValueDefs.Select(p => p.Name).ToArray();
        //        s.AppendLine(retvals.Length == 0 ? "none" : string.Join(" ", retvals));
        //        foreach (var def in ReturnValueDefs)
        //        {
        //            s.AppendFormat("    {0}: {1}\n", def.Name, def.Description);
        //        }
        //        if (!string.IsNullOrWhiteSpace(LongDescription))
        //        {
        //            s.AppendLine();
        //            s.AppendLine("Long description:");
        //            s.AppendLine(LongDescription);
        //        }
        //        if (Examples.Any())
        //        {
        //            s.AppendLine();
        //            s.AppendLine("Examples:");
        //            foreach (var example in Examples)
        //                s.AppendLine(example);
        //        }
        //        return s.ToString();
        //    }
        //}


    }


}
