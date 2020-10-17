---
uid: lambda_packager
title: Lambda Packager
---
# Packaging Lambdas

A feature that is glaringly absent from `aws cloudformation package` is the ability to specify dependent modules that should be included in the lambda zip package, therefore I have come up with my own implementation of this. It works by providing a file in the same directory as the script that contains the lambda handler function that lists all the dependent modules that should be packaged. This file is called `lambda-dependencies` and can have either a `.yaml` or `.json` extension.

Addtionally as part of the lambda packaging process, PSCloudFormation will, where possible, validate the lambda handler defined by the function resource. The lambda code file indicated by the handler is examined to check for the presence of a method within that has the correct name (defined by the Handler property) and signature for a handler function. This is especially useful when creating Custom Resouce functions, as a typo in the handler name can cause the deployment to lock up completely.

## Dependency Specification

The schema for this file is that it is an array of dependency objects, where a dependency object has the following fields:

* `Location` - A path or an environment variable containing a path to the directory containing modules. The path may resolve to an absolute location, or a location relative to the location of the dependency file. For Python lambdas, the `VIRTUAL_ENV` environment variable is especially useful here, provided that you create the package from within your virtual env. To specify an environment variable as a location, precede the variable name with `$`
* `Libraries` - A list of module names to take from `Location`, i.e. subdirectories of `Location`

Note that the dependency system does not currently examine modules listed in `lambda-dependencies` for any sub-dependencies. IT is up to you to identify the full dependency tree of any given module and ensure they ae all listed in the dependencies file.

## Supported Runtimes

Currently the following lambda runtimes are supported, which are basically the script runtimes. Compiled runtimes (Java, .NET and Go) would generally have a build process which can be made to target a zip file which would contain a full lambda package, and that zip file would be referred to in the CloudFormation template.

* `python` - all versions
* `nodejs` - all versions
* `ruby` - all versions

### Python

The easiest way to package Python dependencies is to build your Python lambda in a [virtual env](https://docs.python.org/3/library/venv.html) and then run PSCloudFormation cmdlets from within the virtual env, using the `VIRTUAL_ENV` environment variable in your `lambda-dependencies` file as the location for package dependencies. This environment variable is created when you activate your virtualenv.

```yaml
- Location: "$VIRTUAL_ENV"
  Libraries:
  - yaml
  - PIL
  - six
- Location: /some/other/location
  Libraries:
  - other_library
```

```
lambda-project
├──template.yaml
├──lambda-function
│  └── index.js
└── venv
    └── lib
        └── site-packages
            ├── yaml
            ├── PIL
            ├── six.py
```

### NodeJS

Given a directory structure for a lambda project as below, the easiest way to package the lambda with dependencies is to specify the lambda function's directory in the CloudFormation template. Packager will then recursively package all the included node modules, e.g.

```yaml
  LambdaFunction:
    Type: AWS::Lambda::Function
    Properties:
      Code: lambda-function
```

You can also provide a `lambda-dependencies` file in the same directory as `index.js` to pull additional modules from other directories outside of the lambda project.

```
lambda-project
├──template.yaml
└──lambda-function
   ├── index.js
   └── node_modules
       ├── async
       ├── async-listener
       ├── atomic-batcher
       ├── aws-sdk
       ├── aws-xray-sdk
       ├── aws-xray-sdk-core
```

Note that the entire content of `node_modules` will be included in the zip package which may not be what you want. Not being much of a node developer myself, I am open to suggestions. Raise an issue with ideas (I'm thinking maybe use a `package.json` to express what to include or some such).

### Ruby

Given a directory structure for a lambda project as below, the easiest way to package the lambda with dependencies is to specify the lambda function's directory in the CloudFormation template. Packager will then recursively package all the included Ruby modules, e.g.

```yaml
  LambdaFunction:
    Type: AWS::Lambda::Function
    Properties:
      Code: lambda-function
      Runtime: ruby2.7
```

You can also provide a `lambda-dependencies` file in the same directory as `index.rb` to pull additional modules from other directories outside of the lambda project.

```
lambda-project
├──template.yaml
└──lambda-function
   ├── index.rb
   └── vendor
       └── bundle
           └── ruby
               └── 2.7.0
                   └── cache
                       ├── aws-eventstream-1.0.1.gem
```

Note that the entire content of `vendor` will be included in the zip package which may not be what you want. Not being much of a ruby developer myself, I am open to suggestions. Raise an issue with ideas.

