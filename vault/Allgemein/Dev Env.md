
## Quarkus (CLI)



`winget install EclipseAdoptium.Temurin.17.JDK`

`$jdkPath = "C:\Program Files\Eclipse Adoptium\jdk-17.0.17.10-hotspot\bin"`

`[Environment]::SetEnvironmentVariable("Path", $env:Path + ";" + $jdkPath, "User")`

`$env:Path += ";C:\Program Files\Eclipse Adoptium\jdk-17.0.17.10-hotspot\bin"`

`java -version`


`choco install quarkus`

`quarkus --version`


## IntelliJ Plugins

- GBrowser
- Junie AI
- Quarkus