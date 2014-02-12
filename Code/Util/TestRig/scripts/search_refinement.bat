:: 
:: Tests refinement by comparing several pipelines.
::
FOR %%A IN (1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 22 23) DO  ..\bin\Debug\TestRig.exe -s p [p cls grp rec con "|" cls grp rec con ref_search "|" cls grp rec con ref_ctx] -n 50 -nopause -d "..\..\..\..\Data\DrawingStyleStudyData\User%%A\Final Circuits" -contains .xml
::..\bin\Debug\TestRig.exe -s p [p cls grp rec con "|" cls grp rec con ref_search "|" cls grp rec con ref_ctx ref_shed ref_steal] -n 10 -d "..\..\..\..\Data\ConvertHimetricData\pixels\UCR Gate Study Data" -contains .xml -contains COPY
::..\bin\Debug\TestRig.exe -s p [p cls grp rec con "|" cls grp rec con ref_ctx] -d "..\..\..\..\Data\DrawingStyleStudyData\All Complete Sketches" -contains .xml
