# Aquila
Aquila is a programming language designed to make algorithms easier to understand, whether by humans or by computers. The [Code Vultus project](https://github.com/Nicolas-Reyland/Code-Vultus) is a good example use-case of the Aquila programming language.

You can find an interpreter here. It is still in development, altough in a state of pause for now.
The documentation is unfinished, but you can stil peak into it :^) !
If you want to develop in Aquila, there are two syntax-highlighting extensions out there: [aquila-for-atom](https://github.com/Nicolas-Reyland/aquila-for-atom) for the Atom editor and [aquila-for-vscode](https://github.com/Nicolas-Reyland/aquila-for-vscode) for the VS Code Editor. You can install them both from their editor's package manager or using those links:
 * https://atom.io/packages/aquila-for-atom
 * https://marketplace.visualstudio.com/items?itemName=NicolasReyland.aquila-for-vscode


To compile the Interpreter:

On Windows (never tested):
```
cd src
dotnet build
```
or run the *windows-build.bat* script


On Linux:
```
mcs -target:exe -out:interpreter.exe src/Parser/*.cs
```
or run the *linux-build.sh* script


You can run an Aquila code file by giving its path as argument:

On Windows:
```
interactive.exe "C:\Path\To\File.aq"
```

On Linux:
```
mono interactive.exe "/path/to/file.aq"
```

It will print the return value to the stdout (if there is none, prints "none").

To get the interactive mode, start the compiled program with "interactive" as argument.

On Windows:
```
interactive.exe interactive
```

On Linux:
```
mono interactive.exe interactive
```

You have to have **mono** and **dotnet** (core v3.1 is fine) installed.
