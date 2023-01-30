using System;
using System.Runtime.InteropServices;
using System.Text;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace GH3Controller
{
    internal class StarpowerActivator
    {
        // if for some reason your game window isn't called "clone hero", you'll need to change it here. case sensitive.
        static readonly string GameWindowName = "Clone Hero";

        // if your guitar uses a different vendorid/productid you'll need to change it here. i only know my own guitars.
        static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x12ba, 0x0100);

        // set the key to use in clone hero to activate starpower. get the key from https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
        // the one already set is 's'
        static uint StarPowerKey = 0x53;

        static IUsbDevice PS3Guitar;
        

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private static bool GameIsForeground()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                var foregroundWindowName = Buff.ToString();
                //Console.WriteLine($"window: {foregroundWindowName}");
                return foregroundWindowName == GameWindowName;
            }
            return false;
        }

        public static void Main(string[] args)
        {
            Error ec = Error.Success;

            using (UsbContext context = new UsbContext())
            {
                try
                {
                    // Find and open the usb device.
                    PS3Guitar = (UsbDevice)context.Find(MyUsbFinder);
                    
                    // If the device is open and ready
                    if (PS3Guitar == null) throw new Exception("Device Not Found.");
                    
                    PS3Guitar.Open();

                    // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                    // it exposes an IUsbDevice interface. If not (WinUSB) the 
                    // 'wholeUsbDevice' variable will be null indicating this is 
                    // an interface of a device; it does not require or support 
                    // configuration and interface selection.
                    IUsbDevice wholeUsbDevice = PS3Guitar as IUsbDevice;
                    if (!ReferenceEquals(wholeUsbDevice, null))
                    {
                        // This is a "whole" USB device. Before it can be used, 
                        // the desired configuration and interface must be selected.

                        // Select config #1
                        wholeUsbDevice.SetConfiguration(1);

                        // Claim interface #0.
                        wholeUsbDevice.ClaimInterface(0);
                    }

                    // open read endpoint 1.
                    var reader = PS3Guitar.OpenEndpointReader(ReadEndpointID.Ep01);


                    byte[] readBuffer = new byte[28];
                    bool? lastTiltState = null;
                    int starPowerInc = 0;
                    while (ec == Error.Success)
                    {
                        int bytesRead;

                        // If the device hasn't sent data in the last 5 seconds,
                        // a timeout error (ec = IoTimedOut) will occur. 
                        ec = reader.Read(readBuffer, 5000, out bytesRead);

                        if (bytesRead == 0) throw new Exception(string.Format("{0}:No more bytes!", ec));
                        var isTilted = readBuffer[20] == 2 && readBuffer[22] == 2 && readBuffer[24] == 2;
                        var activateStarPower = false;

                        /* 
                         * 19, 20, 21, 22, 23, 24 are related to guitar tilt
                         * 19 and 20 - not really sure, it seems to stick, but [20] == 2 seems indicative of the guitar being around 45 deg to the vertical.
                         * 21 and 22 - likewise, i don't care enough to test but [22] == 2 seems indicative of guitar being vertical
                         * 23 and 24 - these seem related to front tilt and probably are not used for starpower
                         */

                        if(lastTiltState != null)
                        {
                            activateStarPower = isTilted && !(bool)lastTiltState;
                        }

                        
                        lastTiltState = isTilted;
                        
                        if (activateStarPower && GameIsForeground())
                        {

                            const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
                            keybd_event((byte)StarPowerKey, 0, KEYEVENTF_EXTENDEDKEY | 0, 0);
                            Console.WriteLine($"Activating star power {++starPowerInc}!");
                        }

                        // you can debug your guitar inputs here if you care to
                        // just change the loop to include i=0 to i<28 and then uncomment the writeline
                        // the guitars i tested were gh3 les paul and the WT guitars, they both had the same bytearray read of len 28
                        var sb = new StringBuilder("new byte[] { ");
                        for(var i=19; i <= 22; i++)
                        {
                            var b = readBuffer[i];
                            sb.Append($"[{i}]: {b} ,");
                        }
                        sb.Append("}");
                        //Console.WriteLine(sb.ToString());

                    }

                    Console.WriteLine("\r\nDone!\r\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine((ec != Error.Success ? ec + ":" : String.Empty) + ex.Message);
                }
                finally
                {
                    if (PS3Guitar != null)
                    {
                        if (PS3Guitar.IsOpen)
                        {
                            // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                            // it exposes an IUsbDevice interface. If not (WinUSB) the 
                            // 'wholeUsbDevice' variable will be null indicating this is 
                            // an interface of a device; it does not require or support 
                            // configuration and interface selection.
                            IUsbDevice wholeUsbDevice = PS3Guitar as IUsbDevice;
                            if (!ReferenceEquals(wholeUsbDevice, null))
                            {
                                // Release interface #0.
                                wholeUsbDevice.ReleaseInterface(0);
                            }

                            PS3Guitar.Close();
                        }
                        PS3Guitar = null;
                    }

                    // Wait for user input..
                    Console.WriteLine("bye!");
                    Console.ReadKey();
                }
            }
        }
    }
}
