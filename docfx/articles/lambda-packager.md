---
uid: lambda_packager
title: Lambda Packager
---
# Packaging Lambdas

A feature that is glaringly absent from `aws cloudformation package` is the ability to specify dependent modules *outside* of the lambda code directory structure that should be included in the lambda zip package e.g. modules in a python venv, therefore I have come up with my own implementation of this. It works by providing a file in the same directory as the script that contains the lambda handler function that lists all the dependent modules that should be packaged. This file is called `lambda-dependencies` and can be0 either YAML or JSON format.

Addtionally as part of the lambda packaging process, PSCloudFormation will, where possible, validate the lambda handler defined by the function resource. The lambda code file indicated by the handler is examined to check for the presence of a method within that has the correct name (defined by the Handler property) and signature for a handler function. This is especially useful when creating Custom Resource functions, as a typo in the handler name can cause the stack deployment to lock up completely.

## Dependency Specification

The schema for this file is that it is an array of dependency objects, where a dependency object has the following fields:

* `Location` - A path or an environment variable containing a path to the directory containing modules. The path may resolve to an absolute location, or a location relative to the location of the dependency file. For Python lambdas, the `VIRTUAL_ENV` environment variable is especially useful here, provided that you create the package from within your virtual env. To specify an environment variable as a location, precede the variable name with `$`
* `Libraries` - A list of module names to take from `Location`, i.e. subdirectories of, or single script files mwithin `Location`

Note that the dependency system does not currently examine modules listed in `lambda-dependencies` for any sub-dependencies. It is up to you to identify the full dependency tree of any given module and ensure they ae all listed in the dependencies file.

## Supported Runtimes

Currently the following lambda runtimes are supported, which are basically the script runtimes. Compiled runtimes (Java, .NET and Go) would generally have a build process which can be made to target a zip file which would contain a full lambda package, and that zip file would be referred to in the CloudFormation template.

* `python` - all versions supported by AWS
* `nodejs` - all versions supported by AWS
* `ruby` - all versions supported by AWS

Having said this, the PSCloudFormation packager will still correctly apply a local zip file target referenced from a template file. It will, for the above supported runtimes still attempt to validate the handler by looking inside the zip file, however for compiled languages simply point the template at the zip artifact created by the compiled project's build process.

### Python

**Sample directory structure (Windows)**

```
lambda-project
├──template.yaml
├──lambda-function
│  └── index.py
└── venv
    └── lib
        └── site-packages
            ├── yaml
            ├── PIL
            ├── six.py
```

**Sample directory structure (Linux/Max)**

...where `X.Y` is the Python runtime version e.g. `3.6`

**CAVEAT**: Due to the versioned site packages in the venv, you must develop using the same version of Python as the runtime version you intend to deploy your lambda to.

```
lambda-project
├──template.yaml
├──lambda-function
│  └── index.py
└── venv
    └── lib
        └── pythonX.Y
            └── site-packages
                ├── yaml
                ├── PIL
                ├── six.py
```

#### Using PSCloudFormation dependencies

The easiest way to package Python dependencies is to build your Python lambda in a [virtual env](https://docs.python.org/3/library/venv.html) and then run PSCloudFormation cmdlets from within the virtual env, using the `VIRTUAL_ENV` environment variable in your `lambda-dependencies` file as the location for package dependencies. This environment variable is created when you activate your virtualenv.

Note that for this mechanism you must list all dependencies, including dependencies of dependencies. This mechanism does not walk the dependency tree of each package listed in `Libraries`. Note also that `Libraries` refers to files or directories, _not_ package names therefore specifying `PyYAML` will _not_ package any files!

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

#### Using requirements.txt

**Experimental** For this you must have a [virtual env](https://docs.python.org/3/library/venv.html) active in your environment when you run PSCloudFormation cmdlets.

List the lambda's dependencies in a `requirements.txt` file in the root directory of the lambda function code. Packages listed in `requirements.txt` that are present in the AWS execution environment such as `boto3` and its dependencies will be skipped for packaging to reduce the size of the package zip. To see what is being skipped, run with `-Verbose` switch.

For each listed dependency, it is looked up in the virtual env and the content and sub-dependencies are recursively located through each package's dist-info files.

A parser is employed to evaluate [marker expressions](https://www.python.org/dev/peps/pep-0508/#environment-markers) applied to `Requires-Dist:` statements in `METADATA` files. This probably does not support _all_ possible grammar in these logical statements, but the unit tests cover many cases I've found. If a statement fails to parse but is in your opinion syntactically correct or if a sub-dependency is not included that should have been, please raise an issue. As a workaround, fall back to [PSCloudFormation dependencies](#using-pscloudformation-dependencies).

The following assumptions are made:

* Your virtual env contains all the dependencies your lambda requires. The packager will not `pip install` any missing ones.
* Versions of these dependencies are correct for your lambda.
* When evaluating sub-dependencies via package `METADATA` files, marker variables such as `os_name`, `sys_platform` etc. evaluate to values consistent with the lambda execution environment being Amazon Linux 2. `python_version` evaluates to the version targeted by your lambda resource's `Runtime` property. `extra` is always assumed (possibly incorrectly) undefined, thus any `extra == 'package'` will evaulate to `false`.

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

**CAVEAT**: Due to the versioned cache directory within the bundle structure, you need to develop using the same version of Ruby as the runtime version you intend to deploy your lambda to.

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

