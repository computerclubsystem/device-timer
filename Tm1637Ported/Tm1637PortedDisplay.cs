using System.Device.Gpio;
using System.Globalization;

namespace DeviceTimer.Tm1637Ported;

public class Tm1637PortedDisplay
{
    private readonly Tm1637PortedState _state;

    public Tm1637PortedDisplay(Tm1637PortedDisplayConfig config)
    {
        _state = new Tm1637PortedState
        {
            Config = config,
        };
        SetupPins();
    }

    public void SetBrightness(byte brightness)
    {
        if (brightness < 0)
        {
            brightness = 0;
        }
        if (brightness > 7)
        {
            brightness = 7;
        }
        _state.Brightness = brightness;
        WriteDataCommand();
        WriteDisplayControl();
    }

    public void ShowText(string text)
    {
        byte[] bytes = EncodeString(text);
        WriteSegments(bytes);
    }

    /// <summary>
    /// Shows specified seconds as "hours:minutes" (like "1:23") if seconds is greater than 59 or "seconds" (like "35") if seconds are less than 60
    /// </summary>
    /// <param name="seconds"></param>
    public void ShowSecondsAsTime(int seconds)
    {
        if (seconds == 0)
        {
            ShowText(" 0:00");
        }
        else if (seconds < 60)
        {
            ShowNumber(seconds);
        }
        else if (seconds <= _state.MaxHoursAndMinutesAsSeconds)
        {
            int hours = (int)Math.Floor((decimal)seconds / 3600);
            int minutes = (int)Math.Floor((decimal)seconds % 3600 / 60);
            string text = HoursAndMinutesToText(hours, minutes);
            ShowText(text);
        }
        else
        {
            string text = GetMaxHoursAndMinutesText();
            ShowText(text);
        }
    }

    private string HoursAndMinutesToText(int hours, int minutes)
    {
        string text;
        if (hours <= 99)
        {
            string hoursText = ToInvariantString(hours);
            if (hours < 10)
            {
                hoursText = " " + hoursText;
            }
            string minutesText = ToInvariantString(minutes);
            if (minutes < 10)
            {
                minutesText = "0" + minutesText;
            }
            text = hoursText + ":" + minutesText;
        }
        else
        {
            // Too large - show the max value
            text = "99:99";
        }
        return text;
    }

    public void ShowNumber(int number)
    {
        string text;
        if (number <= 9999)
        {
            text = ToInvariantString(number);
        }
        else
        {
            text = GetMaxNumberText();
        }
        text = text.PadLeft(4, ' ');
        ShowText(text);
    }

    private string ToInvariantString(int number)
    {
        return number.ToString(CultureInfo.InvariantCulture);
    }

    private string GetMaxHoursAndMinutesText()
    {
        return "99:99";
    }

    private string GetMaxNumberText()
    {
        return "9999";
    }

    private byte[] EncodeString(string text)
    {
        byte[] segments = new byte[text.Length];
        bool isColonFound = false;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch != ':' && ch != '.')
            {
                segments[i] = EncodeChar(text[i]);
            }
            else
            {
                isColonFound = true;
            }
        }
        if (isColonFound)
        {
            segments[1] |= 0x80;
        }
        return segments;
    }

    private byte EncodeChar(char ch)
    {
        if (ch == ' ')
        {
            // Space
            return _state.Segments[36];
        }
        if (ch == 42)
        {
            // Star/degrees
            return _state.Segments[38];
        }
        if (ch == '-')
        {
            // Dash
            return _state.Segments[37];
        }
        if (65 <= ch && ch <= 90)
        {
            // Uppercase A-Z
            return _state.Segments[ch - 55];
        }
        if (97 <= ch && ch <= 122)
        {
            // Lowercase a-z
            return _state.Segments[ch - 87];
        }
        if (48 <= ch && ch <= 57)
        {
            // 0-9
            return _state.Segments[ch - 48];
        }

        // Unsupported - return space
        return _state.Segments[36];
    }

    private void Delay()
    {
        Tm1637PortedDisplayDelayHelper.DelayMicroseconds((int)_state.Config.ClockWidth.TotalMicroseconds, true);
    }

    private void Start()
    {
        ExecuteSequence(_state.Sequences.Start);
    }

    private void Stop()
    {
        ExecuteSequence(_state.Sequences.Stop);
    }

    private void WriteDataCommand()
    {
        Start();
        WriteByte(Tm1637Commands.TM1637_CMD1_Data);
        Stop();
    }

    private void WriteDisplayControl()
    {
        Start();
        byte brightnessCommandByte = (byte)(
            Tm1637Commands.TM1637_CMD3_DisplayControl
            | Tm1637Commands.TM1637_DSP_ON_DisplayOn
            | _state.Brightness
        );
        WriteByte(brightnessCommandByte);
        Stop();
    }

    private void WriteByte(byte b)
    {
        for (int i = 0; i < 8; i++)
        {
            bool isDataPinHigh = ((b >> i) & 1) == 1;
            SetDataPin(isDataPinHigh);
            ExecuteSequence(_state.Sequences.WriteByteBitEnd);
        }
        ExecuteSequence(_state.Sequences.WriteByteEnd);
    }

    private void WriteSegments(byte[] segments)
    {
        WriteDataCommand();
        Start();
        WriteByte(Tm1637Commands.TM1637_CMD2_Address);
        foreach (byte b in segments)
        {
            WriteByte(b);
        }
        Stop();
        WriteDisplayControl();
    }

    private void ExecuteSequence(string data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            char ch = data[i];
            switch (ch)
            {
                case '.':
                    Delay();
                    break;
                case 'c':
                    SetClockPinLow();
                    break;
                case 'C':
                    SetClockPinHigh();
                    break;
                case 'd':
                    SetDataPinLow();
                    break;
                case 'D':
                    SetDataPinHigh();
                    break;
            }
        }
    }

    private void SetClockPin(bool high)
    {
        PinValue value = high ? PinValue.High : PinValue.Low;
        _state.Config.GpioController.Write(_state.Config.ClockPin, value);
    }

    private void SetClockPinHigh()
    {
        _state.Config.GpioController.Write(_state.Config.ClockPin, PinValue.High);
    }

    private void SetClockPinLow()
    {
        _state.Config.GpioController.Write(_state.Config.ClockPin, PinValue.Low);
    }

    private void SetDataPin(bool high)
    {
        PinValue value = high ? PinValue.High : PinValue.Low;
        _state.Config.GpioController.Write(_state.Config.DataPin, value);
    }

    private void SetDataPinHigh()
    {
        _state.Config.GpioController.Write(_state.Config.DataPin, PinValue.High);
    }

    private void SetDataPinLow()
    {
        _state.Config.GpioController.Write(_state.Config.DataPin, PinValue.Low);
    }

    private void SetupPins()
    {
        _state.Config.GpioController.OpenPin(_state.Config.ClockPin, PinMode.Output, PinValue.Low);
        _state.Config.GpioController.OpenPin(_state.Config.DataPin, PinMode.Output, PinValue.Low);
    }

    private class Tm1637PortedState
    {
        public Tm1637PortedDisplayConfig Config { get; set; }
        public byte Brightness { get; set; } = 7;
        public readonly Tm1637Sequences Sequences = new();
        public readonly byte[] Segments = [
            0x3F,0x06,0x5B,0x4F,0x66,0x6D,0x7D,0x07,0x7F,0x6F,
            0x77,0x7C,0x39,0x5E,0x79,0x71,0x3D,0x76,0x06,0x1E,0x76,0x38,0x55,0x54,0x3F,0x73,0x67,
            0x50,0x6D,0x78,0x3E,0x1C,0x2A,0x76,0x6E,0x5B,0x00,0x40,0x63
        ];
        public readonly int MaxHoursAndMinutesAsSeconds = 99 * 60 * 60 + 59 * 60;
    }

    class Tm1637Sequences
    {
        public readonly string Start = "CD.d.c.";
        public readonly string Stop = "cd.C.D.";
        public readonly string WriteByteEnd = "c.C.c";
        public readonly string WriteByteBitEnd = ".C.c.";
    }

    static class Tm1637Commands
    {
        public static byte TM1637_CMD1_Data = 0x40;
        public static byte TM1637_CMD2_Address = 0xc0;
        public static byte TM1637_CMD3_DisplayControl = 0x80;
        public static byte TM1637_DSP_ON_DisplayOn = 0x08;
        public static byte TM1637_MSB_Msb = 0x80;
    }
}

