using System.Device.Gpio;

namespace DeviceTimer.Tm1637Ported;

public class Tm1637PortedDisplayConfig
{
    public GpioController GpioController { get; set; }
    public int ClockPin { get; set; }
    public int DataPin { get; set; }
    public TimeSpan ClockWidth { get; set; }
}
