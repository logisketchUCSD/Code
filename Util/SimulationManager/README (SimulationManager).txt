Simulation Manager
------------------

The Simulation Manager provides the interface for interacting with circuit simulation
in WPFCircuitSimulatorUI. The SimulationManager handles simulation via the input toggles and
truth table by calculating the values of each wire element and updating the display
appropriately. The SimulationManager also handles correction of label text recognition.

Files:
------

CircuitValuePopups.cs - Creates and updates WPF popups near each input/output with the correct
	corresponding value of the circuit. The popups are draggable and inputs are clickable.
CleanCircuit.cs - Creates the cleaned up version of the user's drawn circuit by placing gate
	images and text appropriately and then connecting. Allows mesh highlighting and wire
	highlights for simulation. Inherits from WPF Control Image.
ReplaceNamesDialog.xaml.cs - The WPF Window for correcting text recognition.
SimulationManager.cs - Main framework class.
TruthTableWindow.xaml.cs - The WPF Window for the circuit's truth table. Allows for simulation
	of various input values by hovering over a row or by inputting values by string.