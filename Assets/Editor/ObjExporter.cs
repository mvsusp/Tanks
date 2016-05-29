using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public class ObjExporterScript
    {
        private static int _startIndex;

        public static void Start()
        {
            _startIndex = 0;
        }
        public static void End()
        {
            _startIndex = 0;
        }


        public static string MeshToString(MeshFilter mf, Transform t)
        {
            var s = t.localScale;
            var p = t.localPosition;
            var r = t.localRotation;


            var numVertices = 0;
            var m = mf.sharedMesh;
            if (!m)
            {
                return "####Error####";
            }
            var mats = mf.GetComponent<MeshRenderer>().sharedMaterials;

            var sb = new StringBuilder();

            foreach (var vv in m.vertices)
            {
                var v = t.TransformPoint(vv);
                numVertices++;
                sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, -v.z));
            }
            sb.Append("\n");
            foreach (var nn in m.normals)
            {
                var v = r * nn;
                sb.Append(string.Format("vn {0} {1} {2}\n", -v.x, -v.y, v.z));
            }
            sb.Append("\n");
            foreach (var v in m.uv)
            {
                sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
            }
            for (var material = 0; material < m.subMeshCount; material++)
            {
                sb.Append("\n");
                sb.Append("usemtl ").Append(mats[material].name).Append("\n");
                sb.Append("usemap ").Append(mats[material].name).Append("\n");

                var triangles = m.GetTriangles(material);
                for (var i = 0; i < triangles.Length; i += 3)
                {
                    sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                        triangles[i] + 1 + _startIndex, triangles[i + 1] + 1 + _startIndex, triangles[i + 2] + 1 + _startIndex));
                }
            }

            _startIndex += numVertices;
            return sb.ToString();
        }
    }

    public class ObjExporter : ScriptableObject
    {
        [MenuItem("File/Export/Wavefront OBJ")]
        static void DoExportWSubmeshes()
        {
            DoExport(true);
        }

        [MenuItem("File/Export/Wavefront OBJ (No Submeshes)")]
        static void DoExportWOSubmeshes()
        {
            DoExport(false);
        }


        static void DoExport(bool makeSubmeshes)
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.Log("Didn't Export Any Meshes; Nothing was selected!");
                return;
            }

            var meshName = Selection.gameObjects[0].name;
            var fileName = EditorUtility.SaveFilePanel("Export .obj file", "", meshName, "obj");

            ObjExporterScript.Start();

            var meshString = new StringBuilder();

            meshString.Append("#" + meshName + ".obj"
                              + "\n#" + System.DateTime.Now.ToLongDateString()
                              + "\n#" + System.DateTime.Now.ToLongTimeString()
                              + "\n#-------"
                              + "\n\n");

            var t = Selection.gameObjects[0].transform;

            var originalPosition = t.position;
            t.position = Vector3.zero;

            if (!makeSubmeshes)
            {
                meshString.Append("g ").Append(t.name).Append("\n");
            }
            meshString.Append(processTransform(t, makeSubmeshes));

            WriteToFile(meshString.ToString(), fileName);

            t.position = originalPosition;

            ObjExporterScript.End();
            Debug.Log("Exported Mesh: " + fileName);
        }

        static string processTransform(Transform t, bool makeSubmeshes)
        {
            var meshString = new StringBuilder();

            meshString.Append("#" + t.name
                              + "\n#-------"
                              + "\n");

            if (makeSubmeshes)
            {
                meshString.Append("g ").Append(t.name).Append("\n");
            }

            var mf = t.GetComponent<MeshFilter>();
            if (mf)
            {
                meshString.Append(ObjExporterScript.MeshToString(mf, t));
            }

            for (int i = 0; i < t.childCount; i++)
            {
                meshString.Append(processTransform(t.GetChild(i), makeSubmeshes));
            }

            return meshString.ToString();
        }

        static void WriteToFile(string s, string filename)
        {
            using (var sw = new StreamWriter(filename))
            {
                sw.Write(s);
            }
        }
    }
}