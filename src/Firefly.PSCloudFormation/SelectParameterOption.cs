using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation
{
    using System.Reflection;

    internal class SelectParameterOption
    {
        public PropertyInfo SelectedProperty { get; set; }

        public string SelectedOutput { get; set; }
    }
}
