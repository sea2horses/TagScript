# TagScript

A joke programming language that answers that question everyone has about what would a programming language with HTML syntax would look like.

This is REALLY unfinished! And I decided to make it public just because it's probably one of my best efforts in making an Interpreter

Might finish this someday, but I actually want to place my efforts in making a useful programming language.

Documentation at: https://sea2horses.github.io/TagScript/

Although most features stated in the work docs, this language is unfinished, so **beware**. In like, functionality, nothing bad will happen to your computer.


# Usage

**WARNING, THIS NEEDS YOU TO INSTALL THE DOTNET SDK TO RUN!**

To test if you have dotnet sdk, run the following command:

<code>dotnet</code>

If it DOESN'T output something like:
```
Usage: dotnet [options]
Usage: dotnet [path-to-application]

Options:
  -h|--help         Display help.
  --info            Display .NET information.
  --list-sdks       Display the installed SDKs.
  --list-runtimes   Display the installed runtimes.

path-to-application:
  The path to an application .dll file to execute.
```

You need to install it here: https://dotnet.microsoft.com/en-us/download
and **refresh your terminal** when installed.

*Note: This project was built with the 8.0 SDK, but any newer one should work too.*

## Without Installing

Copy the repository use dotnet sdk to run the following command **while on the project's folder**:

<code>dotnet run --configuration Release --project "TagScript Interpreter" [file.tagx]</code>

You can use this to run the example files `sum.tagx` and `tutor.tagx` without installing the project as a Nu Package.
You can also remove the `--configuration Release` line to run the project in Debug mode, which's output will give a lot more insight into how TagScript actually works.

## Installing Globally (easier)

If you wish to simply type a command like `tsi HelloWorld.tagx`, you can install the project as a package, which will let you run it from anywhere in your computer you'll need to follow these series of commands, **only once**:

<code>dotnet pack -c Release</code>

This will pack the project into a NuGet package in a folder called `nupkg` in the project's folder, to install it **globally** with the alias `tsi` you need to run this next command:

<code>dotnet tool install --global --add-source "./TagScript Interpreter/nupkg" TagScript_Interpreter</code>

This should output something like:
```
You can invoke the tool using the following command: tsi
Tool 'tagscript_interpreter' (version '1.0.0') was successfully installed.
```
And such, now you should be able to run `tsi [file.tagx]`!

To uninstall the tool, run

<code>dotnet tool uninstall --global TagScript_Interpreter</code>