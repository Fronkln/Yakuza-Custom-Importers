using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using System.IO;
using Yarhl.IO;
using System.Text;
using System.Xml.Linq;

[ScriptedImporter(1, "sct")]
public class SCTCustomImporter : ScriptedImporter
{
    private AssetImportContext m_ctx;
    private DataReader m_reader = null;
    private DataStream m_readStream = null;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        byte[] fileBuffer = File.ReadAllBytes(ctx.assetPath);

        m_ctx = ctx;
        m_readStream = DataStreamFactory.FromArray(fileBuffer, 0, fileBuffer.Length);
        m_reader = new DataReader(m_readStream) { DefaultEncoding = Encoding.GetEncoding(932) };

        string magic = m_reader.ReadString(4);
        m_reader.Stream.Position -= 4;

        GameObject createdCollisionObject = null;

        //Modern SCT, Yakuza 5 and above
        if(magic == "GCTD")
        {
            Debug.Log("OE/DE SCT");

            GCTHeader gctData = GCTReader.Read(m_reader);
            createdCollisionObject = GCTCustomImporter.Process(gctData, m_ctx);
        }
        else //True SCT, Yakuza 3 & 4
        {
            Debug.Log("OOE SCT");

            SCTHeader sctData = SCTReader.Read(m_reader);
            createdCollisionObject = Process(sctData);
        }

        if (createdCollisionObject != null)
        {
            ctx.AddObjectToAsset(Path.GetFileNameWithoutExtension(ctx.assetPath), createdCollisionObject);
            ctx.SetMainObject(createdCollisionObject);
        }
    }

    //Create the stage collision object
    public GameObject Process(SCTHeader sctData)
    {
        GameObject stageColl = new GameObject();

        //Visualize vertices (Debug)
        GameObject debugMeshObj = new GameObject();
        MeshFilter debugMeshFilter = debugMeshObj.AddComponent<MeshFilter>();
        debugMeshFilter.sharedMesh = DebugCreateVerticesMesh(sctData);

        VisualizeVertex visualizer = debugMeshObj.AddComponent<VisualizeVertex>();
        visualizer.Mf = debugMeshFilter;
        visualizer.Scale = 0.1f;

        debugMeshObj.transform.parent = stageColl.transform;

        m_ctx.AddObjectToAsset("debug_test_vertices_mesh", debugMeshFilter.sharedMesh);

        return stageColl;
    }

    //Creates a mesh that only holds vertex information (only for debugging pruposes)
    private static Mesh DebugCreateVerticesMesh(SCTHeader data)
    {
        Mesh mesh = new Mesh();
        mesh.name = "debug_vertices_mesh";
        mesh.vertices = data.Vertices;
        mesh.RecalculateNormals();

        return mesh;
    }
}
