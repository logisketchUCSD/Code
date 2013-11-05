:: 
:: Tests refinement by comparing several pipelines.
::
::..\bin\Debug\TestRig.exe -s p [p cls grp rec "|" cls grp rec refine_ctx] -d "..\..\..\..\Data\Gate Study Data\AllLabeledSketches" -contains .xml -contains 10
..\bin\Debug\TestRig.exe -s p [p cls grp rec "|" cls grp rec con ref_ctx "|" cls grp rec con ref_cctx] -d "..\..\..\..\Data\Gate Study Data\LabeledSketches\TabletPC\HMC" -contains .xml -contains 10
