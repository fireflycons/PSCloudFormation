namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Schema
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
        TypeBool = 1,

        /// <summary>
        /// Integer value
        /// </summary>
        TypeInt = 2,

        /// <summary>
        /// Double value (name preserved to match with the Go definition)
        /// </summary>
        TypeFloat = 3,

        /// <summary>
        /// String value
        /// </summary>
        TypeString = 4,

        /// <summary>
        /// List of objects
        /// </summary>
        TypeList = 5,

        /// <summary>
        /// Dictionary of string, object
        /// </summary>
        TypeMap = 6,

        /// <summary>
        /// A set of objects, i.e. unique values
        /// </summary>
        TypeSet = 7,

        /// <summary>
        /// An object
        /// </summary>
        TypeObject = 8,

        /// <summary>
        /// Internal type for emitting content of <c>jsonencode()</c>.
        /// This is always emitted as attribute data and all content is included
        /// </summary>
        TypeJsonData = 9
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
    /// <para>
    /// ValueSchema is used to describe the structure of a value.
    /// This is a cut-down version of the full schema in the terraform provider, having
    /// only the properties which can easily be used for HCL serialization
    /// </para>
    /// <para>
    /// There are other properties such as <c>ConflictsWith</c> which only make sense when
    /// validating user-created HCL. When serializing from the state file, all the properties
    /// are present and there is not enough information in the schema to decide which argument
    /// to select from a set of conflicting arguments, hence the rather kludge-worthy <see cref="IResourceTraits"/>
    /// stuff.
    /// </para>
    /// </summary>
    internal class ValueSchema
    {
        /// <summary>
        /// The scalar types
        /// </summary>
        private static readonly IEnumerable<SchemaValueType> ScalarTypes = new[]
                                                                               {
                                                                                   SchemaValueType.TypeBool,
                                                                                   SchemaValueType.TypeString,
                                                                                   SchemaValueType.TypeFloat,
                                                                                   SchemaValueType.TypeInt
                                                                               };

        /// <summary>
        /// The list types
        /// </summary>
        private static readonly IEnumerable<SchemaValueType> ListTypes =
            new[] { SchemaValueType.TypeList, SchemaValueType.TypeSet };

        /// <summary>
        /// The nested schema
        /// </summary>
        private object nestedSchema;

        /// <summary>
        /// Gets the JSON schema.
        /// </summary>
        /// <value>
        /// The JSON schema.
        /// </value>
        public static ValueSchema JsonSchema { get; } = new ValueSchema
                                                            {
                                                                Type = SchemaValueType.TypeJsonData, Required = true
                                                            };

        #region Imported Properties (from AWS provider)

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

        #endregion

        #region Additional Properties (for use by configuration generator)

        /// <summary>
        /// Gets a value indicating whether this instance should be rendered as a block.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is block; otherwise, <c>false</c>.
        /// </value>
        [JsonIgnore]
        public bool IsBlock
        {
            get
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

                if (this.Optional && this.Computed && this.ConfigMode == SchemaConfigMode.SchemaConfigModeAttr)
                {
                    // For the purpose of generating HCL, anything that's attribute-as-blocks 
                    // should be rendered as blocks
                    // https://www.terraform.io/language/attr-as-blocks
                    return true;
                }

                return this.ConfigMode == SchemaConfigMode.SchemaConfigModeBlock;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this value is computed only.
        /// Such values must be omitted when generating configuration.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is computed only; otherwise, <c>false</c>.
        /// </value>
        [JsonIgnore]
        public bool IsComputedOnly => this.Computed && !(this.Optional || this.Required);

        /// <summary>
        /// Gets a value indicating whether this instance is a list or set.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is list or set; otherwise, <c>false</c>.
        /// </value>
        [JsonIgnore]
        public bool IsListOrSet => ListTypes.Contains(this.Type);

        /// <summary>
        /// Gets a value indicating whether this instance is scalar.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is scalar; otherwise, <c>false</c>.
        /// </value>
        [JsonIgnore]
        public bool IsScalar => ScalarTypes.Contains(this.Type);

        #endregion
    }
}