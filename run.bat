@echo off
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
echo [1/3] Compiling...
"%CSC%" /nologo /optimize+ /unsafe /platform:x64 /out:cachebench.exe cachebench.cs > compile_log.txt 2>&1
if errorlevel 1 (
  echo COMPILE FAILED, see compile_log.txt
  type compile_log.txt
  pause
  exit /b 1
)
echo [2/3] Collecting system information...
powershell -NoProfile -Command "Get-CimInstance Win32_Processor | Format-List Name,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed,L2CacheSize,L3CacheSize; Get-CimInstance Win32_CacheMemory | Format-Table Purpose,InstalledSize -AutoSize; Get-CimInstance Win32_ComputerSystem | Format-List Manufacturer,Model,TotalPhysicalMemory; Get-CimInstance Win32_PhysicalMemory | Format-Table Manufacturer,PartNumber,Speed,ConfiguredClockSpeed,Capacity -AutoSize; Get-CimInstance Win32_OperatingSystem | Format-List Caption,Version,OSArchitecture; Get-CimInstance Win32_Battery | Format-List BatteryStatus,EstimatedChargeRemaining; powercfg /getactivescheme; Get-ChildItem $env:WINDIR\Microsoft.NET\Framework64 -Directory | Select-Object Name" > sysinfo.txt 2>&1
echo [3/3] Running benchmark (several minutes). Please do not use the computer.
cachebench.exe > results1.csv 2> progress1.txt
cachebench.exe > results2.csv 2> progress2.txt
echo finished > done.txt
echo ALL DONE
