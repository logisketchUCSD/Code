# -------------------------------------------------------------------------- #
################################ RECOGNITION #################################
# -------------------------------------------------------------------------- #

This folder contains classes used for symbol and sketch recognition. Each
subfolder is summarized below, and may contain additional README.txt files,
if necessary.

# -------------------------------------------------------------------------- #


Aligner: prealigns diagrams, either by brute forcing through 90 degree
     intervals, or by attempting substroke feature matching and smart
     rotation/scaling

CircuitParser: Once a circuit has been recognized, the CircuitParser code puts
     it into a form that allows it to be simulated. Part of the Simulation
     piece of the code.

Featurefy: Find features for strokes, and even for the entire sketch.
     Contains a subdirectory (Featureizer) which can be used to featurefy many
     sketches at once and cache the results

RecognitionInterfaces: Standardized interfaces for recognition phases
     (classification, grouping, and symbol recognition).

Recognizers: Several different recognition algorithms. This folder also
     includes the UniversalRecognizer, which uses several sub-recognizers
     to recognize just about everything.

StrokeClassifier: Classifies substrokes as WIRE, GATE or LABEL based on
     features.

StrokeGrouper: Takes strokes that have been classified by the StrokeClassifier
     and groups them into shapes based on spacial context and shape types. 

SymbolRec: Support library for symbol recognition tasks. Includes the Image
     class, which is used for representing bitmap images in lots of places in
     the code.


---Last Updated 13 June 2011---
