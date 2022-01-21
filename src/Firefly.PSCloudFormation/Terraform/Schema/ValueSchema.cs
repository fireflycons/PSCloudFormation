namespace Firefly.PSCloudFormation.Terraform.Schema
{
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Describes the type of the value represented by a <see cref="ValueSchema"/> item.
    /// </summary>
    internal enum SchemaValueType
    {
        /// <summary>
        /// Invalid type
        /// </summary>
        TypeInvalid = 0,

        /// <summary>
        /// Boolean value
        /// </summary>
        TypeBool,

        /// <summary>
        /// Integer value
        /// </summary>
        TypeInt,

        /// <summary>
        /// Double value (name preserved to match with the Go definition)
        /// </summary>
        TypeFloat,

        /// <summary>
        /// String value
        /// </summary>
        TypeString,

        /// <summary>
        /// List of objects
        /// </summary>
        TypeList,

        /// <summary>
        /// Dictionary of string, object
        /// </summary>
        TypeMap,

        /// <summary>
        /// A set, probably should be a HashSet{object}
        /// </summary>
        TypeSet,

        /// <summary>
        /// An object
        /// </summary>
        TypeObject
    }

    /// <summary>
    /// SchemaConfigMode is used to influence how a schema item is mapped into a
    /// corresponding configuration construct, using the ConfigMode field of
    /// Schema.
    /// </summary>
    internal enum SchemaConfigMode
    {
        /// <summary>
        /// Auto mode
        /// </summary>
        SchemaConfigModeAuto = 0,

        /// <summary>
        /// Map as attribute
        /// </summary>
        SchemaConfigModeAttr,

        /// <summary>
        /// Map as block
        /// </summary>
        SchemaConfigModeBlock,
    }

    /// <summary>
    /// Schema is used to describe the structure of a value.
    /// </summary>
    internal class ValueSchema
    {
        /// <summary>
        /// The nested schema
        /// </summary>
        private object nestedSchema;

        /// <summary>
        /// Gets or sets at least one of.
        /// </summary>
        /// <value>
        /// AtLeastOneOf is a set of schema keys that, when set, at least one of
        /// the keys in that list must be specified.
        /// </value>
        public List<string> AtLeastOneOf { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ValueSchema"/> is computed.
        /// </summary>
        /// <value>
        /// If Computed is true, then the result of this value is computed
        /// (unless specified by config) on creation.
        /// </value>
        public bool Computed { get; set; }

        /// <summary>
        /// Gets or sets the configuration mode.
        /// </summary>
        /// <value>
        /// ConfigMode allows for overriding the default behaviors for mapping
        /// schema entries onto configuration constructs.
        ///
        /// By default, the Elem field is used to choose whether a particular
        /// schema is represented in configuration as an attribute or as a nested
        /// block; if Elem is a *schema.Resource then it's a block and it's an
        /// attribute otherwise.
        ///
        /// If Elem is *schema.Resource then setting ConfigMode to
        /// SchemaConfigModeAttr will force it to be represented in configuration
        /// as an attribute, which means that the Computed flag can be used to
        /// provide default elements when the argument isn't set at all, while still
        /// allowing the user to force zero elements by explicitly assigning an
        /// empty list.
        ///
        /// When Computed is set without Optional, the attribute is not settable
        /// in configuration at all and so SchemaConfigModeAttr is the automatic
        /// behavior, and SchemaConfigModeBlock is not permitted.
        /// </value>
        public SchemaConfigMode ConfigMode { get; set; }

        /// <summary>
        /// Gets or sets the conflicts with.
        /// </summary>
        /// <value>
        /// ConflictsWith is a set of schema keys that conflict with this schema.
        /// This will only check that they're set in the _config_. This will not
        /// raise an error for a malfunctioning resource that sets a conflicting
        /// key.
        /// </value>
        public List<string> ConflictsWith { get; set; }

        /// <summary>
        /// Gets or sets the default.
        /// </summary>
        /// <value>
        /// If this is non-null, then this will be a default value that is used
        /// when this item is not set in the configuration.
        /// If Required is true, then Default cannot be set
        /// </value>
        public object Default { get; set; }

        /// <summary>
        /// Gets or sets the element.
        /// </summary>
        /// <value>
        /// This is only set for a TypeList, TypeSet, or TypeMap.
        /// Elem represents the element type. For a TypeMap, it must be a *Schema
        /// with a Type that is one of the primitives: TypeString, TypeBool,
        /// TypeInt, or TypeFloat. Otherwise it may be either a *Schema or a
        /// *Resource. If it is *Schema, the element type is just a simple value.
        /// If it is *Resource, the element type is a complex structure,
        /// potentially managed via its own CRUD actions on the API.
        /// </value>
        public object Elem
        {
            get => this.nestedSchema;
            set
            {
                if (value is JObject tok)
                {
                    if (tok.SelectToken("Schema") != null)
                    {
                        this.nestedSchema = tok.ToObject<ResourceSchema>();
                    }
                    else
                    {
                        this.nestedSchema = tok.ToObject<ValueSchema>();
                    }
                }
                else
                {
                    this.nestedSchema = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the exactly one of.
        /// </summary>
        /// <value>
        /// ExactlyOneOf is a set of schema keys that, when set, only one of the
        /// keys in that list can be specified. It will error if none are
        /// specified as well.
        /// </value>
        public List<string> ExactlyOneOf { get; set; }

        /// <summary>
        /// Gets or sets the maximum items.
        /// </summary>
        /// <value>
        /// MaxItems defines a maximum amount of items that can exist within a
        /// TypeSet or TypeList. Specific use cases would be if a TypeSet is being
        /// used to wrap a complex structure, however more than one instance would
        /// cause instability.
        /// </value>
        public int MaxItems { get; set; }

        /// <summary>
        /// Gets or sets the minimum items.
        /// </summary>
        /// <value>
        /// MinItems defines a minimum amount of items that can exist within a
        /// TypeSet or TypeList. Specific use cases would be if a TypeSet is being
        /// used to wrap a complex structure, however less than one instance would
        /// cause instability.
        /// </value>
        public int MinItems { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ValueSchema"/> value is optional.
        /// Mutually exclusive with Required. Both cannot be true.
        /// </summary>
        /// <value>
        ///   Mutually exclusive with Required. Both cannot be true.
        /// </value>
        public bool Optional { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ValueSchema"/> value is required.
        /// Mutually exclusive with Optional. Both cannot be true.
        /// </summary>
        /// <value>
        ///   <c>true</c> if required; otherwise, <c>false</c>.
        /// </value>
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets the required with.
        /// </summary>
        /// <value>
        /// RequiredWith is a set of schema keys that must be set simultaneously.
        /// </value>
        public List<string> RequiredWith { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ValueSchema"/> is sensitive.
        /// </summary>
        /// <value>
        /// Sensitive ensures that the attribute's value does not get displayed in
        /// logs or regular output. It should be used for passwords or other
        /// secret fields. Future versions of Terraform may encrypt these
        /// values.
        /// </value>
        public bool Sensitive { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// Type is the type of the value and must be one of the ValueType values.
        ///
        /// This type not only determines what type is expected/valid in configuring
        /// this value, but also what type is returned when ResourceData.Get is
        /// called. The types returned by Get are:
        ///
        ///   TypeBool - bool
        ///   TypeInt - int
        ///   TypeFloat - double
        ///   TypeString - string
        ///   TypeList - IEnumerable{object}
        ///   TypeMap - Dictionary{string, object}
        ///   TypeSet - *schema.Set
        ///
        /// </value>
        public SchemaValueType Type { get; set; }

        /// <summary>
        /// Determines whether this value should be rendered as a block.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance value should be rendered as a block; otherwise, <c>false</c>.
        /// </returns>
        public bool IsBlock()
        {
            if (this.Computed && !this.Optional)
            {
                // When Computed is set without Optional, the attribute is not settable
                // in configuration at all and so SchemaConfigModeAttr is the automatic
                // behavior, and SchemaConfigModeBlock is not permitted.
                return false;
            }

            if (this.ConfigMode == SchemaConfigMode.SchemaConfigModeAuto)
            {
                // By default, the Elem field is used to choose whether a particular
                // schema is represented in configuration as an attribute or as a nested
                // block; if Elem is a *schema.Resource then it's a block and it's an
                // attribute otherwise.
                return this.Elem is ResourceSchema;
            }

            return this.ConfigMode == SchemaConfigMode.SchemaConfigModeBlock;
        }

        /// <summary>
        /// Determines whether this value is a multi-value type, i.e. list or set.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is list or set]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsListOrSet()
        {
            return new[] { SchemaValueType.TypeList, SchemaValueType.TypeSet }.Contains(this.Type);
        }
    }
}