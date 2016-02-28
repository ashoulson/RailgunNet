set ABS_PATH="C:\Users\Alex\Documents\GitHub\RailgunNet\Example"

echo f | xcopy "..\RailgunNet\bin\Debug\RailgunNet.dll" "Unity\Assets\Assemblies\" /Y
echo f | xcopy "..\RailgunNet\bin\Debug\RailgunNet.pdb" "Unity\Assets\Assemblies\" /Y
"C:\Program Files\Unity\Editor\Data\Mono\lib\mono\2.0\pdb2mdb.exe" "%ABS_PATH%\Unity\Assets\Assemblies\RailgunNet.pdb"
pause