using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.WintabImpl;

public static class WintabMemUtils
{
    /// <summary>
    /// Marshal unmanaged data packets into managed WintabPacket data.
    /// </summary>
    /// <param name="numPkts_I">number of packets to marshal</param>
    /// <param name="buf_I">pointer to unmanaged heap memory containing data packets</param>
    /// <returns></returns>

    /// <summary>
    /// Marshal unmanaged data packets into managed WintabPacket data.
    /// </summary>
    /// <param name="numPkts_I">number of packets to marshal</param>
    /// <param name="buf_I">pointer to unmanaged heap memory containing data packets</param>
    /// <returns></returns>
    public static WintabPacket[] MarshalDataPackets(UInt32 numPkts_I, IntPtr buf_I)
    {
        if (numPkts_I == 0 || buf_I == IntPtr.Zero)
        {
            return [];
        }

        WintabPacket[] packets = new WintabPacket[numPkts_I];

        int pktSize = Marshal.SizeOf(new WintabPacket());

        for (int pktsIdx = 0; pktsIdx < numPkts_I; pktsIdx++)
        {
            // Tracing can be added here to capture raw packet data if desired

            packets[pktsIdx] = Marshal.PtrToStructure<WintabPacket>(IntPtr.Add(buf_I, pktsIdx * pktSize));
        }

        return packets;
    }

    /// <summary>
    /// Marshal unmanaged Extension data packets into managed WintabPacketExt data.
    /// </summary>
    /// <param name="numPkts_I">number of packets to marshal</param>
    /// <param name="buf_I">pointer to unmanaged heap memory containing data packets</param>
    /// <returns></returns>
    public static WintabPacketExt[] MarshalDataExtPackets(UInt32 numPkts_I, IntPtr buf_I)
    {
        WintabPacketExt[] packets = new WintabPacketExt[numPkts_I];

        if (numPkts_I == 0 || buf_I == IntPtr.Zero)
        {
            return [];
        }

        // Marshal each WintabPacketExt in the array separately.
        // This is "necessary" because none of the other ways I tried to marshal
        // seemed to work.  It's ugly, but it works.
        int pktSize = Marshal.SizeOf(new WintabPacketExt());
        Byte[] byteArray = new Byte[numPkts_I * pktSize];
        Marshal.Copy(buf_I, byteArray, 0, (int)numPkts_I * pktSize);

        Byte[] byteArray2 = new Byte[pktSize];

        for (int pktsIdx = 0; pktsIdx < numPkts_I; pktsIdx++)
        {
            for (int idx = 0; idx < pktSize; idx++)
            {
                byteArray2[idx] = byteArray[(pktsIdx * pktSize) + idx];
            }

            IntPtr tmp = Marshal.AllocHGlobal(pktSize);
            Marshal.Copy(byteArray2, 0, tmp, pktSize);

            packets[pktsIdx] = Marshal.PtrToStructure<WintabPacketExt>(tmp);
        }

        return packets;
    }
}
