///////////////////////////////////////////////////////////////////////////////
//
//	PURPOSE
//		Wintab information access for WintabDN
//
//	COPYRIGHT
//		Copyright (c) 2010-2020 Wacom Co., Ltd.
//
//		The text and information contained in this file may be freely used,
//		copied, or distributed without compensation or licensing restrictions.
//
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.WintabImpl
{
    /// <summary>
    /// Class to access Wintab interface data.
    /// </summary>
    public class WintabInfo
    {
        public const Int32 MAX_STRING_SIZE = 256;
        public const Int32 MAX_NUM_ATTACHED_TABLETS = 16;
        public const Int32 MAX_NUM_CURSORS = 6;

        /// <summary>
        /// Returns TRUE if Wintab service is running and responsive.
        /// </summary>
        /// <returns></returns>
        public static bool IsWintabAvailable()
        {
            IntPtr buf = IntPtr.Zero;

            var status = (WintabFuncs.WTInfoA(0, 0, buf) > 0);

            return status;
        }

        /// <summary>
        /// Return max normal pressure supported by tablet.
        /// </summary>
        /// <param name="getNormalPressure_I">TRUE=> normal pressure;
        /// FALSE=> tangential pressure (not supported on all tablets)</param>
        /// <returns>maximum pressure value or zero on error</returns>
        public static Int32 GetMaxPressure(bool getNormalPressure_I = true)
        {
            WintabAxis pressureAxis = new WintabAxis();
            int numBytes = Marshal.SizeOf(pressureAxis);
            IntPtr buf = Marshal.AllocHGlobal(numBytes);

            EWTIDevicesIndex devIdx = (getNormalPressure_I ?
                EWTIDevicesIndex.DVC_NPRESSURE :
                EWTIDevicesIndex.DVC_TPRESSURE);

            int size = (int)WintabFuncs.WTInfoA(
                (uint)EWTICategoryIndex.WTI_DEVICES,
                (uint)devIdx, buf);

            pressureAxis = Marshal.PtrToStructure<WintabAxis>(buf);

            Marshal.FreeHGlobal(buf);

            return pressureAxis.axMax;
        }

        /// <summary>
        /// Returns a 3-element array describing the tablet's orientation range and resolution capabilities.
        /// </summary>
        /// <returns></returns>
        public static WintabAxisArray GetDeviceOrientation(out bool tiltSupported_O)
        {
            WintabAxisArray axisArray = new WintabAxisArray();
            tiltSupported_O = false;
            IntPtr buf = Marshal.AllocHGlobal(Marshal.SizeOf(axisArray));

            int size = (int)WintabFuncs.WTInfoA(
                (uint)EWTICategoryIndex.WTI_DEVICES,
                (uint)EWTIDevicesIndex.DVC_ORIENTATION, buf);

            // If size == 0, then returns a zeroed struct.
            axisArray = Marshal.PtrToStructure<WintabAxisArray>(buf);
            tiltSupported_O = (axisArray.array[0].axResolution != 0 && axisArray.array[1].axResolution != 0);

            Marshal.FreeHGlobal(buf);

            return axisArray;
        }

        /*/// <summary>
        /// Returns a string containing device name.
        /// </summary>
        /// <returns></returns>
        public static string GetDeviceInfo()
        {
            string devInfo = null;
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(MAX_STRING_SIZE);

            try
            {
                int size = (int)CWintabFuncs.WTInfoA(
                    (uint)EWTICategoryIndex.WTI_DEVICES,
                    (uint)EWTIDevicesIndex.DVC_NAME, buf);

                if (size < 1)
                {
                    throw new Exception("GetDeviceInfo returned empty string.");
                }

                // Strip off final null character before marshalling.
                devInfo = CMemUtils.MarshalUnmanagedString(buf, size - 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED GetDeviceInfo: " + ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);
            return devInfo;
        }

        /// <summary>
        /// Returns the default digitizing context, with useful context overrides.
        /// </summary>
        /// <param name="options_I">caller's options; OR'd into context options</param>
        /// <returns>A valid context object or null on error.</returns>
        public static CWintabContext GetDefaultDigitizingContext(ECTXOptionValues options_I = 0)
        {
            // Send all possible data bits (not including extended data).
            // This is redundant with CWintabContext initialization, which
            // also inits with PK_PKTBITS_ALL.
            uint PACKETDATA = (uint)EWintabPacketBit.PK_PKTBITS_ALL; // The Full Monty
            uint PACKETMODE = (uint)EWintabPacketBit.PK_BUTTONS;

            CWintabContext context = GetDefaultContext(EWTICategoryIndex.WTI_DEFCONTEXT);

            if (context != null)
            {
                // Add digitizer-specific context tweaks.
                context.PktMode = 0; // all data in absolute mode (set EWintabPacketBit bit(s) for relative mode)
                context.SysMode = false; // system cursor tracks in absolute mode (zero)

                // Add caller's options.
                context.Options |= (uint)options_I;

                // Set the context data bits.
                context.PktData = PACKETDATA;
                context.PktMode = PACKETMODE;
                context.MoveMask = PACKETDATA;
                context.BtnUpMask = context.BtnDnMask;
            }

            return context;
        }

        /// <summary>
        /// Returns the default system context, with useful context overrides.
        /// </summary>
        /// <param name="options_I">caller's options; OR'd into context options</param>
        /// <returns>A valid context object or null on error.</returns>
        public static CWintabContext GetDefaultSystemContext(ECTXOptionValues options_I = 0)
        {
            // Send all possible data bits (not including extended data).
            // This is redundant with CWintabContext initialization, which
            // also inits with PK_PKTBITS_ALL.
            uint PACKETDATA = (uint)EWintabPacketBit.PK_PKTBITS_ALL; // The Full Monty
            uint PACKETMODE = (uint)EWintabPacketBit.PK_BUTTONS;

            CWintabContext context = GetDefaultContext(EWTICategoryIndex.WTI_DEFSYSCTX);

            if (context != null)
            {
                // TODO: Add system-specific context tweaks.

                // Add caller's options.
                context.Options |= (uint)options_I;

                // Make sure we get data packet messages.
                context.Options |= (uint)ECTXOptionValues.CXO_MESSAGES;

                // Set the context data bits.
                context.PktData = PACKETDATA;
                context.PktMode = PACKETMODE;
                context.MoveMask = PACKETDATA;
                context.BtnUpMask = context.BtnDnMask;

                context.Name = "WintabDN Event Data Context";
            }

            return context;
        }

        /// <summary>
        /// Helper function to get digitizing or system default context.
        /// </summary>
        /// <param name="contextType_I">Use WTI_DEFCONTEXT for digital context or WTI_DEFSYSCTX for system context</param>
        /// <returns>Returns the default context or null on error.</returns>
        private static CWintabContext GetDefaultContext(EWTICategoryIndex contextIndex_I)
        {
            CWintabContext context = new CWintabContext();
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(context.LogContext);

            try
            {
                int size = (int)CWintabFuncs.WTInfoA((uint)contextIndex_I, 0, buf);

                context.LogContext = CMemUtils.MarshalUnmanagedBuf<WintabLogContext>(buf, size);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED GetDefaultContext: " + ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return context;
        }

        /// <summary>
        /// Returns the default device.  If this value is -1, then it also known as a "virtual device".
        /// </summary>
        /// <returns></returns>
        public static Int32 GetDefaultDeviceIndex()
        {
            Int32 devIndex = 0;
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(devIndex);

            try
            {
                int size = (int)CWintabFuncs.WTInfoA(
                    (uint)EWTICategoryIndex.WTI_DEFCONTEXT,
                    (uint)EWTIContextIndex.CTX_DEVICE, buf);

                devIndex = CMemUtils.MarshalUnmanagedBuf<Int32>(buf, size);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED GetDefaultDeviceIndex: " + ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return devIndex;
        }

        /// <summary>
        /// Returns the WintabAxis object for specified device and dimension.
        /// </summary>
        /// <param name="devIndex_I">Device index (-1 = virtual device)</param>
        /// <param name="dim_I">Dimension: AXIS_X, AXIS_Y or AXIS_Z</param>
        /// <returns></returns>
        public static WintabAxis GetDeviceAxis(Int32 devIndex_I, EAxisDimension dim_I)
        {
            WintabAxis axis = new WintabAxis();
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(axis);

            try
            {
                int size = (int)CWintabFuncs.WTInfoA(
                    (uint)(EWTICategoryIndex.WTI_DEVICES + devIndex_I),
                    (uint)dim_I, buf);

                // If size == 0, then returns a zeroed struct.
                axis = CMemUtils.MarshalUnmanagedBuf<WintabAxis>(buf, size);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED GetDeviceAxis: " + ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return axis;
        }

        /// <summary>
        /// Returns a 3-element array describing the tablet's rotation range and resolution capabilities
        /// </summary>
        /// <returns></returns>
        public static WintabAxisArray GetDeviceRotation(out bool rotationSupported_O)
        {
            WintabAxisArray axisArray = new WintabAxisArray();
            rotationSupported_O = false;
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(axisArray);

            try
            {
                int size = (int)CWintabFuncs.WTInfoA(
                    (uint)EWTICategoryIndex.WTI_DEVICES,
                    (uint)EWTIDevicesIndex.DVC_ROTATION, buf);

                // If size == 0, then returns a zeroed struct.
                axisArray = CMemUtils.MarshalUnmanagedBuf<WintabAxisArray>(buf, size);
                rotationSupported_O = (axisArray.array[0].axResolution != 0 && axisArray.array[1].axResolution != 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED GetDeviceRotation: " + ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return axisArray;
        }

        /// <summary>
        /// Returns the number of devices connected (attached).
        /// </summary>
        /// <returns>tablet count</returns>
        public static UInt32 GetNumberOfDevices()
        {
            UInt32 numDevices = 0;
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(numDevices);

            try
            {
                int size = (int)CWintabFuncs.WTInfoA(
                    (uint)EWTICategoryIndex.WTI_INTERFACE,
                    (uint)EWTIInterfaceIndex.IFC_NDEVICES, buf);

                numDevices = CMemUtils.MarshalUnmanagedBuf<UInt32>(buf, size);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED GetNumberOfDevices: " + ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return numDevices;
        }

        /// <summary>
        /// Returns whether a stylus is currently connected to the active cursor.
        /// </summary>
        /// <returns></returns>
        public static bool IsStylusActive()
        {
            bool isStylusActive = false;
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(isStylusActive);

            try
            {
                int size = (int)CWintabFuncs.WTInfoA(
                    (uint)EWTICategoryIndex.WTI_INTERFACE,
                    (uint)EWTIInterfaceIndex.IFC_NDEVICES, buf);

                isStylusActive = CMemUtils.MarshalUnmanagedBuf<bool>(buf, size);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED GetNumberOfDevices: " + ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return isStylusActive;
        }

        /// <summary>
        /// Returns a string containing the name of the selected stylus.
        /// </summary>
        /// <param name="index_I">indicates stylus type</param>
        /// <returns></returns>
        public static string GetStylusName(EWTICursorNameIndex index_I)
        {
            string stylusName = null;
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(MAX_STRING_SIZE);

            try
            {
                int size = (int)CWintabFuncs.WTInfoA(
                    (uint)index_I,
                    (uint)EWTICursorsIndex.CSR_NAME, buf);

                if (size < 1)
                {
                    throw new Exception("GetStylusName returned empty string.");
                }

                // Strip off final null character before marshalling.
                stylusName = CMemUtils.MarshalUnmanagedString(buf, size - 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED GetDeviceInfo: " + ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return stylusName;
        }

        /// <summary>
        /// Return the WintabAxis object for the specified dimension.
        /// </summary>
        /// <param name="dimension_I">Dimension to fetch (eg: x, y)</param>
        /// <returns></returns>
        public static WintabAxis GetTabletAxis(EAxisDimension dimension_I)
        {
            WintabAxis axis = new WintabAxis();
            IntPtr buf = CMemUtils.AllocUnmanagedBuf(axis);

            try
            {
                int size = (int)CWintabFuncs.WTInfoA(
                    (uint)EWTICategoryIndex.WTI_DEVICES,
                    (uint)dimension_I, buf);

                axis = CMemUtils.MarshalUnmanagedBuf<WintabAxis>(buf, size);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED GetMaxPressure: " + ex.ToString());
            }

            CMemUtils.FreeUnmanagedBuf(buf);

            return axis;
        }

        /// <summary>
        /// Return the number of tablets that have at some time been attached.
        /// A record of these devices is in the tablet settings.  Since there
        /// is no direct query for this value, we have to enumerate all of
        /// the tablet settings.
        /// </summary>
        /// <returns>tablet count</returns>
        public static UInt32 GetNumberOfConfiguredDevices()
        {
            UInt32 numConfiguredTablets = 0;
            try
            {
                WintabLogContext ctx = new WintabLogContext();
                IntPtr buf = CMemUtils.AllocUnmanagedBuf(ctx);

                for (Int32 idx = 0; idx < MAX_NUM_ATTACHED_TABLETS; idx++)
                {
                    int size = (int)CWintabFuncs.WTInfoA(
                        (UInt32)(EWTICategoryIndex.WTI_DDCTXS + idx), 0, buf);
                    if (size == 0)
                    {
                        break;
                    }
                    else
                    {
                        numConfiguredTablets++;
                    }
                }

                CMemUtils.FreeUnmanagedBuf(buf);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED GetNumberOfConfiguredDevices: " + ex.ToString());
            }

            return numConfiguredTablets;
        }

        /// <summary>
        /// Returns a list of indecies of previous or currently attached devices.
        /// It is up to the caller to use the list to determine which devices are
        /// actually physically device by responding to data events for those devices.
        /// Devices that are not physically attached will, of course, never send
        /// a data event.
        /// </summary>
        /// <returns></returns>
        public static List<Byte> GetFoundDevicesIndexList()
        {
            List<Byte> list = new List<Byte>();

            try
            {
                WintabLogContext ctx = new WintabLogContext();
                IntPtr buf = CMemUtils.AllocUnmanagedBuf(ctx);

                for (Int32 idx = 0; idx < MAX_NUM_ATTACHED_TABLETS; idx++)
                {
                    int size = (int)CWintabFuncs.WTInfoA(
                        (UInt32)(EWTICategoryIndex.WTI_DDCTXS + idx), 0, buf);
                    if (size == 0)
                    {
                        break;
                    }
                    else
                    {
                        list.Add((Byte)idx);
                    }
                }

                CMemUtils.FreeUnmanagedBuf(buf);
            }
            catch (Exception ex)
            {
                MessageBox.Show("FAILED GetNumberOfConfiguredDevices: " + ex.ToString());
            }

            return list;
        }
    }*/
    }
}
