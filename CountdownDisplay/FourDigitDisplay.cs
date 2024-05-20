using System.ComponentModel.DataAnnotations;
using System.Device.Gpio;
using System.Globalization;
using System.Runtime.Serialization;
// using Iot.Device.Tm1637;
using DeviceTimer.CountdownDisplay.CustomTm1637;

namespace DeviceTimer.CountdownDisplay;

public class FourDigitDisplay : IDisposable
{
    private FourDigitDisplayState _state;
    public FourDigitDisplay(GpioController gpioController, int clkPin, int dtoPin)
    {
        _state = new FourDigitDisplayState
        {
            ClkPinNumber = clkPin,
            DioPinNumber = dtoPin,
            GpioController = gpioController,
        };
        _state.CharMap = new Dictionary<char, Character>
        {
            { '0', Character.Digit0 },
            { '1', Character.Digit1 },
            { '2', Character.Digit2 },
            { '3', Character.Digit3 },
            { '4', Character.Digit4 },
            { '5', Character.Digit5 },
            { '6', Character.Digit6 },
            { '7', Character.Digit7 },
            { '8', Character.Digit8 },
            { '9', Character.Digit9 },
            { 'A', Character.A },
            { 'B', Character.B },
            { 'C', Character.C },
            { 'D', Character.D },
            { 'E', Character.E },
            { 'F', Character.F },
            { '-', Character.Minus },
            { ' ', Character.Nothing },
        };
    }

    public void Init()
    {
        _state.Tm = new Tm1637(_state.ClkPinNumber, _state.DioPinNumber, PinNumberingScheme.Logical, _state.GpioController, false);
        // _state.Tm.SetScreen(7, true, true);
        _state.Tm.Brightness = 7;
    }

    public void Test()
    {
        using (Tm1637 tm1637 = new Tm1637(5, 6))
        {
            // When creating the instance without GpioController provided, a new instance of GpioController is created using PinNumberingScheme.Logical from default constructor. The instance of GpioController is disposed with the instance of Tm16xx.
            // Provides an instance of GpioController when constructing Tm16xx instance when specified factory of GpioController is required or for reusing. The instance of GpioContoller provided is not disposed with the instance of Tm16xx.
            // Some board need a delay for self initializing.
            Thread.Sleep(100);

            // Set to the brightest for the next displaying.
            // Note: Setting state by using properties is also supported but using SetScreen is recommended. By using SetScreen instead, not supported properties could be ignored and meaningless device communications are avoided.
            tm1637.SetScreen(7, true, true);
            // Set waitForNextDisplay to true: no data is sent to device but leave it till next digit updates.
            // This will save one communication because the protocol of Tm1637 is defineded to send screen state and digits together.
            // No standalone command for changing screen state only without updating at least one digit.
            // If the screen state need to be changed immediately, leave waitForNextDisplay as false.
            // The default value of waitForNextDisplay is false.

            // Display 12.34
            tm1637.Display(Character.Digit1, Character.Digit2 | Character.Dot, Character.Digit3, Character.Digit4);

            Thread.Sleep(3000);

            // Update digits one by one to ABCD
            tm1637.Display((byte)0, Character.A);
            Thread.Sleep(300);
            tm1637.Display(1, Character.B);
            Thread.Sleep(300);
            tm1637.Display(2, Character.C);
            Thread.Sleep(300);
            tm1637.Display(3, Character.D);

            Thread.Sleep(3000);

            // Flash
            for (int i = 0; i < 5; i++)
            {
                // turn off the screen
                tm1637.IsScreenOn = false;
                Thread.Sleep(200);

                // turn on the screen and set the brightness to 3
                tm1637.SetScreen(3, true, false);
                Thread.Sleep(200);

                // turn on the screen and set the brightness to 7
                tm1637.SetScreen(7, true, false);
                Thread.Sleep(200);
            }

            Console.WriteLine("Press any key to quit...");
            // Console.ReadKey(true);

            // Clear before quit.
            tm1637.ClearDisplay();
        }
    }

    public void SetScreenOnOffState(bool isOn)
    {
        _state.Tm.ScreenOn = isOn;
    }

    public void ShowCharacterAtPosition(byte position, char character)
    {
        bool found = _state.CharMap.TryGetValue(character, out Character characterToDisplay);
        if (found)
        {
            _state.Tm.Display(position, characterToDisplay);
        }
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

    /// <summary>
    /// Shows a text of 4 digits/letters. Also supports colon/dot on third place
    /// </summary>
    /// <param name="text">String to show in format "12:34" or "12.34" or "1234". Letters A to F, minus, colon/dot and space are also supported.
    /// Empty characters must be specified as space as in " 1:23" or " 1.23" or "  12".
    /// Full list of supported characters - "1234567890ABCDEF:.-" - the ":" and "." are equivalent.
    /// </param>
    public void ShowText(string text)
    {
        Character[] displayChars = GetDisplayCharacters(text);
        SetDisplayCharacters(displayChars);
    }

    public void ClearDisplay()
    {
        _state.Tm.ClearDisplay();
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _state.Tm?.Dispose();
            _state.Tm = null;
        }
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

    private void SetDisplayCharacters(Character[] characters)
    {
        // TODO: Queue so we don't update the screen too often to avoid glitches
        _state.Tm.Display(characters);
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

    /// <summary>
    /// Generates characters array to display
    /// </summary>
    /// <param name="text">String to generate characters from in format "12:34" or "12.34" or "1234". Empty characters must be specified as space as in " 1:23" or " 1.23" or "  12"</param>
    /// <returns></returns>
    private Character[] GetDisplayCharacters(string text)
    {
        bool hasSeparator = false;
        Character[] displayCharacters = new Character[4];
        int displayCharacterIndex = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char currentChar = text[i];
            if (currentChar == ':' || currentChar == '.')
            {
                hasSeparator = true;
            }
            else
            {
                var charValueFound = _state.CharMap.TryGetValue(currentChar, out Character charValue);
                displayCharacters[displayCharacterIndex] = charValueFound ? charValue : Character.Nothing;
                displayCharacterIndex++;
            }
        }
        if (hasSeparator)
        {
            displayCharacters[1] = displayCharacters[1] | Character.Dot;
        }
        return displayCharacters;
    }

    private class FourDigitDisplayState
    {
        public int ClkPinNumber { get; set; }
        public int DioPinNumber { get; set; }
        public GpioController GpioController { get; set; }
        public Tm1637? Tm { get; set; }
        public Dictionary<char, Character> CharMap { get; set; }
        public readonly int MaxHoursAndMinutesAsSeconds = 99 * 60 * 60 + 59 * 60;
    }
}
