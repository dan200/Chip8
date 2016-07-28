# Chip8
An easily embeddable emulator for the [CHIP-8](https://en.wikipedia.org/wiki/CHIP-8) system, written in C#. Input/Output must be provided by the user.

## Example usage

At startup:

```C#
chip = new Chip8();
chip.LoadROM( File.ReadAllBytes( "INVADERS" ) ); // See Links below
```

60 times per second:

```C#
for( int n=0; n<16; ++n )
{
  bool pressed = false;
  // Set 'pressed' to true if the n'th key on the keypad is held down
  chip.SetKey( n, pressed );
}

for( int i=0; i<CLOCK_RATE/60; ++i ) // 60hz is suggested, but some games will feel better at higher speeds
{
  chip.Step();
}
chip.Tick();

if( chip.DisplayChanged )
{
  // Copy the contents of chip.Display to your framebuffer/texture/etc
  chip.DisplayChanged = false;
}

if( chip.Beep )
{
  // Set audio output volume to 1
}
else
{
  // Set audio output volume to 0
}
```

## Links

1. [CHIP-8 Wikipedia page (includes specification)](https://en.wikipedia.org/wiki/CHIP-8)
2. [CHIP-8 Games Pack (includes several CHIP-8 ROM files for testing)](http://www.zophar.net/pdroms/chip8/chip-8-games-pack.html)
3. [Another CHIP-8 Specification (includes keypad layout diagram)](http://devernay.free.fr/hacks/chip8/C8TECH10.HTM)
