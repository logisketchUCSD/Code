:: 
:: Tests refinement by comparing several pipelines.
::
..\bin\Debug\TestRig.exe -s y [y -pure   -norefine] -d "..\..\..\..\Data\Gate Study Data\AllLabeledSketches" -contains .xml
..\bin\Debug\TestRig.exe -s y [y -pure   -refine]   -d "..\..\..\..\Data\Gate Study Data\AllLabeledSketches" -contains .xml
..\bin\Debug\TestRig.exe -s y [y -impure -norefine] -d "..\..\..\..\Data\Gate Study Data\AllLabeledSketches" -contains .xml
..\bin\Debug\TestRig.exe -s y [y -impure -refine]   -d "..\..\..\..\Data\Gate Study Data\AllLabeledSketches" -contains .xml
