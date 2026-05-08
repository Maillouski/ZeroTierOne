# ZeroTier Tray (Windows)

A small WPF system-tray UI for the ZeroTier service. Targets Windows 10/11
**ARM64** and **x64**.

## Features

- Status (node ID, version, online/offline)
- List joined networks (status, name, assigned IPs)
- Join / leave networks
- Copy node ID to clipboard
- Open my.zerotier.com

## Requirements

- The `ZeroTierOneService` Windows service must be installed and running
  (see `windows/README.md`).
- Either run the tray as **Administrator**, or copy
  `C:\ProgramData\ZeroTier\One\authtoken.secret` to
  `%LOCALAPPDATA%\ZeroTier\One\authtoken.secret` (readable by your user).

## Build

```
dotnet build windows/ZeroTierTray/ZeroTierTray.csproj -c Release
```

### Publish self-contained ARM64 (single file)

```
dotnet publish windows/ZeroTierTray/ZeroTierTray.csproj ^
  -c Release -r win-arm64 --self-contained true ^
  -p:PublishSingleFile=true -o publish-arm64
```

The output `publish-arm64\zerotier-tray.exe` runs on Windows ARM64 with
**no .NET runtime required** on the target machine.
