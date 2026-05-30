using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public class Board
{
    private byte[][] _matrix = null;
    private Color32[] _palette = null;

    private byte[][] _map = null;
    public byte[][] Map { get { return _map; } }

    public byte[][] Matrix { get { return _matrix; } }
    public Color32[] Palette { get { return _palette; } }

    public int Height { get { return _matrix == null ? -1 : _matrix.Length; } }
    public int Width { get { return _matrix == null ? -1 : _matrix[0].Length; } }

    static readonly byte[] PNG_SIGNATURE = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private enum COLOR_TYPE
    {
        USE_PALETTE = (0x01 << 0),
        USE_COLOR = (0x01 << 1),
        USE_ALPHA = (0x01 << 2)
    }

    private enum FILTER_TYPE
    {
        NONE,
        SUB,
        UP,
        AVERAGE,
        PAETH
    }

    // エリア探索用
    private enum DIRECTION
    {
        UP,
        RIGHT,
        DOWN,
        LEFT,
        BACK
    }

    public Board()
    {

    }

    public Board(byte[] bytes)
    {
        uint offset = 0;

        // ファイルヘッダ解析
        if (bytes.AsSpan(0, PNG_SIGNATURE.Length).SequenceEqual(PNG_SIGNATURE) == false)
        {
            return;
        }

        int width = 0;
        int height = 0;
        byte bitDepth = 0;
        byte colorType = 0;
        byte compressionMethod = 0;
        byte filterMethod = 0;
        byte interlaceMethod = 0;

        offset += 8;

        // IDAT連結用
        using MemoryStream idatStream = new MemoryStream();

        while (offset < bytes.Length)
        {
            var chunk = readChunk(bytes, offset);

            //Debug.Log(chunk.name);

            switch (chunk.name)
            {
                case "IHDR":
                    {
                        width = (int)ReadBigEndianUInt32(chunk.data, 0);
                        height = (int)ReadBigEndianUInt32(chunk.data, 4);
                        bitDepth = chunk.data[8];
                        colorType = chunk.data[9];
                        compressionMethod = chunk.data[10];
                        filterMethod = chunk.data[11];
                        interlaceMethod = chunk.data[12];

                        if ((colorType & (int)COLOR_TYPE.USE_PALETTE) == 0)
                        {
                            return;
                        }
                    }
                    break;
                case "PLTE":
                    {
                        _palette = new Color32[chunk.data.Length / 3];

                        for (int index = 0, dataIndex = 0; index < _palette.Length; index++, dataIndex += 3)
                        {
                            _palette[index].r = chunk.data[dataIndex + 0];
                            _palette[index].g = chunk.data[dataIndex + 1];
                            _palette[index].b = chunk.data[dataIndex + 2];
                            _palette[index].a = 255;
                        }
                    }
                    break;
                case "IDAT":
                    {
                        // 連結して溜めておくだけ
                        idatStream.Write(chunk.data, 0, chunk.data.Length);
                    }
                    break;
                case "IEND":
                    // 強制終了
                    offset = (uint)bytes.Length;
                    break;
                case "acTL":
                case "bKGD":
                case "cHRM":
                case "eXIf":
                case "fcTL":
                case "fdAT":
                case "gAMA":
                case "hIST":
                case "iCCP":
                case "iTXt":
                case "pHYs":
                case "sBIT":
                case "sPLT":
                case "sRGB":
                case "tEXt":
                case "tIME":
                    break;
                case "tRNS":
                    {
                        int alphaCount = Mathf.Min(chunk.data.Length, _palette.Length);

                        for (int index = 0; index < alphaCount; index++)
                        {
                            _palette[index].a = chunk.data[index];
                        }
                    }
                    break;
                case "zTXt":
                    break;
                default:
                    break;
            }

            offset += 8 + (uint)(chunk.data.Length) + 4;
        }

        // IDATを解凍
        using MemoryStream inputStream = new MemoryStream(idatStream.ToArray());

        // zlib header 2byte を飛ばす
        inputStream.ReadByte();
        inputStream.ReadByte();

        using DeflateStream deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using MemoryStream outputStream = new MemoryStream();

        deflateStream.CopyTo(outputStream);
        byte[] decompressedImageBytes = outputStream.ToArray();

        int rowDataSize = ((int)width * bitDepth + 7) / 8;
        int stride = 1 + rowDataSize; // 先頭1byteはfilterType

        byte[] previousRow = null;

        switch (bitDepth)
        {
            case 1:
                _matrix = new byte[height][];
                for (int y = 0, rowOffset = 0; y < height; y++, rowOffset += stride)
                {
                    _matrix[y] = new byte[width];

                    FILTER_TYPE filterType = (FILTER_TYPE)decompressedImageBytes[rowOffset];
                    byte[] rowData = decompressedImageBytes.AsSpan(rowOffset + 1, rowDataSize).ToArray();

                    UnfilterRow(filterType, rowData, previousRow, 1);

                    for (int x = 0; x < width; x++)
                    {
                        byte packed = rowData[x / 8];
                        int shift = 7 - (x & 0x07);
                        _matrix[y][x] = (byte)((packed >> shift) & 0x01);
                    }

                    previousRow = rowData;
                }
                break;
            case 2:
                _matrix = new byte[height][];
                for (int y = 0, rowOffset = 0; y < height; y++, rowOffset += stride)
                {
                    _matrix[y] = new byte[width];

                    FILTER_TYPE filterType = (FILTER_TYPE)decompressedImageBytes[rowOffset];
                    byte[] rowData = decompressedImageBytes.AsSpan(rowOffset + 1, rowDataSize).ToArray();

                    UnfilterRow(filterType, rowData, previousRow, 1);

                    for (int x = 0; x < width; x++)
                    {
                        byte packed = rowData[x / 4];
                        int shift = 6 - ((x & 0x03) * 2);
                        _matrix[y][x] = (byte)((packed >> shift) & 0x03);
                    }

                    previousRow = rowData;
                }
                break;
            case 4:
                _matrix = new byte[height][];
                for (int y = 0, rowOffset = 0; y < height; y++, rowOffset += stride)
                {
                    _matrix[y] = new byte[width];

                    FILTER_TYPE filterType = (FILTER_TYPE)decompressedImageBytes[rowOffset];
                    byte[] rowData = decompressedImageBytes.AsSpan(rowOffset + 1, rowDataSize).ToArray();

                    UnfilterRow(filterType, rowData, previousRow, 1);

                    for (int x = 0; x < width; x++)
                    {
                        byte packed = rowData[x / 2];
                        int shift = 4 - ((x & 0x01) * 4);
                        _matrix[y][x] = (byte)((packed >> shift) & 0x0f);
                    }

                    previousRow = rowData;
                }
                break;
            case 8:
                _matrix = new byte[height][];
                for (int y = 0, rowOffset = 0; y < height; y++, rowOffset += stride)
                {
                    _matrix[y] = new byte[width];

                    FILTER_TYPE filterType = (FILTER_TYPE)decompressedImageBytes[rowOffset];
                    byte[] rowData = decompressedImageBytes.AsSpan(rowOffset + 1, rowDataSize).ToArray();

                    UnfilterRow(filterType, rowData, previousRow, 1);

                    for (int x = 0; x < width; x++)
                    {
                        _matrix[y][x] = rowData[x];
                    }

                    previousRow = rowData;
                }
                break;
            default:
                break;
        }

        _map = new byte[height][];
        for (int y = 0; y < height; y++)
        {
            _map[y] = new byte[width];
        }
    }

     private (string name, byte[] data) readChunk(in byte[] bytes, uint offset)
    {
        // 先頭4バイトがデータのサイズ
        uint chunkLength = ReadBigEndianUInt32(bytes, offset);
        // 次の4バイトがチャンク名
        string chunkName = System.Text.Encoding.ASCII.GetString(bytes, (int)offset + 4, 4);

        byte[] data = new byte[chunkLength];
        Array.Copy(bytes, offset + 8, data, 0, (int)chunkLength);

        return (chunkName, data);
    }

    private static uint ReadBigEndianUInt32(in byte[] bytes, uint offset)
    {
        return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
    }

    private static void UnfilterRow(FILTER_TYPE filterType, byte[] currentRow, byte[] previousRow, int bytesPerPixel)
    {
        for (int index = 0; index < currentRow.Length; index++)
        {
            int left = index >= bytesPerPixel ? currentRow[index - bytesPerPixel] : 0;
            int up = previousRow != null ? previousRow[index] : 0;
            int upperLeft = previousRow != null && index >= bytesPerPixel ? previousRow[index - bytesPerPixel] : 0;

            switch (filterType)
            {
                case FILTER_TYPE.NONE:
                    break;

                case FILTER_TYPE.SUB:
                    currentRow[index] = (byte)(currentRow[index] + left);
                    break;

                case FILTER_TYPE.UP:
                    currentRow[index] = (byte)(currentRow[index] + up);
                    break;

                case FILTER_TYPE.AVERAGE:
                    currentRow[index] = (byte)(currentRow[index] + ((left + up) / 2));
                    break;

                case FILTER_TYPE.PAETH:
                    {
                        int estimate = left + up - upperLeft;
                        int distanceLeft = Math.Abs(estimate - left);
                        int distanceUp = Math.Abs(estimate - up);
                        int distanceUpperLeft = Math.Abs(estimate - upperLeft);

                        if (distanceLeft <= distanceUp && distanceLeft <= distanceUpperLeft)
                        {
                            currentRow[index] = (byte)(currentRow[index] + left);
                        }
                        else if (distanceUp <= distanceUpperLeft)
                        {
                            currentRow[index] = (byte)(currentRow[index] + up);
                        }
                        else
                        {
                            currentRow[index] = (byte)(currentRow[index] + upperLeft);
                        }
                    }
                    break;
                default:
                    throw new Exception($"Unknown PNG filter type: {filterType}");
            }
        }
    }

    public void Copy(Board board)
    {
        if (board.Matrix == null)
        {
            _matrix = null;
        }
        else
        {
            if (board.Width != Width || board.Height != Height)
            {
                _matrix = new byte[board.Height][];

                for (int y = 0; y < board.Matrix.Length; y++)
                {
                    _matrix[y] = (byte[])board.Matrix[y].Clone();
                }
            }
            else
            {
                for (int y = 0; y < Height; y++)
                {
                    Array.Copy(board.Matrix[y], _matrix[y], Width);
                }
            }
        }

        if (board.Palette == null)
        {
            _palette = null;
        }
        else
        {
            if(_palette == null || _palette.Length != board.Palette.Length)
            {
                _palette = (Color32[])board.Palette.Clone();
            }
            else
            {
                Array.Copy(board.Palette, _palette, _palette.Length);
            }
        }
    }

    public void PaintArea(int x, int y, byte index)
    {
        if (x < 0 || Width <= x || y < 0 || Height <= y)
        {
            return;
        }

        CalcAreaMap(x, y);

        for(int j = 0; j < Height; j++)
        {
            for(int i = 0; i < Width; i++)
            {
                if(Map[j][i] == 1)
                {
                    _matrix[j][i] = index;
                }
            }
        }
    }

    public void CalcAreaMap(int x, int y)
    {
        if (x < 0 || Width <= x || y < 0 || Height <= y)
        {
            _map = null;
            return;
        }

        if(_map == null || _map.Length != Height || _map[0].Length != Width)
        {
            _map = new byte[Height][];
            for (int j = 0; j < Height; j++)
            {
                _map[j] = new byte[Width];
            }
        }
        else
        {
            for (int j = 0; j < Height; j++)
            {
                Array.Clear(_map[j], 0, _map[j].Length);
            }
        }

        int target = _matrix[y][x];

        List<DIRECTION> stack = new List<DIRECTION>();

        _map[y][x] = 1;
        stack.Add(DIRECTION.UP);

        while (true)
        {
            switch(stack[stack.Count - 1])
            {
                case DIRECTION.UP:
                    if( 0 <= y - 1 && _matrix[y - 1][x] == target && _map[y - 1][x] == 0 )
                    {
                        y -= 1;
                        _map[y][x] = 1;
                        stack.Add(DIRECTION.UP);
                    }
                    else
                    {
                        stack[stack.Count - 1] = DIRECTION.RIGHT;
                    }
                    break;
                case DIRECTION.RIGHT:
                    if (x + 1 < Width && _matrix[y][x + 1] == target && _map[y][x + 1] == 0)
                    {
                        x += 1;
                        _map[y][x] = 1;
                        stack.Add(DIRECTION.UP);
                    }
                    else
                    {
                        stack[stack.Count - 1] = DIRECTION.DOWN;
                    }
                    break;
                case DIRECTION.DOWN:
                    if (y + 1 < Height && _matrix[y + 1][x] == target && _map[y + 1][x] == 0)
                    {
                        y += 1;
                        _map[y][x] = 1;
                        stack.Add(DIRECTION.UP);
                    }
                    else
                    {
                        stack[stack.Count - 1] = DIRECTION.LEFT;
                    }
                    break;
                case DIRECTION.LEFT:
                    if (0 <= x - 1 && _matrix[y][x - 1] == target && _map[y][x - 1] == 0)
                    {
                        x -= 1;
                        _map[y][x] = 1;
                        stack.Add(DIRECTION.UP);
                    }
                    else
                    {
                        stack[stack.Count - 1] = DIRECTION.BACK;
                    }
                    break;
                case DIRECTION.BACK:
                    stack.RemoveAt(stack.Count - 1);
                    
                    if (stack.Count == 0)
                    {
                        return;
                    }
                    
                    switch (stack[stack.Count - 1])
                    {
                        case DIRECTION.UP:
                            y += 1;
                            stack[stack.Count - 1] = DIRECTION.RIGHT;
                            break;
                        case DIRECTION.RIGHT:
                            x -= 1; stack[stack.Count - 1] = DIRECTION.DOWN;
                            break;
                        case DIRECTION.DOWN:
                            y -= 1;
                            stack[stack.Count - 1] = DIRECTION.LEFT;
                            break;
                        case DIRECTION.LEFT:
                            x += 1;
                            stack[stack.Count - 1] = DIRECTION.BACK;
                            break;
                        default:
                            _map = null;
                            return;
                    }
                    break;
                default:
                    _map = null;
                    return;
            }
        }
    }

    public override string ToString()
    {
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();

        stringBuilder.AppendLine("Matrix:");

        if (_matrix != null)
        {
            for (int y = 0; y < _matrix.Length; y++)
            {
                stringBuilder.Append(string.Join(" ", Array.ConvertAll(_matrix[y], value => value.ToString().PadLeft(3, ' '))));
                stringBuilder.AppendLine();
            }
        }
        else
        {
            stringBuilder.AppendLine("null");
        }

        stringBuilder.AppendLine();

        stringBuilder.AppendLine("Palette:");

        if (_palette != null)
        {
            for (int index = 0; index < _palette.Length; index++)
            {
                Color32 color = _palette[index];

                stringBuilder.AppendLine($"{index,3} : ( {color.r.ToString().PadLeft(3, ' ')}, {color.g.ToString().PadLeft(3, ' ')}, {color.b.ToString().PadLeft(3, ' ')}, {color.a.ToString().PadLeft(3, ' ')} )");
            }
        }
        else
        {
            stringBuilder.AppendLine("null");
        }

        return stringBuilder.ToString();
    }
}
