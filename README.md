# Chip8
An easily embeddable emulator for the [CHIP-8](https://en.wikipedia.org/wiki/CHIP-8) system, written in C#. I/O not included.

## Example usage

At startup:

```C#
chip = new Chip8();
chip.LoadROM( File.ReadAllBytes( "INVADERS" ) ); // Find CHIP-8 ROMs at http://www.zophar.net/pdroms/chip8/chip-8-games-pack.html
```

60 times per second:

```C#
chip.Tick();
for( int i=0; i<CLOCK_RATE/60; ++i ) // 60hz is suggested, but some games will feel better at higher speeds
{
  chip.Step();
}

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
