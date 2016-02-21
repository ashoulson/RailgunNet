@echo OFF 
call "C:\Program Files (x86)\Microsoft Visual Studio 11.0\VC\vcvarsall.bat" x86
echo Starting Build for all Projects with proposed changes
echo .  
devenv "Example.sln" /build "Client Debug"
echo . 
echo "All builds completed." 
pause