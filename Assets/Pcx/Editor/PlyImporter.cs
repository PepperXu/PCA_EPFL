// Pcx - Point cloud importer & renderer for Unity
// https://github.com/keijiro/Pcx

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pcx
{
    [ScriptedImporter(1, "ply")]
    class PlyImporter : ScriptedImporter
    {
        private float centerHeight = 0.8f;

        #region ScriptedImporter implementation

        public enum ContainerType { Mesh, ComputeBuffer  }

        [SerializeField] ContainerType _containerType;

        //private float scale;
        //private int vertexCount;
        //[System.Serializable]
        //public class PointCloudConfig
        //{
        //    public float[] lightPosition;
        //}
        //private string dataPath = "/StreamingAssets/data.json";

        public override void OnImportAsset(AssetImportContext context)
        {
            if (_containerType == ContainerType.Mesh)
            {
                // Mesh container
                // Create a prefab with MeshFilter/MeshRenderer.
                var gameObject = new GameObject();
                var mesh = ImportAsMesh(context.assetPath);
                

                var meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                var meshRenderer = gameObject.AddComponent<MeshRenderer>();

                Vector3[] fittedVertArr = DataBody.vertArr;

                string name = Path.GetFileNameWithoutExtension(context.assetPath);

                FitTransform(meshRenderer, gameObject.transform, new Vector3(1.414f, 1f, 1.414f), ref fittedVertArr);
                Stage stage = gameObject.AddComponent<Stage>();
                //string filePath = Application.dataPath + this.dataPath;
                //if (File.Exists(filePath))
                //{
                //string dataAsJson = File.ReadAllText(filePath);
                //PointCloudConfig pcConfig = JsonUtility.FromJson<PointCloudConfig>(dataAsJson);
                Vector3 lightPos = new Vector3(0f, 100f, 0f);
                Vector2 quadPos, quadScale;
                Vector2[] normShadowCoord = CreateNormalisedShadowCoordinates(lightPos, fittedVertArr,out quadPos,out quadScale);
                stage.quadPos = quadPos;
                stage.quadScale = quadScale;
                CreateShadowTexture(normShadowCoord, name);
                //}
                //else
                //{
                //    Debug.Log("LightPos info not found!");
                //}

                string path = "Assets/Resources/Materials/" + name + ".mat";

                AssetDatabase.CopyAsset("Assets/Resources/Materials/Default Point.mat", path);

                Material mat = null;
                
                mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                meshRenderer.sharedMaterial = mat;

                gameObject.AddComponent<MatConfig>();
                

                context.AddObjectToAsset("prefab", gameObject);
                if (mesh != null) context.AddObjectToAsset("mesh", mesh);

                context.SetMainObject(gameObject);
            }
            else
            {
                // ComputeBuffer container
                // Create a prefab with PointCloudRenderer.
                var gameObject = new GameObject();
                var data = ImportAsPointCloudData(context.assetPath);

                var renderer = gameObject.AddComponent<PointCloudRenderer>();
                renderer.sourceData = data;

                context.AddObjectToAsset("prefab", gameObject);
                if (data != null) context.AddObjectToAsset("data", data);

                context.SetMainObject(gameObject);
            }
        }

        #endregion

        #region Internal utilities

        static Material CopyAndGetMaterial(string path)
        {
            AssetDatabase.CopyAsset("Assets/Resources/Materials/Default Point.mat", "Assets/Resources/Materials/" + Path.GetFileNameWithoutExtension(path) + ".mat");
            return Resources.Load<Material>("Materials/" + Path.GetFileNameWithoutExtension(path));
        }

        static Material GetDefaultMaterial()
        {
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Default Point.mat");
        }

        #endregion

        #region Internal data structure

        enum DataProperty {
            Invalid,
            X, Y, Z,
            R, G, B, A,
            Data8, Data16, Data32
        }

        static int GetPropertySize(DataProperty p)
        {
            switch (p)
            {
                case DataProperty.X: return 4;
                case DataProperty.Y: return 4;
                case DataProperty.Z: return 4;
                case DataProperty.R: return 1;
                case DataProperty.G: return 1;
                case DataProperty.B: return 1;
                case DataProperty.A: return 1;
                //case DataProperty.Size: return 4;
                case DataProperty.Data8: return 1;
                case DataProperty.Data16: return 2;
                case DataProperty.Data32: return 4;
            }
            return 0;
        }

        class DataHeader
        {
            public List<DataProperty> properties = new List<DataProperty>();
            public int vertexCount = -1;
        }

        class DataBody
        {
            public List<Vector3> vertices;
            public List<Color32> colors;
            public List<Vector2> uv;
            public static Vector3[] vertArr;

            public DataBody(int vertexCount)
            {
                vertices = new List<Vector3>(vertexCount);
                colors = new List<Color32>(vertexCount);
                uv = new List<Vector2>(vertexCount);
            }

            public void AddPoint(
                float x, float y, float z,
                byte r, byte g, byte b, byte a
            )
            {
                vertices.Add(new Vector3(x, y, z));
                colors.Add(new Color32(r, g, b, a));
                //uv.Add(new Vector2(size, size));
            }

            public void CalculatePointSize(int vertexCount)
            {
                vertArr = vertices.ToArray();
                KDTree tree = KDTree.MakeFromPoints(vertArr);
                float[] pointSize = new float[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    int[] indices = tree.FindNearestsK(vertArr[i], 5);
                    float[] dists = new float[5];
                    for (int j = 0; j < 5; j++)
                    {
                        dists[j] = Vector3.Distance(vertArr[i], vertArr[indices[j]]);
                    }
                    pointSize[i] = dists.Average();
                }
                float globalMean = pointSize.Average();
                float globalSTD = Mathf.Sqrt(pointSize.Select(val => (val - globalMean) * (val - globalMean)).Sum() / vertexCount);
                for (int i = 0; i < vertexCount; i++)
                {
                    if (pointSize[i] > globalMean + 3f * globalSTD || pointSize[i] < globalMean - 3f * globalSTD)
                    {
                        pointSize[i] = globalMean;
                    }
                    uv.Add(new Vector2(pointSize[i], pointSize[i]));
                }

            }
        }

        #endregion

        #region Reader implementation

        Mesh ImportAsMesh(string path)
        {
            try
            {
                var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var header = ReadDataHeader(new StreamReader(stream));
                var body = ReadDataBody(header, new BinaryReader(stream));
                //this.vertexCount = header.vertexCount;

                var mesh = new Mesh();
                mesh.name = Path.GetFileNameWithoutExtension(path);

                mesh.indexFormat = header.vertexCount > 65535 ?
                    IndexFormat.UInt32 : IndexFormat.UInt16;

                mesh.SetVertices(body.vertices);
                mesh.SetColors(body.colors);
                mesh.SetUVs(0, body.uv);

                mesh.SetIndices(
                    Enumerable.Range(0, header.vertexCount).ToArray(),
                    MeshTopology.Points, 0
                );

                mesh.UploadMeshData(true);
                return mesh;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed importing " + path + ". " + e.Message);
                return null;
            }
        }

        PointCloudData ImportAsPointCloudData(string path)
        {
            try
            {
                var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var header = ReadDataHeader(new StreamReader(stream));
                var body = ReadDataBody(header, new BinaryReader(stream));
                var data = ScriptableObject.CreateInstance<PointCloudData>();
                data.Initialize(body.vertices, body.colors);
                data.name = Path.GetFileNameWithoutExtension(path);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed importing " + path + ". " + e.Message);
                return null;
            }
        }

        DataHeader ReadDataHeader(StreamReader reader)
        {
            var data = new DataHeader();
            var readCount = 0;

            // Magic number line ("ply")
            var line = reader.ReadLine();
            readCount += line.Length + 1;
            if (line != "ply")
                throw new ArgumentException("Magic number ('ply') mismatch.");

            // Data format: check if it's binary/little endian.
            line = reader.ReadLine();
            readCount += line.Length + 1;
            if (line != "format binary_little_endian 1.0")
                throw new ArgumentException(
                    "Invalid data format ('" + line + "'). " +
                    "Should be binary/little endian.");

            // Read header contents.
            for (var skip = false;;)
            {
                // Read a line and split it with white space.
                line = reader.ReadLine();
                readCount += line.Length + 1;
                if (line == "end_header") break;
                var col = line.Split();

                // Element declaration (unskippable)
                if (col[0] == "element")
                {
                    if (col[1] == "vertex")
                    {
                        data.vertexCount = Convert.ToInt32(col[2]);
                        skip = false;
                    }
                    else
                    {
                        // Don't read elements other than vertices.
                        skip = true;
                    }
                }

                if (skip) continue;

                // Property declaration line
                if (col[0] == "property")
                {
                    var prop = DataProperty.Invalid;

                    // Parse the property name entry.
                    switch (col[2])
                    {
                        case "x"    : prop = DataProperty.X; break;
                        case "y"    : prop = DataProperty.Y; break;
                        case "z"    : prop = DataProperty.Z; break;
                        case "red"  : prop = DataProperty.R; break;
                        case "green": prop = DataProperty.G; break;
                        case "blue" : prop = DataProperty.B; break;
                        case "alpha": prop = DataProperty.A; break;
                        //case "intensity" : prop = DataProperty.Size; break;
                    }

                    // Check the property type.
                    if (col[1] == "char" || col[1] == "uchar")
                    {
                        if (prop == DataProperty.Invalid)
                            prop = DataProperty.Data8;
                        else if (GetPropertySize(prop) != 1)
                            throw new ArgumentException("Invalid property type ('" + line + "').");
                    }
                    else if (col[1] == "short" || col[1] == "ushort")
                    {
                        if (prop == DataProperty.Invalid)
                            prop = DataProperty.Data16;
                        else if (GetPropertySize(prop) != 2)
                            throw new ArgumentException("Invalid property type ('" + line + "').");
                    }
                    else if (col[1] == "int" || col[1] == "uint" || col[1] == "float")
                    {
                        if (prop == DataProperty.Invalid)
                            prop = DataProperty.Data32;
                        else if (GetPropertySize(prop) != 4)
                            throw new ArgumentException("Invalid property type ('" + line + "').");
                    }
                    else
                    {
                        throw new ArgumentException("Unsupported property type ('" + line + "').");
                    }

                    data.properties.Add(prop);
                }
            }

            // Rewind the stream back to the exact position of the reader.
            reader.BaseStream.Position = readCount;

            return data;
        }

        DataBody ReadDataBody(DataHeader header, BinaryReader reader)
        {
            var data = new DataBody(header.vertexCount);

            float x = 0, y = 0, z = 0;
            Byte r = 255, g = 255, b = 255, a = 255;

            for (var i = 0; i < header.vertexCount; i++)
            {
                foreach (var prop in header.properties)
                {
                    switch (prop)
                    {
                        case DataProperty.X: x = reader.ReadSingle(); break;
                        case DataProperty.Y: y = reader.ReadSingle(); break;
                        case DataProperty.Z: z = reader.ReadSingle(); break;

                        case DataProperty.R: r = reader.ReadByte(); break;
                        case DataProperty.G: g = reader.ReadByte(); break;
                        case DataProperty.B: b = reader.ReadByte(); break;
                        case DataProperty.A: a = reader.ReadByte(); break;

                        //case DataProperty.Size: size = reader.ReadSingle(); break;
                        case DataProperty.Data8: reader.ReadByte(); break;
                        case DataProperty.Data16: reader.BaseStream.Position += 2; break;
                        case DataProperty.Data32: reader.BaseStream.Position += 4; break;
                    }
                }

                data.AddPoint(x, y, z, r, g, b, a);
                
            }
            data.CalculatePointSize(header.vertexCount);

            return data;
        }

        

        void FitTransform(MeshRenderer mr, Transform transform, Vector3 desiredSize, ref Vector3[] fittedVertCal)
        {
            Bounds b = mr.bounds;
            Vector3 originalSize = b.extents *2f; 
            
            float scale = Mathf.Min(desiredSize.x / originalSize.x, desiredSize.y / originalSize.y, desiredSize.z / originalSize.z);
            float desiredSurface = desiredSize.x * desiredSize.y + desiredSize.y * desiredSize.z + desiredSize.z * desiredSize.x;
            float boundingSurface = (originalSize.x * originalSize.y + originalSize.x*originalSize.z+originalSize.z * originalSize.y) * scale* scale;
            float diff = 0.1f;
            while (Mathf.Abs(boundingSurface- desiredSurface) > 0.01f)
            {
                scale += diff;
                bool dir = (boundingSurface < desiredSurface);
                boundingSurface = (originalSize.x * originalSize.y + originalSize.x * originalSize.z + originalSize.z * originalSize.y) * scale * scale;
                if ((boundingSurface < desiredSurface) != dir)
                {
                    diff *= -0.1f;
                }
            }
            transform.localScale = Vector3.one * scale;
            Bounds boundNew = mr.bounds;
            if(boundNew.extents.y*2f > 1.75f)
            {
                scale *= 1.75f / (boundNew.extents.y * 2f);
            }
            transform.localScale = Vector3.one * scale;
            Vector3 middlePos = b.center;
            Vector3 offset = (transform.position - middlePos) * scale;
            transform.position = offset + Vector3.up * mr.bounds.extents.y;

            Bounds bound = mr.bounds;


            for (int i = 0; i < fittedVertCal.Length; i++)
            {
                fittedVertCal[i] *= scale;
                fittedVertCal[i] += (offset + Vector3.up * mr.bounds.extents.y);
                if (bound.center.y < centerHeight - 0.1f)
                {
                    float off = centerHeight - transform.position.y;
                    fittedVertCal[i] += Vector3.up * off;
                }
            }
        }

        void CreateShadowTexture(Vector2[] normShadowCoord, string name)
        {
            Texture2D tex = new Texture2D(512, 512);
            Color[,] colors = new Color[512,512];
            for(int i = 0; i < 512; i++)
            {
                for (int j = 0; j < 512; j++)
                {
                    colors[i, j] = Color.white;
                }
            }


            for(int i = 0; i < normShadowCoord.Length; i++)
            {

                Vector2Int pixelCoord = new Vector2Int((int)(normShadowCoord[i].x * 512f), (int)(normShadowCoord[i].y*512f));

                for(int m = -3; m <= 3; m++)
                {
                    for(int n = -3; n <= 3; n++)
                    {
                        int pixX = Mathf.Clamp(pixelCoord.x+m, 0, 511);
                        int pixY = Mathf.Clamp(pixelCoord.y+n, 0, 511);
                        Color col = colors[pixX, pixY];
                        float red = Mathf.Clamp(col.r - 0.1f, 0.6f, 1f);
                        float gre = Mathf.Clamp(col.g - 0.1f, 0.6f, 1f);
                        float blu = Mathf.Clamp(col.b - 0.1f, 0.6f, 1f);
                        colors[pixX, pixY] = new Color(red, gre, blu);
                    }
                }

                for (int m = -2; m <= 2; m++)
                {
                    for (int n = -2; n <= 2; n++)
                    {
                        int pixX = Mathf.Clamp(pixelCoord.x + m, 0, 511);
                        int pixY = Mathf.Clamp(pixelCoord.y + n, 0, 511);
                        Color col = colors[pixX, pixY];
                        float red = Mathf.Clamp(col.r - 0.1f, 0.6f, 1f);
                        float gre = Mathf.Clamp(col.g - 0.1f, 0.6f, 1f);
                        float blu = Mathf.Clamp(col.b - 0.1f, 0.6f, 1f);
                        colors[pixX, pixY] = new Color(red, gre, blu);
                    }
                }
                for (int m = -1; m <= 1; m++)
                {
                    for (int n = -1; n <= 1; n++)
                    {
                        int pixX = Mathf.Clamp(pixelCoord.x + m, 0, 511);
                        int pixY = Mathf.Clamp(pixelCoord.y + n, 0, 511);
                        Color col = colors[pixX, pixY];
                        float red = Mathf.Clamp(col.r - 0.1f, 0.6f, 1f);
                        float gre = Mathf.Clamp(col.g - 0.1f, 0.6f, 1f);
                        float blu = Mathf.Clamp(col.b - 0.1f, 0.6f, 1f);
                        colors[pixX, pixY] = new Color(red, gre, blu);
                    }
                }

                int pixxX = Mathf.Clamp(pixelCoord.x, 0, 511);
                int pixxY = Mathf.Clamp(pixelCoord.y, 0, 511);
                Color c = colors[pixxX, pixxY];
                float r = Mathf.Clamp(c.r - 0.1f, 0.7f, 1f);
                float g = Mathf.Clamp(c.g - 0.1f, 0.7f, 1f);
                float b = Mathf.Clamp(c.b - 0.1f, 0.7f, 1f);
                colors[pixxX, pixxY] = new Color(r, g, b);
                
            }

            for (int i = 0; i < 512; i++)
            {
                for (int j = 0; j < 512; j++)
                {
                    tex.SetPixel(i, j, colors[i, j]);
                }
            }
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            //UnityEngine.Object.De(tex);
            File.WriteAllBytes(Application.dataPath + "/Resources/Materials/" + name + "_shadowTexture.png", bytes);
        }

        Vector2[] CreateNormalisedShadowCoordinates(Vector3 lightPos, Vector3[] fittedVerts, out Vector2 quadPos, out Vector2 quadScale)
        {
            List<Vector2> normalisedShadowCoords = new List<Vector2>();
            Plane plane = new Plane(Vector3.up, Vector3.zero);

            float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;

            for(int i = 0; i < fittedVerts.Length; i++)
            {
                Vector3 lightDir = fittedVerts[i] - lightPos;
                Ray ray = new Ray(lightPos, lightDir);
                float enter;
                if(plane.Raycast(ray, out enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    if(hitPoint.x < xMin)
                    {
                        xMin = hitPoint.x;
                    }
                    if (hitPoint.x > xMax)
                    {
                        xMax = hitPoint.x;
                    }
                    if (hitPoint.z < yMin)
                    {
                        yMin = hitPoint.z;
                    }
                    if (hitPoint.z > yMax)
                    {
                        yMax = hitPoint.z;
                    }
                    normalisedShadowCoords.Add(new Vector2(hitPoint.x, hitPoint.z));
                } 
            }

            quadPos = new Vector2((xMin + xMax) / 2f, (yMin + yMax) / 2f);
            quadScale = new Vector2(xMax-xMin, yMax-yMin);

            for(int i = 0; i < normalisedShadowCoords.Count; i++)
            {
                normalisedShadowCoords[i] -=  quadPos;
                normalisedShadowCoords[i] = new Vector2(normalisedShadowCoords[i].x / quadScale.x, normalisedShadowCoords[i].y / quadScale.y);
                normalisedShadowCoords[i] += new Vector2(0.5f, 0.5f);
            }

            return normalisedShadowCoords.ToArray();
        }
        
    }

    #endregion
}
