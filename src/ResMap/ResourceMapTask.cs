using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ResMap
{
    public class ResourceMapTask : Microsoft.Build.Utilities.Task
    {
        [Microsoft.Build.Framework.Required]
        public Microsoft.Build.Framework.ITaskItem[] Inputs { get; set; }

        [Microsoft.Build.Framework.Required]
        public string RootNamespace { get; set; }

        [Microsoft.Build.Framework.Required]
        public string WorkDir { get; set; }

        [Microsoft.Build.Framework.Output]
        public string MappingFilePath { get; set; }

        public override bool Execute()
        {
            var resources = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var input in this.Inputs)
            {
                var pair = this.GetLogicalname(input);
                resources.Add(pair.Key, pair.Value);
            }
            var fileContents = this.GetFileContents(resources);

            this.MappingFilePath = this.GenerateFile(fileContents);

            return true;
        }

        private System.Collections.Generic.KeyValuePair<string, string> GetLogicalname(Microsoft.Build.Framework.ITaskItem item)
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

            return new System.Collections.Generic.KeyValuePair<string, string>(logicalName, filePath);
        }

        private string GetFileContents(System.Collections.Generic.IDictionary<string, string> mappings)
        {
            var contents = this.embeddedResMapText;
            var builder = new System.Text.StringBuilder();
            foreach (var mapping in mappings)
                builder.AppendFormat("\t\t\t{0}.Add(\"{1}\", @\"{2}\");", Constants.PropertyName, mapping.Key, mapping.Value).AppendLine();

            return contents.Replace(Constants.MappingsPlaceHolder, builder.ToString());
        }

        private string GenerateFile(string fileContents)
        {
            var resMapFolder = System.IO.Path.Combine(this.WorkDir, Constants.Namespace);
            if (!System.IO.Directory.Exists(resMapFolder))
                System.IO.Directory.CreateDirectory(resMapFolder);

            var fileName = System.IO.Path.Combine(resMapFolder, "Mappings.cs");
            using (var writer = new System.IO.StreamWriter(fileName))
            {
                writer.Write(fileContents);
            }

            return fileName;
        }

        internal class Constants
        {
            internal const string Namespace = "ResMap";
            internal const string TypeName = "EmbeddedResMap";
            internal const string PropertyName = "Mappings";
            internal const string MappingsPlaceHolder = "INSERT_MAPPINGS_HERE";
        }

        private readonly string embeddedResMapText = string.Format(@"
                  using System.Collections.Generic;
                  namespace {0}
                  {{
                      internal class {1}
                      {{
                          static {1}()
                          {{
                              {2} = new Dictionary<string, string>();
                              {3}
                          }}

                          internal static IDictionary<string, string> {2} {{ get; private set; }}
                      }}
                  }}", Constants.Namespace, Constants.TypeName, Constants.PropertyName, Constants.MappingsPlaceHolder);
    }
}
