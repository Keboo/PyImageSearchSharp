using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;

namespace OpenCVSharp
{
    public class DefaultProgram
    {
        private static readonly Regex _embededResourceMatch = new Regex(@"\<Embeded\|(?<ResourceName>[\w\.]+)\>");

        public static int Main(string[] args)
        {
            var assembly = Assembly.GetEntryAssembly();
            var types = assembly.GetTypes();
            var program = types.SingleOrDefault(x => x.Name == "Program");

            if (program == null)
            {
                Console.WriteLine($@"Failed to find entry Program class in {assembly.CodeBase}");
                Console.ReadLine();
                return 1;
            }

            MethodInfo runMethod = program.GetMethod("Run");
            if (runMethod == null)
            {
                Console.WriteLine($@"Failed to find Run method in {program.FullName}");
                Console.ReadLine();
                return 1;
            }
            if (!runMethod.IsStatic)
            {
                Console.WriteLine($@"{program.FullName}.{runMethod.Name} must be static");
                Console.ReadLine();
                return 1;
            }

            ParameterInfo[] parameters = runMethod.GetParameters();
            object[] paramValues = new object[parameters.Length];
            List<FileInfo> tempFiles = new List<FileInfo>();
            switch (parameters.Length)
            {
                case 0:
                    break;
                case 1:
                    var options = Activator.CreateInstance(parameters[0].ParameterType);
                    if (!Parser.Default.ParseArguments(args, options))
                    {
                        Console.WriteLine(HelpText.AutoBuild(options));
                        Console.ReadLine();
                        return 1;
                    }
                    foreach (PropertyInfo property in parameters[0].ParameterType.GetProperties()
                        .Where(pi => pi.GetCustomAttribute<OptionAttribute>() != null &&
                                     pi.PropertyType == typeof(string)))
                    {
                        var value = property.GetValue(options) as string;
                        Match match;
                        if (value != null && (match = _embededResourceMatch.Match(value)).Success)
                        {
                            string resourceName = match.Groups["ResourceName"].Value;
                            var assemblyResourceName =
                                assembly.GetManifestResourceNames()
                                    .FirstOrDefault(x => x.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));
                            if (assemblyResourceName == null)
                            {
                                Console.WriteLine($@"Failed to find embeded resource {resourceName}");
                                Console.ReadLine();
                                return 1;
                            }
                            var resourceStream = assembly.GetManifestResourceStream(assemblyResourceName);
                            if (resourceStream != null)
                            {
                                var tempFile = new FileInfo(resourceName);
                                using (var fs = tempFile.Open(FileMode.Create))
                                {
                                    resourceStream.CopyTo(fs);
                                }
                                property.SetValue(options, tempFile.FullName);
                                tempFiles.Add(tempFile);
                            }
                        }
                    }
                    paramValues[0] = options;
                    break;
                default:
                    Console.WriteLine($@"{program.FullName}.{runMethod.Name} contains too many parameters");
                    Console.ReadLine();
                    return 1;
            }

            try
            {
                object result = runMethod.Invoke(null, paramValues);

                //TODO: C# 7 switch statment would be great here
                if (result is int)
                {
                    return (int)result;
                }
                if (result is bool)
                {
                    return (bool)result ? 0 : 1;
                }
                return 0;
            }
            finally
            {
                foreach (FileInfo file in tempFiles)
                {
                    file.Delete();
                }
            }
        }
    }
}
