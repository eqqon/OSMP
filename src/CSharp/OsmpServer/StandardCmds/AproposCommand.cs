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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Osmp.Extensions;

namespace Osmp.StandardCmds
{

    [OsmpCommand]
    public class AproposCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }
        public override string Name { get { return "apropos"; } }

        public override IEnumerable<string> Aliases
        {
            get { yield return "ap"; }
        }

        public override string Description { get { return "Full text search in all available commands for given keyword."; } }

        public override IEnumerable<string> Examples
        {
            get
            {
                yield return "apropos download";
                yield return "ap download";
                yield return "apropos cpu";
            }
        }

        public override IEnumerable<CommandParam> MandatoryParamDefs
        {
            get { yield return new CommandParam() { Name = "term", Description = "The search filter (regex ignoring case)." }; }
        }

        public override IEnumerable<CommandParam> OptionalParamDefs
        {
            get { yield break; }
        }

        //public override IEnumerable<CommandParam> ReturnValueDefs
        //{
        //    get { yield return new CommandParam() { Name = "commands", Description = "List of matching commands" }; }
        //}

        private static Dictionary<string, AbstractCommand> _cmd_prototypes;
        private Regex _regex;

        protected override async Task<CommandResult> ExecuteImplementation()
        {
            var server = Session;
            if (_cmd_prototypes == null)
            {
                _cmd_prototypes = new Dictionary<string, AbstractCommand>();
                foreach (var info in server.Commands.Values)
                {
                    if (info.IsAlias)
                        continue;
                    try
                    {
                        var command = server.CreateInstance(info.Type) as AbstractCommand;
                        _cmd_prototypes[command.Name] = command;
                        //foreach (var alias in command.Aliases)
                        //    _cmd_prototypes[alias] = command;
                    }
                    catch (Exception e)
                    {
                        this.Log(LogLevel.ERROR, x => x.Append(e.PrettyPrint()));
                    }
                }
            }
            var s = new StringBuilder();
            var term = Params.Get<string>("term");
            if (string.IsNullOrWhiteSpace(term))
                return CommandResult.Fail("No search term given!");
            try
            {
                _regex = new Regex(term, RegexOptions.IgnoreCase);
            }
            catch (Exception e)
            {
                return CommandResult.Fail("Invalid search term: " + term + "\n" + e.PrettyPrint());
            }
            // find matching commands
            var cmds = _cmd_prototypes.Values.Where(IsMatch).OrderBy(cmd => cmd.Name).ToArray();
            //    // print help list
            //    result.Data["commands"] = prot.Commands.Keys.ToArray();
            //var table = new TextFormatter.TableBuilder();
            //table.SetHeaders("Command", "Aliases", "Description");
            var commands=new JArray();
            foreach (var command in cmds)
            {
                var aliases = command.Aliases;
                commands.Add(new JObject { {"cmd",command.Name}, {"aliases", new JArray(aliases) }, { "description", command.Description}, {"instruction-set", command.InstructionSet} });
            }
            //s.AppendLine(table.ToString());
            var result = CommandResult.Success();
            result.Data["commands"] = commands;
            return result;
        }

        private bool IsMatch(AbstractCommand cmd)
        {
            if (_regex.IsMatch(cmd.Name ?? ""))
                return true;
            foreach (var alias in cmd.Aliases)
            {
                if (string.IsNullOrEmpty(alias))
                    continue;
                if (_regex.IsMatch(alias))
                    return true;
            }
            if (cmd.ParamDefs.Any(IsMatch))
                return true;
            if (cmd.ReturnValueDefs.Any(IsMatch))
                return true;
            if (_regex.IsMatch(cmd.Description ?? ""))
                return true;
            if (_regex.IsMatch(cmd.LongDescription ?? ""))
                return true;
            foreach (var example in cmd.Examples)
            {
                if (string.IsNullOrEmpty(example))
                    continue;
                if (_regex.IsMatch(example))
                    return true;
            }
            return false;
        }

        private bool IsMatch(CommandParam p)
        {
            if (_regex.IsMatch(p.Name ?? ""))
                return true;
            if (_regex.IsMatch(p.Description ?? ""))
                return true;
            return false;
        }
    }
}
