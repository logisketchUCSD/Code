CreateSymbol:
-------------

This program is a Console Application.  Run it from the command line with no arguments and the following will appear that tells the user how to run the program:

*** Usage: CreateSymbol.exe (-c | -d directory | -r)
*** Usage: CreateSymbol.exe input1.xml [input2.xml ...]
***
*** -c: convert all files in current directory
*** -d directory: convert all files in the specified directory
*** -r directory: recursively convert files from the specified directory

So, it can be placed into a directory and be run from there with the -c or -r command.  It will create directories in a specific directory structure within the current with the -c command or the corresponding folders to the current file with the -r command.  The results that are put into the folders can be used to create a Definition Image that can be used to train SymbolRec.

1) CreateSymbol.cs: Creates training files for the symbol recognizer.  It can operate on a specified directory, on the current directory, or search recursively through directories from the parent directory to find all of the labeled xml files.  It creates a neighborhood for each of the shapes (by passing in the list of substrokes in the shape), and creates .amat files, which represent bitmap images, in a specific directory structure that can be used by the symbol recognition training program.

2) GatePartsDomain.txt: Domain file for the Labeler for labeling the different parts of each gate (backline, frontarc, etc.).