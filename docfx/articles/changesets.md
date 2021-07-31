# Changesets

PSCloudFormation supports extracting changeset detail to a file, the pipeline, and potentially viewing in a browser. It also suppports nested changeset creation (new as of re:Invent 2020).

* [Cmdlets for Changeset Creation](#Cmdlets-for-Changeset-Creation)
* [Working with Changesets](#Working-with-Changesets)
    * [Nested Changests](#Nested-Changeset)
    * [Pipeline Output](#Pipeline-Output)
    * [Exporting Change Detail to File](#Exporting-Change-Detail-to-File)
    * [Viewing in a Browser](#Viewing-in-a-Browser)
* [Caveats](#Caveats)
    * [Browser View](#Browser-View)
    * [Nested Changeset Creation](#Nested-Changeset-Creation)

## Cmdlets for Changeset Creation

* [New-PSCFNChangeset](xref:Update-PSCFNChangeset) - Use this to create a cchangeset only for review.
* [Update-PSCFNStack](xref:Update-PSCFNStack) - A changeset is automatically created and presented fo review unless the `-Force` switch is present, in which case it is executed immediately.

## Working with Changesets

### Changeset View

When a changeset is created, it is presented to the console for review. This contains the usual columns as displayed in the changeset viewer on the AWS console, with the addition of an extra column that describes the scope of the resource change for a `Modify` action. This is a comma separated list of single characters with the following meaning

| Scope | Meaning                                           |
|-------|---------------------------------------------------|
| C     | `CreationPolicy` is being changed                 |
| M     | `Metadata` is being changed                       |
| P     | One or more resource properties are being changed |
| T     | One or more tags are being changed                |
| U     | `UpdatePolicy` is being changed                   |

### Nested Changesets

If the switch `-IncludeNestedStacks` is passed to either [New-PSCFNChangeset](xref:Update-PSCFNChangeset) or [Update-PSCFNStack](xref:Update-PSCFNStack), and the given stack contains `AWS::CloudFomation::Stack` resources, changesets will be computed for these stacks also. Changes for all the stacks in the nest will be displayed.

Please be aware of the [caveat](#Nested-Changeset-Creation) below!

### Pipeline Output

The [New-PSCFNChangeset](xref:Update-PSCFNChangeset) cmdlet will output raw JSON to the pipeline if neither `-ChangesetDetail` not `-ShowInBrowser` arguments are present. The changest may be further processed by piping this output through `ConvertFrom-Json` to get a PowerShell object representation of the changeset detail.

The JSON object is the same as that which you can see in the `JSON Changes` tab of the CloudFormation Console, with the small exception that the property names in the JSON begin with an uppercase letter.


### Exporting Change Detail to File

If a file path is given to the `-ChangesetDetail` parameter of [New-PSCFNChangeset](xref:Update-PSCFNChangeset) or [Update-PSCFNStack](xref:Update-PSCFNStack), then the JSON Changes document is written to this file.

### Viewing in a Browser

Where possible, detailed changeset information may be viewed in a browser. PSCloudFormation attempts to detect whether a browser should be available. It does this by examining the operating system version and from this working out whether a GUI is present. This is done like so:

* **Windows**. Read the registry value `InstallationType` at `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion`. If the result is `Client` or `Server` then a GUI is present. Use Shell Execute to invoke the generated HTML document directly resulting in it being opened by the default browser.
* **Linux**. Check for environment variable `XDG_CURRENT_DESKTOP`. If present, pass the generated HTML document to `xdg-open`.
* **MacOS**. On Macs a GUI is assumed always present. Pass the generated HTML document to `open`.

When a GUI is detected, [Update-PSCFNStack](xref:Update-PSCFNStack) provides an additional option `View in Browser` when it asks whether to apply the generated changeset. When `-Force` parameter is set, this is skipped.

[New-PSCFNChangeset](xref:Update-PSCFNChangeset) will perform the above by addition of `-ShowInBrowser` switch argument.

The HTML document contains a formatted view of each change along with the detail provided in the `JSON Changes` view in the CloudFormation console. A graph of relationships between modified resouces is also provided. This has the following key

| Icon                  | Meaning                                                                     |
|-----------------------|-----------------------------------------------------------------------------|
| Solid box, green font | New resource.                                                               |
| Dashed box, red font  | Resource is being deleted.                                                  |
| Box, green fill       | Resource is being modified, without replacement.                            |
| Box, amber fill       | Resource is being modified, conditional replacement.                        |
| Box, red fill         | Resource is being modified, and will be REPLACED.                           |
| Diamond               | A parameter. Text is the parameter's name.                                  |
| Ellipse               | Direct modification, e.g. user changed a property directly in the template. |
| Connectors            | Label shows property being changed on target resource.                      |

Additionally, only relevant information from the JSON changes are diplayed. Properties that are `null` in the JSON change are hidden. Change detail is only shown for `Modify` changes, as there isn't any relevant detail for `Add` or `Remove`. Full detail is intially hidden, however a button will show to un-hide this detail where it exists.

## Caveats

### Browser View

The HTML view relies on [jQuery](https://jquery.com) and [Bootstrap](https://getbootstrap.com) for its layout. If you are executing thes cmdlets from a restricted network with no Internet access, these components will not download from their respective CDNs and thus the changeset display may be at least unhelpful and at worst unintelligeable. In such situations, use the `-ChangesetDetail` parameter to write JSON changes to a file and examine the file content.

### Nested Changeset Creation

There is a [server-side bug in this which I reported](https://github.com/fireflycons/aws-nested-changeset-bug) back in Dec 2020 and has been acknowledged by AWS. If a nested stack has no changes, then any outputs of this nested stack are incorrectly deemed to have *all* been changed. Any other stacks in the nest which use these outputs as parameters then incorrectly show resource changes caused by these paameters.

This issue renders the nested changeset feature fairly useless at present. Since the bug is server-side (i.e. within AWS itself), as soon as AWS roll out the fix, then PSCloudFormation will work with this feature without the need for a new release.

AWS have still not fixed this as of July 2021!

