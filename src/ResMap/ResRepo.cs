using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ResMap
{
    public class ResRepo
    {
        private static IDictionary<string, string> allMaps = new Dictionary<string, string>();
        private static ISet<string> probedAssemblies = new HashSet<string>();
        private static readonly object syncLock = new object();

        public static bool TryGetPath(string assemblyName, string resName, out string filePath)
        {
            if (!allMaps.TryGetValue(resName, out filePath) && !probedAssemblies.Contains(assemblyName))
            {
                lock (syncLock)
                {
                    if (!allMaps.TryGetValue(resName, out filePath) && !probedAssemblies.Contains(assemblyName))
                    {
                        var asm = Assembly.Load(assemblyName);
                        var asmType = asm.GetTypes().FirstOrDefault(x => x.Name == ResourceMapTask.Constants.TypeName && x.Namespace == ResourceMapTask.Constants.Namespace);
                        if (asmType != null)
                        {
                            var prop = asmType.GetProperty(ResourceMapTask.Constants.PropertyName, BindingFlags.Static | BindingFlags.NonPublic);
                            var mappings = prop.GetValue(null, null) as IDictionary<string, string>;

                            foreach (var mapping in mappings)
                            {
                                allMaps[mapping.Key] = mapping.Value;
                            }

                            mappings = null;
                        }

                        probedAssemblies.Add(assemblyName);
                    }
                }

                return allMaps.TryGetValue(resName, out filePath);
            }

            return filePath != null;
        }
    }
}
