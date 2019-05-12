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
using Osmp.Extensions;


namespace Osmp.StandardCmds
{

    [OsmpCommand]
    public class HelpCommand : AbstractCommand
    {
        public override string InstructionSet { get { return "standard"; } }
        public override string Name { get { return "help"; } }

        public override IEnumerable<string> Aliases
        {
            get { yield return "?"; }
        }

        public override string Description { get { return "List available commands or display extended command help."; } }

        public override string LongDescription { get { return @"Without parameters, help will list all available commands. 
With parameter 'cmd' set it will return detailed help on the given command, including parameter description and return values."; } }

        public override IEnumerable<string> Examples
        {
            get {
                yield return "help";
                yield return "?";
                yield return "help download";
                yield return "? dl";
            }
        }

        public override IEnumerable<CommandParam> MandatoryParamDefs
        {
            get { yield break; }
        }

        public override IEnumerable<CommandParam> OptionalParamDefs
        {
            get { yield return new CommandParam(){ Name = "cmd", Description = "The command to print extended help about."}; }
        }

        public override IEnumerable<CommandParam> ReturnValueDefs
        {
            get
            {
                yield return new CommandParam() { Name = "commands", Description = "List of available commands (returned only if parameter cmd is omitted)" };
                yield return new CommandParam() { Name = "command", Description = "Name of the optionally specified command" };
                yield return new CommandParam() { Name = "aliases", Description = "List of aliases for the command" };
                yield return new CommandParam() { Name = "version", Description = "Command version number" };
                yield return new CommandParam() { Name = "instruction-set", Description = "Instruction set is the name of the domain specific command group" };
                yield return new CommandParam() { Name = "description", Description = "Short description of the command" };
                yield return new CommandParam() { Name = "long-description", Description = "Longer more detailed description of the command" };
                yield return new CommandParam() { Name = "mandatory-params", Description = "List of mandatory parameters (must be specified)" };
                yield return new CommandParam() { Name = "optional-params", Description = "List of optional parameters (can be omitted)" };
                yield return new CommandParam() { Name = "return-values", Description = "List of return values" };
            }
        }

        private static Dictionary<string, AbstractCommand> _cmd_prototypes;

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
                        foreach (var alias in command.Aliases)
                            _cmd_prototypes[alias] = command;
                    }
                    catch (Exception e)
                    {
                        this.Log(LogLevel.ERROR, x => x.Append(e.PrettyPrint()));
                    }
                }
            }
            var result = new CommandResult();
            //var s = new StringBuilder();
            //result.Data = new Dictionary<string, object>();
            var cmd = Params.Get<string>("cmd");
            if (!string.IsNullOrWhiteSpace(cmd)) {
                // print command help
                AbstractCommand command = null;
                if (_cmd_prototypes.TryGetValue(cmd, out command))
                {
                    if (command == null)
                        return CommandResult.Fail("Command '"+cmd+"' not found!");
                    result.Data["command"]=command.Name;
                    result.Data["aliases"] = new JArray(command.Aliases);
                    result.Data["version"] = command.Version;
                    result.Data["instruction-set"] = command.InstructionSet;
                    result.Data["description"] = command.Description;
                    result.Data["long-description"] = command.LongDescription;
                    result.Data["mandatory-params"] = new JArray(command.MandatoryParamDefs.Select(ParamToJobject));
                    result.Data["optional-params"] = new JArray(command.OptionalParamDefs.Select(ParamToJobject));
                    result.Data["return-values"] = new JArray(command.ReturnValueDefs.Select(ParamToJobject));
                    //result.Data["examples"] = command.Examples;
                }
                else
                    return CommandResult.Fail("Command '" + cmd + "' not found!");
            }
            else {
                // return list of commands
                var cmds = new List<object>();
                //var table = new TextFormatter.TableBuilder();
                //table.SetHeaders("Command", "Aliases", "Inst.Set", "Description");
                foreach (var command in _cmd_prototypes.Values.Distinct().OrderBy(c=>c.Name).ToArray())
                {
                    //var aliases = Conversion.ToCommaDelimitedString(command.Aliases);
                    //table.AddRow(command.Name, aliases, command.InstructionSet, command.Description);
                    cmds.Add(new JObject
                    {
                        { "command", command.Name },
                        { "aliases", new JArray(command.Aliases) },
                        { "instruction-set", command.InstructionSet },
                        { "description", command.Description },
                    });
                }
                result.Data["commands"] = new JArray(cmds);
                //s.AppendLine(table.ToString());
            }
            //result.Text = s.ToString();
            return result;
        }

        private JObject ParamToJobject(CommandParam arg)
        {
            var obj= new JObject { {"name", arg.Name}, {"description",arg.Description} };
            if (!string.IsNullOrWhiteSpace(arg.Type))
                obj["type"] = arg.Type;
            return obj;
        }

        //private void FindSimilar(string cmd, StringBuilder s)
        //{
        //    // very primitive implementation 
        //    var similar_cmds=_cmd_prototypes.Keys.Where(x => x.Contains(cmd)).ToArray();
        //    if (similar_cmds.Length == 0)
        //        return;
        //    s.AppendLine("\n\nSimilar commands:");
        //    foreach (var cmd_name in similar_cmds)
        //    {
        //        s.AppendLine("   " + cmd_name);
        //    }
        //}
    }
}
