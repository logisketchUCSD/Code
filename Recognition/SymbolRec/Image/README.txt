# -------------------------------------------------------------------------- #
############################## SymbolRec.Image ###############################
# -------------------------------------------------------------------------- #

This is a support class used for bitmap image representation in many of the
other classes. The most important class here is Image, which represents an
image in Cartesian coordinates, and allows for cached Hausdorff distances
(which were previously very slow).

The other image classes represent specialized representations of images, and
can probably be safely ignored.

The Distance directory contains wrappers for calculating the distance
between pairs of images.
