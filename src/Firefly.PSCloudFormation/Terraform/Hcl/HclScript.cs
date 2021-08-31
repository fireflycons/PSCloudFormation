namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Container for editing an HCL script
    /// </summary>
    internal class HclScript
    {
        /// <summary>
        /// The lines of the file
        /// </summary>
        private readonly List<string> lines = new List<string>();

        /// <summary>
        /// The path to script
        /// </summary>
        private readonly string pathToScript;

        /// <summary>
        /// Initializes a new instance of the <see cref="HclScript"/> class.
        /// </summary>
        /// <param name="path">Path to script.</param>
        public HclScript(string path)
        {
            this.pathToScript = path;

            if (File.Exists(path))
            {
                this.lines = File.ReadAllLines(path).ToList();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.String"/> with the specified line number.
        /// </summary>
        /// <value>
        /// The text at the given line.
        /// </value>
        /// <param name="lineNumber">The line number.</param>
        /// <returns>Text of the given line</returns>
        /// <exception cref="System.IndexOutOfRangeException">
        /// Invalid line {lineNumber} in HCL file
        /// or
        /// Invalid line {lineNumber} in HCL file
        /// </exception>
        public string this[int lineNumber]
        {
            get
            {
                if (lineNumber < 1 || lineNumber > this.lines.Count)
                {
                    throw new IndexOutOfRangeException($"Invalid line {lineNumber} in HCL file");
                }

                return this.lines[lineNumber - 1];
            }

            set
            {
                if (lineNumber < 1 || lineNumber > this.lines.Count)
                {
                    throw new IndexOutOfRangeException($"Invalid line {lineNumber} in HCL file");
                }

                this.lines[lineNumber - 1] = value;
            }
        }

        /// <summary>
        /// Removes lines from script by line number.
        /// </summary>
        /// <param name="lineNumbers">The line numbers.</param>
        public void RemoveLines(IEnumerable<int> lineNumbers)
        {
            foreach (var ind in lineNumbers.OrderByDescending(n => n))
            {
                this.lines.RemoveAt(ind - 1);
            }
        }

        /// <summary>
        /// Fixes up variable references.
        /// </summary>
        /// <remarks>
        /// VERY EXPERIMENTAL!
        /// For now, using only String scalar variables having a non-blank default value,
        /// simply grep for attribute values matching the variable's default and replace
        /// </remarks>
        public void FixUpVariableReferences(IList<InputVariable> parameters)
        {
            foreach (var parameter in parameters.Where(p => p.IsScalar && p.Type == "string"))
            {
                var valueToMatch = $"\"{parameter.DefaultValue}\"";

                for (var ind = 0; ind < this.lines.Count; ++ind)
                {
                    if (this.lines[ind].Contains(valueToMatch) && !this.lines[ind].StartsWith(InputVariable.DefaultDeclaration))
                    {
                        this.lines[ind] = this.lines[ind].Replace(valueToMatch, $"var.{parameter.Name}");
                    }
                }
            }
        }

        /// <summary>
        /// Saves the script.
        /// </summary>
        public void Save()
        {
            File.WriteAllLines(this.pathToScript, this.lines, new UTF8Encoding(false));
        }
    }
}