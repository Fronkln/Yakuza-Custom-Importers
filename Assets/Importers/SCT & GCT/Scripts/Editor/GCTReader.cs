using PlasticPipe.PlasticProtocol.Messages;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Unity.VisualScripting;
using UnityEngine;
using Yarhl.IO;

public class GCTReader
{
    private SizedPointer m_nodeChunk;
    private SizedPointer m_shapeChunk;
    private uint m_nodeAaBoxChunk;
    private uint m_shapeAaBoxChunk;
    private SizedPointer m_vertexChunk;

    private GCTHeader m_header;
    private DataReader m_reader;

    public static GCTHeader Read(DataReader reader)
    {
        GCTReader gctReader = new GCTReader();
        gctReader.m_reader = reader;
        gctReader.Read();

        return gctReader.m_header;
    }

    private void Read()
    {
        ReadHeader();
        ReadVertices();
        ReadShapes();

        List<int> indices = new List<int>();

        //Generate indices buffer
        foreach(GCTShapePrimitive prim in m_header.Shapes)
            foreach(uint u in prim.Indices)
            {
                int i = (int)u;

                if (!indices.Contains(i))
                    indices.Add(i);

                if (!indices.Contains((int)prim.NormalIndex))
                    indices.Add((int)prim.NormalIndex);
            }

        m_header.Indices = indices.OrderBy(x => x).ToArray();
    }
    private void ReadHeader()
    {
        m_header = new GCTHeader();
        m_header.Magic = m_reader.ReadString(4);
        m_header.Endian = m_reader.ReadUInt32();
        m_reader.Endianness = m_header.Endian == 511 ? EndiannessMode.BigEndian : EndiannessMode.LittleEndian;

        m_header.Flags = m_reader.ReadInt32();
        m_header.FileSize = m_reader.ReadUInt32();

        m_nodeChunk = m_reader.Read<SizedPointer>();
        m_shapeChunk = m_reader.Read<SizedPointer>();
        m_nodeAaBoxChunk = m_reader.ReadUInt32();
        m_shapeAaBoxChunk = m_reader.ReadUInt32();
        m_vertexChunk = m_reader.Read<SizedPointer>();

        m_header.HitFilter = m_reader.ReadUInt32();
        m_header.NodeDepth = m_reader.ReadUInt32();
        m_reader.Stream.Position += 8; //empty space

        m_header.Bounds.center = new Vector3(m_reader.ReadSingle(), m_reader.ReadSingle(), m_reader.ReadSingle());
        m_header.Bounds.extents = new Vector3(m_reader.ReadSingle(), m_reader.ReadSingle(), m_reader.ReadSingle());
        m_reader.Stream.Position += 8; //unneeded bbox unks
        m_reader.Stream.Position += 36; //empty space
        m_reader.Stream.Position += 8; //unneeded node/shape counts we already knew
        m_reader.Stream.Position += 84; //empty space

        m_header.Name = m_reader.Read<PXDHash>();
    }

    private void ReadShapes()
    {
        m_reader.Stream.Seek(m_shapeChunk.Pointer, SeekMode.Start);

        m_header.Shapes = new GCTShape[m_shapeChunk.Count];

        for(int i = 0; i < m_header.Shapes.Length; i++)
        {
            GCTShapeHeader header = new GCTShapeHeader();
            header.Flags = m_reader.ReadInt32();
            header.Attributes = m_reader.ReadInt32();

            GCTShape readShape = null;
            GCTShapeType shapeType = header.GetShapeType();

            switch(shapeType)
            {
                default:
                    throw new System.Exception("Don't know how to read shape type: " + shapeType);
                case GCTShapeType.Triangle:
                    readShape = new GCTShapeTriangle();
                    break;
                case GCTShapeType.Quad:
                    readShape = new GCTShapeQuad();
                    break;
            }

            readShape.Header = header;
            readShape.ReadData(m_reader);
            m_header.Shapes[i] = readShape;
        }
        
    }

    private void ReadVertices()
    {
        m_reader.Stream.Seek(m_vertexChunk.Pointer, SeekMode.Start);

        long vertexStart = m_reader.Stream.Position;
        long vertexEnd = m_reader.Stream.Length - 36;

        int numVertices = (int)((vertexEnd - vertexStart) / 12);
        
        m_header.Vertices = new Vector3[numVertices];

        for(int i = 0; i < numVertices; i++)
        {
            m_header.Vertices[i] = new Vector3(m_reader.ReadSingle(), m_reader.ReadSingle(), m_reader.ReadSingle());
        }
    }
}
