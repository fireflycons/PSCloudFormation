# Packaging Lambda Dependencies

A feature that is glaringly absent from `aws cloudformation package` is the ability to specify dependent modules that should be included in the lambda zip package, therefore I have come up with my own implementation of this. It works by providing a file in the same directory as the script that contains the lambda handler function that lists all the dependent modules that should be packaged. This file is called `lambda-dependencies` and can have either a `.yaml` or `.json` extension.

The schema for this file is that it is an array of dependency objects, where a dependency object has the following fields:

* `Location` - A path or an environment variable containing a path to the directory containing modules. The path may resolve to an absolute location, or a location relative to the location of the dependency file. For Python lambdas, the `VIRTUAL_ENV` environment variable is especially useful here, provided that you create the package from within your virtual env. To specify an environment variable as a location, precede the variable name with `$`
* `Libraries` - A list of module names to take from `Location`, i.e. subdirectories of `Location`

## Supported Runtimes

Currently the following lambda runtimes are supported, which are basically the script runtimes. Compiled runtimes (Java, .NET and Go) would generally have a build process which can be made to target a zip file which would contain a full lambda package, and that zip file would be referred to in the CloudFormation template.

* `python` - all versions
* `nodejs` - all versions
* `ruby` - all versions

### Python

The easiest way to package Python dependencies is to build your Python lambda in a [virtual env](https://virtualenv.pypa.io/en/latest/) and then run `New-PSCFNPackage` from within the virtual env, using the `VIRTUAL_ENV` environment variable in your `lambda-dependencies` file as the location for package dependencies.

```yaml
- Location: "$VIRTUAL_ENV"
  Libraries:
  - yaml
  - PIL
- Location: /some/other/location
  Libraries:
  - other_library
```

### NodeJS

Given a directory structure for a lambda project as below, the easiest way to package the lambda with dependencies is to specify the lambda function's directory in the CloudFormation template. Packager will then recursively package all the included node modules, e.g.

```yaml
  LambdaFunction:
    Type: AWS::Lambda::Function
    Properties:
      Code: lambda-function
```

You can also provide a `lambda-dependencies` file in the same directory as `index.js` to pull additional modules from other directories outside of the lambda poject.

```
lambda-project
├──template.yaml
└──lammbda-function
   ├── index.js
   └── node_modules
       ├── async
       ├── async-listener
       ├── atomic-batcher
       ├── aws-sdk
       ├── aws-xray-sdk
       ├── aws-xray-sdk-core
```

### Ruby

Given a directory structure for a lambda project as below, the easiest way to package the lambda with dependencies is to specify the lambda function's directory in the CloudFormation template. Packager will then recursively package all the included Ruby modules, e.g.

```yaml
  LambdaFunction:
    Type: AWS::Lambda::Function
    Properties:
      Code: lambda-function
      Runtime: ruby2.7
```

You can also provide a `lambda-dependencies` file in the same directory as `index.rb` to pull additional modules from other directories outside of the lambda poject.

```
lambda-project
├──template.yaml
└──lammbda-function
   ├── index.rb
   └── vendor
       └── bundle
           └── ruby
               └── 2.7.0
                   └── cache
                       ├── aws-eventstream-1.0.1.gem
```

