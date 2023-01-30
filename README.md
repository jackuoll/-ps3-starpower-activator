# PS3 guitar controller starpower activator for Windows

This program listens to your guitar input and simulates pressing a key on your keyboard when you tilt the guitar. Although I'd prefer you compile and run it yourself, I published an executable version [here](https://drive.google.com/file/d/1kpMWko2No_Ij9fT4Y6CQ-tISG5UoWpcT/view?usp=sharing) that you can download.

This won't work with Linux since it is using the Windows API for a bunch of things, I may update it to work with Linux at some point but odds are slim. It could be pretty easily re-written in Python using PyUSB, here is a snippet of python code reading the tilt:

```python
import usb.core
import usb.backend.libusb1


REDOCTANE_PS3_GUITAR = 0x12ba, 0x0100
usb.backend.libusb1.get_backend(find_library=lambda _: "./libusb-1.0.dll")  # note you need libusb
dev = usb.core.find(idVendor=REDOCTANE_PS3_GUITAR[0], idProduct=REDOCTANE_PS3_GUITAR[1])
was_tilted = False

while True:
    try:
        res = dev.read(0x81, 28, 100)
    except usb.core.USBError as e:
        if e.errno == 19:
            self.connection_lost.emit()
            print("Connection to USB device lost.")
            break
        raise
    
    is_tilted = res[20] == 2 and res[22] == 2 and res[24] == 2
    if is_tiled and not was_tilted:
        pass  # send a key input
```

## Instructions for Clone Hero

1. First, configure the script with whatever key you want it to push when you tilt the guitar. The default is 's', so if you're fine with that, you don't need to change it. If not, you must change `static uint StarPowerKey = 0x53;` to the [valid keycode](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes).
2. Your Clone Hero window should have the name "Clone Hero". The window name that is in the foreground of your PC is used to determine whether or not the Starpower key should be hit or not, to avoid random inputs being pressed when Clone Hero is not active. If your game does not have the window name "Clone Hero", then you will need to change it in the script (`static readonly string GameWindowName = "Clone Hero";`)
3. It's possible your Guitar may not have the same vendor ID/product ID as mine. You can try fiddle with it in `static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x12ba, 0x0100);`, but please don't bother asking me for support regarding this. Good luck!

### Issues
There may be issues with it I find while playing so this will probably see some updates. So far the tilt seems good most the time, but occasionally it will be difficult to activate, or other times get stuck on activation.

### Notes 

* I threw this together in about an hour and obviously I don't have a plethora of GH hardware to test it with. I tested it with my own PS3 WT wireless guitars.
* I do not know if this is the same way the official GH games recognise tilt and don't really care to spend time figuring out the best way to detect tilt. Take it or leave it. There's some notes in the script itself you can use to debug if you care to.
