::
:: Tests the adaptive image recognizer on all user-study users.
::
FOR %%A IN (1 2 3 4 5 6 7 8 9 10 11 12 13 14 15) DO ..\bin\Debug\TestRig.exe -s i -nopause -d "..\..\..\..\Data\DrawingStyleStudyData\User%%A\Final Circuits" -contains .xml
