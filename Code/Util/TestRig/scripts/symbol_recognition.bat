:: 
:: Tests pure symbol recognition
::
FOR %%A IN (1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 22 23 26) DO  ..\bin\Debug\TestRig.exe -s y [y -pure -norefine] -nopause -d "..\..\..\..\Data\ConvertHimetricData\pixels\UCR Gate Study Data\Separated by Users\User %%A\Tablet Data" -contains .xml
