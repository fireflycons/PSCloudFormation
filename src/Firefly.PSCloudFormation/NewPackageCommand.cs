namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Firefly.CloudFormation.Parsers;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// <para type="synopsis">
    /// Packages the local artifacts (local paths) that your AWS CloudFormation template references, similarly to <c>aws cloudformation package</c>.
    /// </para>
    /// <para type="description">
    /// The command uploads local artifacts, such as source code for an AWS Lambda function or a Swagger file for an AWS API Gateway REST API, to an S3 bucket.
    /// The command returns a copy of your template, replacing references to local artifacts with the S3 location where the command uploaded the artifacts.
    /// Unlike <c>aws cloudformation package</c>, the output template is in the same format as the input template, i.e. there is no conversion from JSON to YAML.
    /// </para>
    /// <para type="description">
    /// Use this command to quickly upload local artifacts that might be required by your template.
    /// After you package your template's artifacts, run the <c>New-PSCFNStack</c> command to deploy the returned template.
    /// </para>
    /// <para type="description">
    /// To specify a local artifact in your template, specify a path to a local file or folder, as either an absolute or relative path.
    /// The relative path is a location that is relative to your template's location.
    /// </para>
    /// <para type="description">
    /// For example, if your AWS Lambda function source code is in the <c>/home/user/code/lambdafunction/</c> folder,
    /// specify <c>CodeUri: /home/user/code/lambdafunction</c> for the <c>AWS::Serverless::Function</c> resource.
    /// The command returns a template and replaces the local path with the S3 location: <c>CodeUri: s3://mybucket/lambdafunction.zip</c>.
    /// </para>
    /// <para type="description">
    /// If you specify a file, the command directly uploads it to the S3 bucket, zipping it first if the resource requires it (e.g. lambda).
    /// If you specify a folder, the command zips the folder and then uploads the .zip file.
    /// For most resources, if you don't specify a path, the command zips and uploads the current working directory.
    /// he exception is <c>AWS::ApiGateway::RestApi</c>; if you don't specify a <c>BodyS3Location</c>, this command will not upload an artifact to S3.
    /// </para>
    /// <para>
    /// For supported lambda runtimes (currently Python, Node and Ruby), this cmdlet can also package dependencies - a feature not currently available in <c>aws cloudformation package</c>.
    /// To package dependencies, you must create a file called lambda-dependencies.yaml or lambda-dependencies.json in the same directory as the script containing your handler function.
    /// This file is an array of objects describing dependency locations (directories) and a list of modules therein which should be included in the lambda package.
    /// Follow the link in the links section for more information on packaging dependencies.
    /// </para>
    /// <example>
    /// <code>
    /// New-PSCFNPackage -TemplateFile my-template.json -OutputTemplateFile my-modified-template.json
    /// </code>
    /// <para>
    /// Reads the template, recursively walking any <c>AWS::CloudFormation::Stack</c> resources, uploading code artifacts and nested templates to S3,
    /// using the bucket that is auto-created by this module.
    /// </para>
    /// </example>
    /// <example>
    /// <code>
    /// New-PSCFNPackage -TemplateFile my-template.json -OutputTemplateFile my-modified-template.json -S3Bucket my-bucket -S3Prefix template-resouces
    /// </code>
    /// <para>
    /// Reads the template, recursively walking any <c>AWS::CloudFormation::Stack</c> resources, uploading code artifacts and nested templates to S3,
    /// using the specified bucket which must exist, and key prefix for all uploaded objects.
    /// </para>
    /// </example>
    /// <example>
    /// <code>
    /// New-PSCFNPackage -TemplateFile my-template.json | New-PSCFNStack -StackName my-stack -ParameterFile stack-parameters.json
    /// </code>
    /// <para>
    /// Reads the template, recursively walking any <c>AWS::CloudFormation::Stack</c> resources, uploading code artifacts and nested templates to S3,
    /// then sends the modified template to <c>New-PSCFNStack</c> to create a new stack.
    /// </para>
    /// <para>
    /// Due to the nuances of PowerShell dynamic parameters, any stack customization parameters must be placed in a parameter file, as PowerShell starts
    /// the <c>New-PSCFNStack</c> cmdlet before it receives the template, therefore the template parameters cannot be known in advance.
    /// </para>
    /// </example>
    /// <para type="link" uri="https://github.com/fireflycons/PSCloudFormation/blob/master/static/lambda-dependencies.md">Packaging Lambda Dependencies</para>
    /// <para type="link" uri="https://github.com/fireflycons/PSCloudFormation/blob/master/static/s3-usage.md">PSCloudFormation private S3 bucket</para>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.CloudFormationServiceCommand" />
    [Cmdlet(VerbsCommon.New, "PSCFNPackage")]
    public class NewPackageCommand : CloudFormationServiceCommand
    {
        /// <summary>
        /// The output template file
        /// </summary>
        private string outputTemplateFile;

        /// <summary>
        /// The template file
        /// </summary>
        private string templateFile;

        /// <summary>
        /// Gets or sets the metadata.
        /// <para type="description">
        /// A map of metadata to attach to ALL the artifacts that are referenced in your template.
        /// </para>
        /// </summary>
        /// <value>
        /// The metadata.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public IDictionary Metadata { get; set; }

        /// <summary>
        /// Gets or sets the output template file.
        /// <para type="description">
        /// The path to the file where the command writes the output AWS CloudFormation template. If you don't specify a path, the command writes the template to the standard output.
        /// </para>
        /// </summary>
        /// <value>
        /// The output template file.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string OutputTemplateFile
        {
            get => this.outputTemplateFile;
            set => this.outputTemplateFile = this.ResolvePath(value);
        }

        /// <summary>
        /// Gets or sets the s3 bucket.
        /// <para type="description">
        /// The name of the S3 bucket where this command uploads the artifacts that are referenced in your template.
        /// If not specified, then the oversize template bucket will be used.
        /// </para>
        /// </summary>
        /// <value>
        /// The s3 bucket.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Bucket", "BucketName")]
        public string S3Bucket { get; set; }

        /// <summary>
        /// Gets or sets the s3 prefix.
        /// <para type="description">
        /// A prefix name that the command adds to the artifacts' name when it uploads them to the S3 bucket. The prefix name is a path name (folder name) for the S3 bucket.
        /// </para>
        /// </summary>
        /// <value>
        /// The s3 prefix.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Prefix", "KeyPrefix")]
        public string S3Prefix { get; set; }

        /// <summary>
        /// Gets or sets the template file.
        /// <para type="description">
        /// The path where your AWS CloudFormation template is located.
        /// </para>
        /// </summary>
        /// <value>
        /// The template file.
        /// </value>
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 0, Mandatory = true)]
        public string TemplateFile
        {
            get => this.templateFile;
            set => this.templateFile = this.ResolvePath(value);
        }

        /// <summary>
        /// Gets or sets the instance of PowerShell implementation of path resolver using provider intrinsic
        /// </summary>
        internal IPathResolver PathResolver { get; set; }

        /// <summary>
        /// Gets or sets the bucket operations instance for S3 uploads
        /// </summary>
        internal IPSS3Util S3 { get; set; }

        /// <summary>
        /// Processes template and nested templates recursively, leaf first
        /// </summary>
        /// <param name="templatePath">The template path.</param>
        /// <param name="workingDirectory">Working directory for files to upload.</param>
        /// <returns>The path to the processed template.</returns>
        internal async Task<string> ProcessTemplate(string templatePath, string workingDirectory)
        {
            var outputTemplatePath = templatePath;
            var parser = TemplateParser.Create(File.ReadAllText(templatePath));
            var resources = parser.GetResources().ToList();
            var templateModified = false;

            // Process nested stacks first
            foreach (var nestedStack in resources.Where(r => r.ResourceType == PackagerUtils.CloudFormationStack))
            {
                templateModified |= await this.ProcessNestedStack(nestedStack, templatePath, workingDirectory);
            }

            // Process remaining resources
            foreach (var resource in resources.Where(
                r => r.ResourceType != PackagerUtils.CloudFormationStack && PackagerUtils.PackagedResources.ContainsKey(r.ResourceType)))
            {
                templateModified |= await this.ProcessResource(resource, templatePath, workingDirectory);
            }

            if (templateModified)
            {
                // Generate a filename and save.
                // The caller will upload the template to S3
                outputTemplatePath = Path.Combine(workingDirectory, Path.GetFileName(templatePath));

                parser.Save(outputTemplatePath);
            }

            return outputTemplatePath;
        }

        /// <summary>
        /// Processes the record.
        /// </summary>
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            this.PathResolver = new PSPathResolver(this.SessionState);

            var context = this.CreateCloudFormationContext();
            var clientFactory = new PSAwsClientFactory(
                this.CreateClient(this._CurrentCredentials, this._RegionEndpoint),
                context);
            this.S3 = new S3Util(clientFactory, context, this.TemplateFile, this.S3Bucket);

            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(workingDirectory);

                try
                {
                    var processedTemplatePath = this.ProcessTemplate(this.TemplateFile, workingDirectory).Result;

                    // The base stack template was changed
                    if (this.OutputTemplateFile != null)
                    {
                        File.Copy(processedTemplatePath, this.OutputTemplateFile, true);
                    }
                    else
                    {
                        this.WriteObject(File.ReadAllText(processedTemplatePath));
                    }

                    if (processedTemplatePath != this.TemplateFile)
                    {
                        File.Delete(processedTemplatePath);
                    }
                }
                catch (Exception ex)
                {
                    this.ThrowExecutionError(ex.Message, this, ex);
                }
            }
            finally
            {
                if (Directory.Exists(workingDirectory))
                {
                    try
                    {
                        Directory.Delete(workingDirectory, true);
                    }
                    catch (Exception e)
                    {
                        this.Logger?.LogWarning($"Cannot remove workspace directory '{workingDirectory}': {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Processes a nested stack.
        /// </summary>
        /// <param name="nestedStackResource">The nested stack.</param>
        /// <param name="templatePath">The template path.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns><c>true</c> if the containing template should be modified (to point to S3); else <c>false</c></returns>
        /// <exception cref="FileNotFoundException">Nested stack resource '{nestedStackResource.LogicalName}': TemplateURL cannot refer to a directory.</exception>
        private async Task<bool> ProcessNestedStack(
            TemplateResource nestedStackResource,
            string templatePath,
            string workingDirectory)
        {
            var nestedTemplateLocation = new PackagerUtils(this.PathResolver).ResolveFileSystemResource(
                templatePath,
                nestedStackResource.GetResourcePropertyValue("TemplateURL"));

            switch (nestedTemplateLocation)
            {
                case null:

                    // Value of TemplateURL is already a URL.
                    return false;

                case FileInfo fi:

                    // Referenced nested template is in the filesystem, therefore it must be uploaded
                    // whether or not it was itself modified
                    var processedTemplate = new FileInfo(await this.ProcessTemplate(fi.FullName, workingDirectory));
                    var templateHash = processedTemplate.MD5();

                    // Output intermediate templates to console if -Debug
                    this.Logger.LogDebug($"Processed template '{fi.FullName}', Hash: {templateHash}");
                    this.Logger.LogDebug("\n\n{0}", File.ReadAllText(processedTemplate.FullName));

                    var resourceToUpload = new ResourceUploadSettings
                                               {
                                                   File = processedTemplate,
                                                   Hash = templateHash,
                                                   KeyPrefix = this.S3Prefix,
                                                   Metadata = this.Metadata
                                               };

                    if (await this.S3.ObjectChangedAsync(resourceToUpload))
                    {
                        await this.S3.UploadResourceToS3Async(resourceToUpload);
                    }

                    // Update resource to point to uploaded template
                    nestedStackResource.UpdateResourceProperty("TemplateURL", resourceToUpload.S3Artifact.Url);

                    break;

                default:

                    // The path references a directory, which is illegal in this context.
                    throw new FileNotFoundException(
                        $"Nested stack resource '{nestedStackResource.LogicalName}': TemplateURL cannot refer to a directory.");
            }

            return true;
        }

        /// <summary>
        /// Processes resources that can point to artifacts in S3.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="templatePath">The template path.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns><c>true</c> if the containing template should be modified (to point to S3); else <c>false</c></returns>
        /// <exception cref="InvalidDataException">Unsupported derivative of FileSystemInfo</exception>
        /// <exception cref="MissingMethodException">Missing constructor for the replacement template artifact.</exception>
        private async Task<bool> ProcessResource(
            TemplateResource resource,
            string templatePath,
            string workingDirectory)
        {
            var utils = new PackagerUtils(this.PathResolver);
            var templateModified = false;

            foreach (var propertyToCheck in PackagerUtils.PackagedResources[resource.ResourceType])
            {
                string resourceFile;

                try
                {
                    resourceFile = resource.GetResourcePropertyValue(propertyToCheck.PropertyPath);
                }
                catch (FormatException)
                {
                    if (!propertyToCheck.Required)
                    {
                        // Property is missing, but CloudFormation does not require it.
                        continue;
                    }

                    throw;
                }

                if (resourceFile == null)
                {
                    // Property was not found, or was not a value type.
                    continue;
                }

                var fsi = utils.ResolveFileSystemResource(templatePath, resourceFile);

                if (fsi == null)
                {
                    // Property value did not resolve to a path in the file system
                    continue;
                }

                ResourceUploadSettings resourceToUpload;

                templateModified = true;

                // If this is a lambda, check for a dependency file in the same directory
                if (resource.ResourceType == "AWS::Lambda::Function"
                    || resource.ResourceType == "AWS::Serverless::Function")
                {
                    var handler = resource.GetResourcePropertyValue("Runtime");

                    using (var packager = LambdaPackager.CreatePackager(fsi, handler, this.S3, this.Logger))
                    {
                        resourceToUpload = await packager.Package(workingDirectory);
                    }
                }
                else
                {
                    switch (fsi)
                    {
                        case FileInfo fi:

                            // Property value points to a file
                            resourceToUpload = await ArtifactPackager.PackageFile(
                                                   fi,
                                                   workingDirectory,
                                                   propertyToCheck.Zip,
                                                   this.S3,
                                                   this.Logger);
                            break;

                        case DirectoryInfo di:

                            // Property value points to a directory, which must always be zipped.
                            resourceToUpload = await ArtifactPackager.PackageDirectory(
                                                   di,
                                                   workingDirectory,
                                                   this.S3,
                                                   this.Logger);
                            break;

                        default:

                            // Should never get here, but shuts up a bunch of compiler/R# warnings
                            throw new InvalidDataException(
                                $"Unsupported derivative of FileSystemInfo: {fsi.GetType().FullName}");
                    }
                }

                if (!resourceToUpload.HashMatch)
                {
                    resourceToUpload.KeyPrefix = this.S3Prefix;
                    resourceToUpload.Metadata = this.Metadata;

                    await this.S3.UploadResourceToS3Async(resourceToUpload);
                }

                if (propertyToCheck.ReplacementType == typeof(string))
                {
                    // Insert the URI directly
                    resource.UpdateResourceProperty(propertyToCheck.PropertyPath, resourceToUpload.S3Artifact.Url);
                }
                else
                {
                    // Create an instance of the new mapping
                    // ReSharper disable once StyleCop.SA1305
                    var s3Location = propertyToCheck.ReplacementType.GetConstructor(new[] { typeof(S3Artifact) })
                        ?.Invoke(new object[] { resourceToUpload.S3Artifact });

                    if (s3Location == null)
                    {
                        throw new MissingMethodException(propertyToCheck.ReplacementType.FullName, ".ctor(S3Artifact)");
                    }

                    // and set on the resource property
                    resource.UpdateResourceProperty(propertyToCheck.PropertyPath, s3Location);
                }
            }

            return templateModified;
        }

        // ReSharper restore UnusedAutoPropertyAccessor.Local
        // ReSharper restore MemberCanBePrivate.Local
    }
}