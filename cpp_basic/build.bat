@echo off
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
cd /d "C:\Users\hanyi\claude\cpp_basic"
cl /std:c++17 /O2 /EHsc /W4 main.cpp /Fe:trading.exe
