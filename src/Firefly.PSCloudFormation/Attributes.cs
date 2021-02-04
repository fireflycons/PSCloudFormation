// ReSharper disable StyleCop.SA1402
namespace Firefly.PSCloudFormation
{
    using System;

    /// <summary>
    /// Marker attribute for properties whose values can be returned with -Select
    /// </summary>
    /// <seealso cref="System.Attribute" />
    public class SelectableOutputPropertyAttribute : Attribute
    {
    }

    /// <summary>
    /// Marker attribute for cmdlet parameters that should not be returned by -Select
    /// </summary>
    /// <seealso cref="System.Attribute" />
    public class SuppressParameterSelectAttribute : Attribute
    {
    }
}