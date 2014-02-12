Display Manager
---------------

The DisplayManager handles display feedback to the user about recognition and the built circuit.
The DisplayManager has three main parts: Color Display, Feedback Alerts, and Label ToolTips. Colors
are updated according to what the system recognizes the given strokes as. Label tooltips also give
the label of the recognized strokes when the user hovers above them. Feedback mechanisms are used to
give feedback to the user about the built circuit to allow the user to correct circuit rather than
recognition errors.

Files:
------

DisplayLabelTool.cs - Handles displaying the tooltips when a user is in the hoverspace above a shape.
DisplayManager.cs - Main framework class.
EndpointHighlightFeedback.cs - Places rectangles at the end of all wires indicating whether they
	are connected to a shape or not. Ideally, these can be moved to allow users to manually connect
	wires to other shapes.
Feedback.cs - Base class for all feedback mechanisms.
MeshHighlightFeedback.cs - Thickens the meshes of the circuit. When the user hovers over a circuit 
	element, all attached meshes highlight. When the user hovers over a mesh, that mesh and all
	attached elements highlight.
ParseErrorFeedback.cs - Highlights strokes in red that led to parse errors when creating the circuit.