
******************************
*   Recognition Interfaces   *
******************************

These stubs standardize the interfaces provided by all classifiers,
groupers, and symbol recognizers. Classes should use these interfaces
so we can easily add/remove different algorithms later.

For sample usage, see: 
    /Trunk/Code/Util/RecognitionManager/RecognitionManager.cs
    /Trunk/Code/Util/RecognitionManager/RecognitionPipeline.cs

Notable implementations:
    /Trunk/Code/Recognition/StrokeClassifier/StrokeClassifier.cs
    /Trunk/Code/Recognition/StrokeGrouper/StrokeGrouper.cs
    /Trunk/Code/Recognition/Recognizers/UniversalRecognizer.cs
    /Trunk/Code/Recognition/Recognizers/WireRecognizer.cs
    /Trunk/Code/Recognition/Recognizers/TextRecognizer.cs
    /Trunk/Code/Recognition/CombinationRecognizer/ComboRecognizer.cs
