# -------------------------------------------------------------------------- #
################################# FEATUREFY ##################################
# -------------------------------------------------------------------------- #

Many times in the code, we need to use a feature-based approach to examine
strokes, or even the whole sketch. This is especially common in the CRF,
although various Groupers and Recognizers also look at features. Featurefy
is currently morphing into a new way to calculate and optimize that
information. The idea is that when the chain is started (by the user
clicking "Recognize" in a sketch, for example), a FeatureSketch object will
be created from the Sketch. This object will initially just contain a
reference to the original Sketch, and some empty FeatureStroke objects, each
of which contains a reference to a Substroke in the sketch. The key is that
all of these objects have on-demand cached computations for things like Arc
Length, angle traversed, intersections, and distances. If we pass a single
FeatureSketch object between all of our classes (instead of a single Sketch)
object, we only need to calculate all of these computationally-intensive
things once. Additionally, we can precompute these values and serialize
them, for quicker training and testing.

# -------------- FeatureSketch ---------------- #

This class stores multi-stroke features. Simple pass it a ref Sketch in the
constructor, and everything else will be taken care of automatically.
There's... rather a lot of API here, and the intersection stuff could
probably be pruned a bit

# -------------- FeatureStroke ---------------- #

This stores single-stroke features. Unlike FeatureSketch, features here are
actually broken out into subobjects for Curvature, ArcLength, Slope, etc.

# -------------- Featureizer ------------------ #

This console application allows you to pre-calculate a FeatureSketch for a
given sketch (or batch of sketches), and serialize the output to a file. It
is useful for preprocessing training data, especially for the CRF.
