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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Osmp.Extensions
{

    #region --> Exception

    public static class ExceptionExtension
    {
        public static string PrettyPrint(this Exception exception)
        {
            if (exception == null)
                return "";
            var string_builder = new StringBuilder();
            PrintRecursive(exception, "", string_builder);
            return string_builder.ToString();
        }

        private static void PrintRecursive(Exception exception, string indent, StringBuilder string_builder)
        {
            string stars = new string('*', 80);
            if (exception == null)
            {
                string_builder.AppendLine(indent + "<exception is null>");
                return;
            }
            string_builder.AppendLine(indent + stars);
            string_builder.AppendFormat(indent + "{0}: \"{1}\"\n", exception.GetType().Name, exception.Message);
            string_builder.AppendLine(indent + new string('-', 80));
            if (exception.InnerException != null)
            {
                string_builder.AppendLine(indent + "InnerException:");
                PrintRecursive(exception.InnerException, indent + "   ", string_builder);
            }
            FormatStacktrace(exception.StackTrace, indent, string_builder, stars);
        }

        private static void FormatStacktrace(string stackTrace, string indent, StringBuilder stringBuilder, string stars)
        {
            if (stackTrace == null)
            {
                stringBuilder.AppendLine(indent + "<stacktrace is null>");
                return;
            }
            foreach (string line in Regex.Split(stackTrace, "\r?\n"))
            {
                stringBuilder.AppendLine(indent + "  " + line);
            }
            stringBuilder.AppendLine(indent + stars);
        }

        //internal static void ParseStacktrace(string stacktrace, string indent, StringBuilder string_builder, string stars)
        //{
        //    if (stacktrace == null)
        //        string_builder.AppendLine(indent + "!!!! Stacktrace == null !!!!");
        //    else
        //        foreach (string line in Regex.Split(stacktrace, "\r?\n"))
        //        {
        //            var m = Regex.Match(line, @" at (.+?)( in (.+?):line (\d+))?");
        //            if (!m.Success)
        //                continue;
        //            string class_info = m.Groups[1].Value;
        //            if (parts.Length == 2)
        //            {
        //                parts = parts[1].Trim().Split(new string[] {"line"}, StringSplitOptions.RemoveEmptyEntries);
        //                if (parts.Length == 2)
        //                {
        //                    string src_file = parts[0];
        //                    int line_nr = int.Parse(parts[1]);
        //                    string_builder.AppendFormat(indent + "  {0}({1},1):   {2}\n", src_file.TrimEnd(':'), line_nr, class_info);
        //                }
        //                else
        //                    string_builder.AppendLine(indent + "  " + class_info);
        //            }
        //            else
        //                string_builder.AppendLine(indent + "  " + class_info);
        //        }
        //    string_builder.AppendLine(indent + stars);
        //}
    }

    #endregion

    #region --> Assembly
    public static class AssemblyExtensions
    {

        public static IEnumerable<Type> FindTypesByAttribute<T>(this Assembly assembly) where T : Attribute
        {
            return assembly.GetExportedTypes().Where(t => t.GetCustomAttributes(typeof(T), true).Count() > 0).ToArray();
        }


        /// <summary>
        /// Attempts to find and return the given resource from within the specified assembly.
        /// </summary>
        /// <returns>The embedded resource stream.</returns>
        /// <param name="assembly">Assembly.</param>
        /// <param name="resourceFileName">Resource file name.</param>
        public static Stream GetEmbeddedResourceStream(this Assembly assembly, string resourceFileName)
        {
            var resourceNames = assembly.GetManifestResourceNames();

            var resourcePaths = resourceNames
                .Where(x => x.EndsWith(resourceFileName, StringComparison.CurrentCultureIgnoreCase))
                .ToArray();

            if (!resourcePaths.Any())
            {
                throw new Exception(string.Format("Resource ending with {0} not found.", resourceFileName));
            }

            if (resourcePaths.Count() > 1)
            {
                throw new Exception(string.Format("Multiple resources ending with {0} found: {1}{2}", resourceFileName, Environment.NewLine, string.Join(Environment.NewLine, resourcePaths)));
            }

            return assembly.GetManifestResourceStream(resourcePaths.Single());
        }

        /// <summary>
        /// Attempts to find and return the given resource from within the specified assembly.
        /// </summary>
        /// <returns>The embedded resource as a byte array.</returns>
        /// <param name="assembly">Assembly.</param>
        /// <param name="resourceFileName">Resource file name.</param>
        public static byte[] GetEmbeddedResourceBytes(this Assembly assembly, string resourceFileName)
        {
            var stream = GetEmbeddedResourceStream(assembly, resourceFileName);

            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Attempts to find and return the given resource from within the specified assembly.
        /// </summary>
        /// <returns>The embedded resource as a string.</returns>
        /// <param name="assembly">Assembly.</param>
        /// <param name="resourceFileName">Resource file name.</param>
        public static string GetEmbeddedResourceString(this Assembly assembly, string resourceFileName)
        {
            var stream = GetEmbeddedResourceStream(assembly, resourceFileName);

            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }




    }
    #endregion

    #region --> Dictionary 

    public static class DictionaryExtensions
    {
        public static T Get<T>(this Dictionary<string, object> self, string field)
        {
            if (self == null)
                throw new ArgumentException("self must not be null");
            object value;
            if (!self.TryGetValue(field, out value))
                return default(T);
            return (T)value;
        }

        public static void Set(this Dictionary<string, object> self, string[] key_chain, object value)
        {
            if (key_chain.Length < 1)
                return;
            var current_dict = self;
            for (int i = 0; i < key_chain.Length - 1; i++)
            {
                var key = key_chain[i];
                var dict = current_dict.Get<Dictionary<string, object>>(key);
                if (dict == null)
                    current_dict[key] = dict = new Dictionary<string, object>();
                current_dict = dict;
            }
            current_dict[key_chain.Last()] = value;
        }


        public static T Get<T>(this Dictionary<string, object> self, string[] key_chain)
        {
            if (key_chain.Length < 1)
                return default(T);
            var current_dict = self;
            for (int i = 0; i < key_chain.Length - 1; i++)
            {
                var key = key_chain[i];
                current_dict = current_dict.Get<Dictionary<string, object>>(key);
                if (current_dict == null)
                    return default(T);
            }
            return current_dict.Get<T>(key_chain.Last());
        }
    }

    #endregion
}
