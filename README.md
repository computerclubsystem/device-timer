# Device Timer

## Build
After installing .NET 8 runtime ([https://learn.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#manual-install]), navigate to the root folder where the `DeviceTimer.sln` file is and execute:
```bash
dotnet build
```

## Start
- First you need to copy the `.crt` and .`key` certificate files created for the device to `/etc/ssl/certs/device-timer.crt` and `/etc/ssl/certs/device-timer.key`
- After the application is built, navigate to the output folder of `DeviceTimerApp` project - should be `DeviceTimerApp/bin/Debug/net8.0` and execute (change the parameter values to point to the real environment):
```bash
./DeviceTimerApp --web-server-url wss://192.168.1.9:65445 --server-certificate-thumbprint 02C347A57731C65931D30D3D93298BDC610488A8
```
Or you can set the following environment variables in `.bashrc` file by adding these lines at the end:
```bash
export DEVICE_TIMER_WEB_SOCKET_URL=wss://192.168.1.9:65445
export DEVICE_TIMER_SERVER_CERTIFICATE_THUMBPRINT=02C347A57731C65931D30D3D93298BDC610488A8
```
And then execute the app without command line parameters:
```bash
./DeviceTimerApp
```
If command line parameter is provided, it will override the environment variable value.