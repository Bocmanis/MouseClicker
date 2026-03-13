# Build Instructions

This is a .NET Framework WPF project. Use MSBuild (not `dotnet build`).

## Build Command

```
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "D:\Personal\Projekti\Coding\MouseClicker\BetterClicker.csproj" -p:Configuration=Release -verbosity:minimal
```

## Output

Executable: `bin\Release\BetterClicker.exe`

## Run

```
start "" "D:\Personal\Projekti\Coding\MouseClicker\bin\Debug\BetterClicker.exe"
```

Build with `-p:Configuration=Debug` and launch from `bin\Debug\BetterClicker.exe`.
