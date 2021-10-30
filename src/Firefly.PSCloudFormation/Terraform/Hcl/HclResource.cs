namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Represents a resource definition in HCL script
    /// </summary>
    [DebuggerDisplay("{Address}")]
    internal class HclResource
    {
        /// <summary>
        /// Regex to strip out fields returned by terraform show that don't belong in the HCL.
        /// </summary>
        private static readonly Regex invalidFieldsRegex = new Regex(@"^\s*(id|arn)");

        /// <summary>
        /// Initializes a new instance of the <see cref="HclResource"/> class.
        /// </summary>
        /// <param name="address">The resource address.</param>
        public HclResource(string address)
        {
            var addressParts = address.Split('.');

            this.Type = addressParts[0];
            this.Name = addressParts[1];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HclResource"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="lines">The lines that make up the resource definition.</param>
        public HclResource(string address, IEnumerable<string> lines)
        : this(address)
        {
            this.Lines = lines.Where(l => !invalidFieldsRegex.IsMatch(l)).ToList();
        }

        /// <summary>
        /// Gets the resource address.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        public string Address => $"{this.Type}.{this.Name}";

        /// <summary>
        /// Gets the resource name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the resource type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public string Type { get; }

        /// <summary>
        /// Gets the script lines.
        /// </summary>
        /// <value>
        /// The lines.
        /// </value>
        public List<string> Lines { get; } = new List<string>();

        /// <summary>
        /// Converts to string - i.e. the HCL representation of the resource.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Join(Environment.NewLine, this.Lines);
        }

        /// <summary>
        /// Sets the value of an attribute to reference the target resource rather than the physical ID.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="targetResource">The target resource to point to.</param>
        /// <returns><c>true</c> if the mapping was made; else <c>false</c></returns>
        public bool SetAttributeRefValue(string attributeName, ResourceImport targetResource)
        {
            var rx = new Regex(@"\s*" + attributeName + @"\s*=\s*(?<id>""[^""]+"")");

            for (var ind = 0; ind < this.Lines.Count; ++ind)
            {
                var m = rx.Match(this.Lines[ind]);

                if (!m.Success)
                {
                    continue;
                }

                this.Lines[ind] = this.Lines[ind].Replace(m.Groups["id"].Value, $"{targetResource.Address}.id");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the value of an attribute to reference the target resource rather than the physical ID, where the source attribute is an array of values.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="targetResource">The target resource to point to.</param>
        /// <returns><c>true</c> if the mapping was made; else <c>false</c></returns>
        public bool SetAttributeArrayRefValue(string attributeName, ResourceImport targetResource)
        {
            var arrayStartRx = new Regex(@"\s*" + attributeName + @"\s*=\s*\[");

            for (var ind = 0; ind < this.Lines.Count; ++ind)
            {
                var m = arrayStartRx.Match(this.Lines[ind]);

                if (!m.Success)
                {
                    continue;
                }

                // Now search array values for matching physical ID
                var idToFind = "\"" + targetResource.PhysicalId + "\"";

                for (var ind2 = ind + 1; !Regex.IsMatch(this.Lines[ind2], @"^\s*\]"); ++ind2)
                {
                    if (this.Lines[ind2].Contains(idToFind))
                    {
                        this.Lines[ind2] = this.Lines[ind2].Replace(idToFind, $"{targetResource.Address}.id");
                        return true;
                    }
                }
            }

            return false;
        }
    }
}