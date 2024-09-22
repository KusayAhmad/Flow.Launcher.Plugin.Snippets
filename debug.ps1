dotnet publish Flow.Launcher.Plugin.Snippets -c Release -r win-x64 --no-self-contained
Compress-Archive -LiteralPath Flow.Launcher.Plugin.Snippets/bin/Release/win-x64/publish -DestinationPath Flow.Launcher.Plugin.Snippets/bin/Snippets.zip -Force

try {
    taskkill /F /IM Flow.Launcher.exe
}
catch {

}
Start-Sleep -Seconds 1
try {
    taskkill /F /IM Flow.Launcher.exe
}
catch {

}
try {
    Remove-Item $env:APPDATA\FlowLauncher\Plugins\Snippets-1.0.0\* -recurse
}
catch {

}
Copy-Item -Path Flow.Launcher.Plugin.Snippets\bin\Release\win-x64\publish\* -Destination $env:APPDATA\FlowLauncher\Plugins\Snippets-1.0.0\ -recurse
C:\Users\kerryzhang\AppData\Local\FlowLauncher\Flow.Launcher.exe