# Device timer wiring

## Relays
- Inputs
  - Relay `TONGLING JQC-3FF-S-Z` without opto-isolator - use 20K resistor between Raspberry PI 5 GPIO output pin and relay input pin
  - Relay `TONGLING JQC-3FF-S-Z` or `SONGLE SRD-05VDC-SL-C` with opto-isolator - use 3.2K resistor between Raspberry PI 5 GPIO output pin and relay input pin

## PlayStation power circuit
- GPIO 25 (pin 22) - In:

- Pin 1 (3.3V) --> 20K resistor --> Playstation power signal normal open contact
- Playstation signal relay common contact --> GPIO 25 (pin 22)


- GPIO 23 (pin 16) - Out - Pin 16 -> 20K -> Power on relay in pin - this will alternate between 5 seconds Low state and 1 second High state until GPIO 25 (pin 22) provides signal 
- GPIO ?? - In - 20K - Coin inserted signal
