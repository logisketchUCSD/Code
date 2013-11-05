# -------------------------------------------------------------------------- #
################################# SymbolRec ##################################
# -------------------------------------------------------------------------- #

SymbolRec is a utility class used in many different parts of the code. The
most common use is of the SymbolRec.Image class to represent bitmap images;
separate documentation for all of the SymbolRec image processing tools can
be found in the Image\ subdirectory of this directory

SymbolRec also contains other projects. It contains CreateSymbol and
ImageMatch. CreateSymbol creates symbols with context and without context.
ImageMatch is the general framework for making definition images, creating
training, and training (used for Svm-based recognizers, so far as I can
tell)

Issues:

WindewedImage and DefinitionImage are a little "dirty", could be cleaned up.
No imediate reasons to do that except for modularity.

Rotations currently rely on the weighted center of the strokes.  If the
symbol is not similarly stroked, then recognition may be off.
