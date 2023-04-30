﻿using Yarhl.IO;

public static class DataReaderExtension {
    /// <summary>
    /// Simply push the reader stream forward by N bytes.
    /// 
    /// This is different to SkipPadding(), which pads the current stream position out to a multiple of the argument.
    /// For example, if a stream were at 0xec0 and called SkipAhead(4) it would go to 0xec4,
    /// but if it called SkipPadding it would stay at 0xec0 because that is divisible by 4.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="bytes"></param>
    public static void SkipAhead(this DataReader reader, int bytes) {
        reader.Stream.Seek(bytes, SeekMode.Current);
    }
}