using Iot.Device.CpuTemperature;
using Iot.Device.Button;

namespace DeviceTimer.Sensors;

public class Temperatures
{
    private readonly CpuTemperature cpuTemperature = new();

    public double GetCpuTemperature()
    {
        try
        {
            return Math.Round(cpuTemperature.Temperature.DegreesCelsius, 1);
        }
        catch
        {
            return 0;
        }
    }
}
