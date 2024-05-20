# Device timer wiring

## Relays
- Relay `TONGLING JQC-3FF-S-Z` without opto-isolator - use 20K resistor between GPIO output pin and relay input pin
- Relay `TONGLING JQC-3FF-S-Z` (two-channel relay) with opto-isolator or `SONGLE SRD-05VDC-SL-C` (single-channel relay) with opto-isolator - resistor value between GPIO output pin and relay input ping must be experimentally found
- Relay `SONGLE SRD-05VDC-SL-C` (single-channel relay) with opto-isolator - resistor of 5.5K should work

## PlayStation power circuit
- GPIO 25 (pin 22) - In:
- Pin 1 (3.3V) --> 20K resistor --> Playstation power signal normal open contact
- Playstation signal relay common contact --> GPIO 25 (pin 22)
- GPIO 23 (pin 16) - Out - Pin 16 -> 20K or 1K depending on the relay (see `Relays`) -> Power on relay input pin - this will alternate between 5 seconds Low state and 1 second High state until GPIO 25 (pin 22) provides signal 
- GPIO 16 (pin 36) - Coin inserted signal:
  - Coin device coin inserted output pin (must be between 2.6V and 3.3V) -> 20K resistor -> GPIO 16. If the coin device coin inserted output pin provides more than 3.3V, it must control a relay which can be used as switch between Raspberry PI 5 3.3V (pin 1) -> 20K resistor -> relay normal open contacts _/_ -> GPIO 16
