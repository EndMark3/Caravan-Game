using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class ByteArrayConverter
{
    // Convert byte[] to NativeArray<byte>
    public static NativeArray<byte> ConvertToNativeArray(byte[] byteArray, Allocator allocator)
    {
        NativeArray<byte> nativeArray = new NativeArray<byte>(byteArray.Length, allocator);
        nativeArray.CopyFrom(byteArray);
        return nativeArray;
    }

    // Convert NativeArray<byte> to byte[]
    public static byte[] ConvertToByteArray(NativeArray<byte> nativeArray)
    {
        byte[] byteArray = new byte[nativeArray.Length];
        nativeArray.CopyTo(byteArray);
        return byteArray;
    }
}