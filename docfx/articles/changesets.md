# Changesets

PSCloudFormation supports extracting changeset detail to a file, the pipeline, and potentially viewing in a browser.

## Pipeline Output

The [New-PSCFNChangeset](xref:Update-PSCFNChangeset) cmdlet will output raw JSON to the pipeline if neither `-ChangesetDetail` not `-ShowInBrowser` arguments are present. The changest may be further processed by piping this output through `ConvertFrom-Json` to get a PowerShell object representation of the changeset detail.

## Exporting Change Detail to File

If a file path is given to the `-ChangesetDetail` parameter of [New-PSCFNChangeset](xref:Update-PSCFNChangeset) or [Update-PSCFNStack](xref:Update-PSCFNStack), then the JSON Changes document is written to this file.

## Viewing in a Browser

Where possible, detailed changeset information may be viewed in a browser. PSCloudFormation attempts to detect whether a browser should be available. It does this by examining the operating system version and from this working out whether a GUI is present. This is done like so:

* **Windows**. Read the registry value `InstallationType` at `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion`. If the result is `Client` or `Server` then a GUI is present. Use Shell Execute to invoke the generated HTML document directly resulting in it being opened by the default browser.
* **Linux**. Check for environment variable `XDG_CURRENT_DESKTOP`. If present, pass the generated HTML document to `xdg-open`.
* **MacOS**. On Macs a GUI is assumed always present. Pass the generated HTML document to `open`.

When a GUI is detected, [Update-PSCFNStack](xref:Update-PSCFNStack) provides an additional option `View in Browser` when it asks whether to apply the generated changeset. When `-Force` parameter is set, this is skipped.

[New-PSCFNChangeset](xref:Update-PSCFNChangeset) will perform the above by addition of `-ShowInBrowser` switch argument.

The HTML document contains a formatted view of each change along with the detail provided in the `JSON Changes` view in the CloudFormation console.

