using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ResMap
{
    public class ResourceMapTask : Task
    {
        [Required]
        public ITaskItem[] Inputs { get; set; }

        [Required]
        public string RootNamespace { get; set; }

        [Required]
        public string WorkDir { get; set; }

        [Output]
        public string MappingFilePath { get; set; }

        public override bool Execute()
        {
            var resources = this.Inputs.Select(x => this.GetLogicalname(x)).ToDictionary(x => x.Key, x => x.Value);
            var fileContents = this.GetFileContents(resources);

            this.MappingFilePath = this.GenerateFile(fileContents);

            return true;
        }

        private KeyValuePair<string, string> GetLogicalname(ITaskItem item)
        {
            var logicalName = item.GetMetadata("LogicalName");
            var filePath = item.GetMetadata("FullPath");
            if (string.IsNullOrEmpty(logicalName))
            {
                var projectDir = item.GetMetadata("DefiningProjectDirectory");
                var partialFilePath = filePath.Replace(projectDir, string.Empty);
                var logicalFilePath = partialFilePath.Replace("\\", ".");
                logicalName = string.Format("{0}.{1}", this.RootNamespace, logicalFilePath);
            }

            return new KeyValuePair<string, string>(logicalName, filePath);
        }

        private string GetFileContents(IDictionary<string, string> mappings)
        {
            var contents = ResourceMapTask.EmbeddedResMapText;
            var builder = new StringBuilder();
            foreach (var mapping in mappings)
                builder.AppendFormat("\t\t\tMappings.Add(\"{0}\", @\"{1}\");", mapping.Key, mapping.Value).AppendLine();

            return contents.Replace("INSERT_MAPPINGS_HERE", builder.ToString());
        }

        private string GenerateFile(string fileContents)
        {
            var resMapFolder = Path.Combine(this.WorkDir, "ResMap");
            if (!Directory.Exists(resMapFolder))
                Directory.CreateDirectory(resMapFolder);

            var fileName = Path.Combine(resMapFolder, "Mappings.cs");
            using (var writer = new StreamWriter(fileName))
            {
                writer.Write(fileContents);
            }

            return fileName;
        }

        private const string EmbeddedResMapText = @"
        using System.Collections.Generic;
        namespace MsBuildGenerated
        {
            internal class EmbeddedResMap
            {
                static EmbeddedResMap()
                {
                    Mappings = new Dictionary<string, string>();
        INSERT_MAPPINGS_HERE
                }

                internal static IDictionary<string, string> Mappings { get; private set; }
            }
        }";
    }
}
