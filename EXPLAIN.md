# VIT-AP Windows Authenticator - Complete Setup Guide

## 🎯 Quick Start

This is a professional Windows desktop application for VIT-AP WiFi authentication.

### What You Need

1. **Visual Studio 2022 Community** (Free)
   - Download: https://visualstudio.microsoft.com/downloads/
   - Select: `.NET desktop development` workload
   - Include: C# and WPF components

2. **.NET 8.0 SDK or later**
   - Included with Visual Studio
   - Or download separately: https://dotnet.microsoft.com/download

3. **Windows 10/11 PC**
   - Connected to VIT-AP WiFi

### How to Run

#### Option 1: Run from Visual Studio (Development)

1. Clone the repository:
   ```bash
   git clone https://github.com/YOUR-USERNAME/vitap-windows-authenticator.git
   cd vitap-windows-authenticator
   ```

2. Open `VitapAuthenticator.sln` in Visual Studio

3. Press `F5` to run

#### Option 2: Build Standalone EXE

1. Open Command Prompt in the project folder

2. Run:
   ```bash
   dotnet build -c Release
   ```

3. Find EXE at: `bin/Release/net8.0-windows/VitapAuthenticator.exe`

#### Option 3: Publish Application

1. In Visual Studio: Build > Publish VitapAuthenticator
2. Or Command Prompt:
   ```bash
   dotnet publish -c Release
   ```

3. Find files in: `bin/Release/net8.0-windows/publish/`

## 📦 Project Structure

```
vitap-windows-authenticator/
├── VitapAuthenticator.csproj       # Project file
├── App.xaml                        # Application setup
├── App.xaml.cs                     # Code-behind
├── MainWindow.xaml                 # GUI Layout
├── MainWindow.xaml.cs              # GUI Logic  
├── VitapClient.cs                  # Core Auth Logic
├── SessionManager.cs               # Session Management
├── Properties/
│   └── launchSettings.json         # Debug settings
└── bin/
    └── Release/
        └── VitapAuthenticator.exe  # Final executable
```

## 🚀 Features

✅ Modern WPF GUI (Windows Native)  
✅ Firewall Bypass (Cisco Meraki)  
✅ Session Management  
✅ Keep-Alive Mechanism  
✅ Concurrent User Bypass  
✅ Detailed Logging  
✅ Single Executable (No Dependencies)  

## ⚙️ System Requirements

- Windows 10 or 11
- .NET 8.0 Runtime (included in app)
- Connected to VIT-AP WiFi
- Administrator privileges (optional, for some features)

## 🛠️ Development

### Building from Scratch

1. Create new WPF Project in Visual Studio
2. Target: `.NET 8.0-windows`
3. Install NuGet packages:
   - RestSharp
   - Newtonsoft.Json
4. Copy the provided code files
5. Build and run

### Debugging

1. Press F5 in Visual Studio
2. Set breakpoints in C# code
3. Watch the Debug Output window
4. Check log files in: `C:\Users\YourUsername\AppData\Local\VitAP\`

## 📝 Configuration

Edit `App.config` to customize:

```xml
<configuration>
  <appSettings>
    <add key="PortalURL" value="http://172.18.10.10:8090" />
    <add key="LoginEndpoint" value="http://172.18.10.10:8090/guestportal/gateway" />
    <add key="KeepAliveInterval" value="600" />
  </appSettings>
</configuration>
```

## 🔧 Troubleshooting

### "Cannot connect to portal"
- Verify you're connected to VIT-AP WiFi
- Check firewall (this app should work through firewall)
- Verify 172.18.10.10 is reachable: `ping 172.18.10.10`

### "Application won't start"
- Ensure .NET 8.0 Runtime is installed
- Run as Administrator
- Check Event Viewer for errors

### "Authentication failed"
- Verify username/password are correct
- Check logs in AppData folder
- Try clearing stored sessions

## 📦 Distribution

### For End Users

Publish the application:

```bash
dotnet publish -c Release -p:DebugType=embedded
```

Zip the `publish` folder and distribute. Users can run `VitapAuthenticator.exe` directly.

### For Deployment

Create Windows Installer (.MSI):

1. Install `WiX Toolset`
2. Create `.wxs` file with installation configuration
3. Build with `candle.exe` and `light.exe`
4. Distribute `.msi` file

## 📄 License

Based on vitap-mate by synaptic-gg.  
Educational use only.

## 👨‍💻 Contributing

Feel free to fork, modify, and improve!

## ⚠️ Important Notes

- Only use on VIT-AP network
- Comply with VIT-AP Acceptable Use Policy
- Never share credentials
- Use responsibly

---

**For detailed code and project files, check the repository structure.**
