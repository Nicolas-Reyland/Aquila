# Aquila
You can find an Aquila interpreter here. It is still in development. Come back in June 2021 to have a final product!
If you reallt want to test it, look at the Parser.cs file.

To compile the code:

On Windows (never tested):
```
csc -target:exe -out:program.exe *.cs
```
On Linux:
```
mcs -target:exe -out:program.exe *.cs
```

To get the interactive mode, start the compiled program with "interactive" as argument.

On Windows:
```
program.exe interactive
```

On Linux:
```
mono program.exe interactive
```

The documentation is unfinished, but you can stil peak into it :^) !

Credits:
 - Nicolas Reyland
 - Daryl Djelou
