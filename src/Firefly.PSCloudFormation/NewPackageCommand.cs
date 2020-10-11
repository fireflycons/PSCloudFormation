namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Management.Automation;

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
            set => this.outputTemplateFile = this.PathResolver.ResolvePath(value);
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
            set => this.templateFile = this.PathResolver.ResolvePath(value);
        }

        /// <summary>
        /// Gets or sets the bucket operations instance for S3 uploads
        /// </summary>
        internal IPSS3Util S3 { get; set; }

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
            this.S3 = new S3Util(
                clientFactory,
                context,
                this.TemplateFile,
                this.S3Bucket,
                this.S3Prefix,
                this.Metadata);

            try
            {
                using (var workingDirectory = new WorkingDirectory(this.Logger))
                {
                    var packager = new PackagerUtils(this.PathResolver, this.Logger, this.S3);
                    var processedTemplatePath = packager.ProcessTemplate(this.TemplateFile, workingDirectory).Result;

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
            }
            catch (Exception ex)
            {
                this.ThrowExecutionError(ex.Message, this, ex);
            }
        }
    }
}