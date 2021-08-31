namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;

    [DebuggerDisplay("{Address}")]
    internal class HclResource
    {
        private readonly List<string> lines = new List<string>();

        public HclResource(string address)
        {
            var addressParts = address.Split('.');

            this.Type = addressParts[0];
            this.Name = addressParts[1];
        }

        public string Address => $"{this.Type}.{this.Name}";

        public string Name { get; }

        public string Type { get; }

        public List<string> Lines => this.lines;

        public override string ToString()
        {
            return string.Join(Environment.NewLine, this.lines);
        }

        public bool SetAttributeRefValue(string attributeName, ResourceImport targetResource)
        {
            var rx = new Regex(@"\s*" + attributeName + @"\s*=\s*(?<id>""[^""]+"")");

            for (var ind = 0; ind < this.lines.Count; ++ind)
            {
                var m = rx.Match(this.lines[ind]);

                if (!m.Success)
                {
                    continue;
                }

                this.lines[ind] = this.lines[ind].Replace(m.Groups["id"].Value, $"{targetResource.Address}.id");
                return true;
            }

            return false;
        }

        public bool SetAttributeArrayRefValue(string attributeName, ResourceImport targetResource)
        {
            var arrayStartRx = new Regex(@"\s*" + attributeName + @"\s*=\s*\[");

            for (var ind = 0; ind < this.lines.Count; ++ind)
            {
                var m = arrayStartRx.Match(this.lines[ind]);

                if (!m.Success)
                {
                    continue;
                }

                // Now search array values for matching physical ID
                var idToFind = "\"" + targetResource.PhysicalId + "\"";

                for (var ind2 = ind + 1; !Regex.IsMatch(this.lines[ind2], @"^\s*\]"); ++ind2)
                {
                    if (this.lines[ind2].Contains(idToFind))
                    {
                        this.lines[ind2] = this.lines[ind2].Replace(idToFind, $"{targetResource.Address}.id");
                        return true;
                    }
                }
            }

            return false;
        }
    }
}