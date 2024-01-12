using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;

using Pair = System.Tuple<int, int>;
using Rhino.Geometry.Collections;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using Rhino;

namespace GmshCommon
{
    public static class GeometryExtensions
    {
        /// <summary>
        /// Returns a Rhino mesh from Gmsh 2D elements. Gmsh must be initialized beforehand.
        /// </summary>
        /// <returns>A mesh.</returns>
        public static Mesh GetMeshOld(int dim = 3, int tag = -1)
        {
            // Get a mesh out
            IntPtr[] nodeTags;
            double[] coords;
            Gmsh.Model.Mesh.GetNodes(out nodeTags, out coords, dim, tag, true, false);

            if (nodeTags == null || nodeTags.Length < 1) throw new Exception("Bad nodeTags in GetMesh()");

            var nodes = new Point3d[nodeTags.Length + 1];

            for (int i = 0; i < nodeTags.Length; ++i)
            {
                var pt = new Point3d(coords[i * 3], coords[i * 3 + 1], coords[i * 3 + 2]);
                nodes[(int)nodeTags[i]] = pt;
            }

            int[] elementTypes;
            IntPtr[][] elementTags, enodeTags;


            Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out enodeTags, 2, tag);

            var mesh = new Mesh();

            mesh.Vertices.AddVertices(nodes);

            for (int i = 0; i < elementTypes.Length; ++i)
            {
                if (elementTypes[i] == 2)
                {

                    for (int j = 0; j < enodeTags[i].Length; j += 3)
                    {
                        mesh.Faces.AddFace(
                          (int)enodeTags[0][j],
                          (int)enodeTags[0][j + 1],
                          (int)enodeTags[0][j + 2]);
                    }
                }
                else if (elementTypes[i] == 3)
                {

                    for (int j = 0; j < enodeTags[i].Length; j += 4)
                    {
                        mesh.Faces.AddFace(
                          (int)enodeTags[0][j],
                          (int)enodeTags[0][j + 1],
                          (int)enodeTags[0][j + 2],
                          (int)enodeTags[0][j + 3]);
                    }
                }
                else
                {
                    continue;
                }
            }

            mesh.Compact();
            mesh.RebuildNormals();
            mesh.Unweld(0.1, true);

            return mesh;
        }

        public static Mesh GetMesh(int dim = 3, int tag = -1)
        {
            return GetMesh(new Pair(dim, tag));
        }

        public static Mesh GetMesh(Pair dimTag)
        {
            return GetMesh(new Pair[] { dimTag });
        }

        public static Mesh GetMesh(Pair[] dimTags)
        {

            List<Pair> dimTagList = new List<Pair>();

            foreach (var dimTag in dimTags)
            {
                if (dimTag.Item1 == 3)
                {
                    Pair[] boundaries;
                    Gmsh.Model.GetBoundary(new Tuple<int, int>[] { dimTag }, out boundaries, true, false, false);
                    dimTagList.AddRange(boundaries);
                }
                else
                    dimTagList.Add(dimTag);
            }

            if (dimTagList.Count < 1)
            {
                throw new Exception("No valid entities present.");
            }

            var nodes = new Dictionary<int, Point3d>();
            var indexMap = new Dictionary<int, int>();

            int totalNodes = 0;
            foreach (var dimTag in dimTagList)
            {
                IntPtr[] nodeTags;
                double[] coords;
                Gmsh.Model.Mesh.GetNodes(out nodeTags, out coords, 2, dimTag.Item2, true, false);

                if (nodeTags == null || nodeTags.Length < 1) continue;

                for (int i = 0; i < nodeTags.Length; ++i)
                {
                    indexMap[(int)nodeTags[i]] = totalNodes + i;
                    nodes[totalNodes + i] = new Point3d(
                      coords[i * 3], coords[i * 3 + 1], coords[i * 3 + 2]);
                }

                totalNodes += nodeTags.Length;
            }

            if (totalNodes < 1) throw new Exception("Couldn't get any nodes!");

            var mesh = new Mesh();
            mesh.Vertices.AddVertices(nodes.Values);

            foreach (var dimTag in dimTagList)
            {
                int[] elementTypes;
                IntPtr[][] elementTags, enodeTags;

                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out enodeTags, 2, dimTag.Item2);

                for (int i = 0; i < elementTypes.Length; ++i)
                {
                    if (elementTypes[i] == 2)
                    {

                        for (int j = 0; j < enodeTags[i].Length; j += 3)
                        {
                            mesh.Faces.AddFace(
                              indexMap[(int)enodeTags[i][j]],
                              indexMap[(int)enodeTags[i][j + 1]],
                              indexMap[(int)enodeTags[i][j + 2]]);
                        }
                    }
                    else if (elementTypes[i] == 3)
                    {

                        for (int j = 0; j < enodeTags[i].Length; j += 4)
                        {
                            mesh.Faces.AddFace(
                              indexMap[(int)enodeTags[i][j]],
                              indexMap[(int)enodeTags[i][j + 1]],
                              indexMap[(int)enodeTags[i][j + 2]],
                              indexMap[(int)enodeTags[i][j + 3]]);
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            mesh.Compact();
            mesh.RebuildNormals();
            mesh.Unweld(0.2, true);

            return mesh;
        }

        public static void KnotsToOCC(IEnumerable<double> knotsOriginal, int degree, out double[] knotsOcc, out int[] multsOcc)
        {
            var knotList = knotsOriginal.ToList();

            double lastKnot = knotList[0];
            double currentKnot = knotList[1];
            int mult = 1;

            var knots = new List<double>();
            var mults = new List<int>();

            var epsilon = 1e-6;

            for (int i = 1; i < knotList.Count; ++i)
            {
                currentKnot = knotList[i];
                if (Math.Abs(lastKnot - currentKnot) > epsilon)
                {
                    knots.Add(lastKnot);
                    mults.Add(mult);

                    mult = 1;
                    lastKnot = currentKnot;
                }
                else
                {
                    mult++;
                }
            }

            knots.Add(currentKnot);
            mults.Add(mult);

            mults[0] = degree + 1;
            mults[mults.Count - 1] = degree + 1;

            knotsOcc = knots.ToArray();
            multsOcc = mults.ToArray();
        }
        /*
        public static void KnotsToOCC(NurbsCurveKnotList knotsOriginal, int degree, out double[] knotsOcc, out int[] multsOcc)
        {
            double lastKnot = knotsOriginal[0];
            double currentKnot = knotsOriginal[1];
            int mult = 1;

            var knots = new List<double>();
            var mults = new List<int>();

            var epsilon = 1e-6;

            for (int i = 1; i < knotsOriginal.Count; ++i)
            {
                currentKnot = knotsOriginal[i];
                if (Math.Abs(lastKnot - currentKnot) > epsilon)
                {
                    knots.Add(lastKnot);
                    mults.Add(mult);

                    mult = 1;
                    lastKnot = currentKnot;
                }
                else
                {
                    mult++;
                }
            }

            knots.Add(currentKnot);
            mults.Add(mult);

            mults[0] = degree + 1;
            mults[mults.Count - 1] = degree + 1;

            knotsOcc = knots.ToArray();
            multsOcc = mults.ToArray();
        }
        */

        public static int AddBSpline(NurbsCurve bspline)
        {
            var pointTags = new List<int>();
            var weights = new List<double>();

            int start = 0;
            int end = bspline.Points.Count;

            if (bspline.IsPeriodic)
            {
                //start = bspline.Degree - 1;
                end = bspline.Points.Count - bspline.Degree;
            }

            for (int i = start; i < end; ++i)
            {
                var cpt = bspline.Points[i];

                pointTags.Add(Gmsh.Model.OCC.AddPoint(cpt.Location.X, cpt.Location.Y, cpt.Location.Z));
                weights.Add(cpt.Weight);
            }

            if (bspline.IsClosed)
            {
                //pointTags[pointTags.Count - 1] = pointTags[0];
            }


            double[] knots; int[] multiplicities;
            KnotsToOCC(bspline.Knots, bspline.Degree, out knots, out multiplicities);

            return Gmsh.Model.OCC.AddBSpline(pointTags.ToArray(), -1, bspline.Degree, weights.ToArray(), knots, multiplicities);
        }

        public static int AddBSplineSurface(Surface srf)
        {
            var pts = new List<Point3d>();

            var bsrf = srf.ToNurbsSurface();

            var pointTags = new List<int>();
            var weights = new List<double>();

            for (int v = 0; v < bsrf.Points.CountV; ++v)
            {
                for (int u = 0; u < bsrf.Points.CountU; ++u)
                {
                    ControlPoint cpt = bsrf.Points.GetControlPoint(u, v);

                    var cptLoc = cpt.Location;

                    var pTag = Gmsh.Model.OCC.AddPoint(cptLoc.X, cptLoc.Y, cptLoc.Z);
                    pointTags.Add(pTag);

                    Gmsh.Model.OCC.Synchronize();
                    weights.Add(cpt.Weight);
                    pts.Add(cpt.Location);
                }
            }

            if (bsrf.IsClosed(0))
            {
                for (int i = 0; i < bsrf.Points.CountV; ++i)
                {
                    //pointTags[i * bsrf.Points.CountU] = pointTags[(i + 1) * bsrf.Points.CountU - 1];
                }
            }

            if (bsrf.IsClosed(1))
            {
                for (int i = 0; i < bsrf.Points.CountU; ++i)
                {
                    //pointTags[i * bsrf.Points.CountV] = pointTags[(i + 1) * bsrf.Points.CountV - 1];
                }
            }

            double[] knotsU, knotsV;
            int[] multsU, multsV;

            KnotsToOCC(bsrf.KnotsU, bsrf.Degree(0), out knotsU, out multsU);
            KnotsToOCC(bsrf.KnotsV, bsrf.Degree(1), out knotsV, out multsV);

            return Gmsh.Model.OCC.AddBSplineSurface(pointTags.ToArray(), bsrf.Points.CountU, -1, bsrf.Degree(0), bsrf.Degree(1),
              weights.ToArray(), knotsU, knotsV, multsU, multsV);
        }

        public static int AddBrep(Brep brep, bool heal = true)
        {
            List<int> faces = new List<int>();
            return AddBrep(brep, faces, heal);
        }


        public static int AddBrep(Brep brep, List<int> faces, bool heal = true)
        {
            var verts = new List<int>();
            var edges = new List<int>();
            var curves3d = new List<int>();
            var loops = new List<int>();
            var trims = new List<int>();
            var surfaces = new List<int>();
            //var faces = new List<int>();

            foreach (BrepVertex vertex in brep.Vertices)
            {
                //Gmsh.OCC.AddPoint(vertex.Location.X, vertex.Location.Y, vertex.Location.Z, -1);
            }

            foreach (var edge in brep.Edges)
            {
                var crv = edge.EdgeCurve;
                var bspline = crv.ToNurbsCurve();
                curves3d.Add(AddBSpline(bspline));
            }

            foreach (BrepTrim trim in brep.Trims)
            {
                var crv = trim.TrimCurve;
                var bspline = crv.ToNurbsCurve();
                trims.Add(AddBSpline(bspline));
            }

            foreach (Surface srf in brep.Surfaces)
            {
                surfaces.Add(AddBSplineSurface(srf));
                Gmsh.Model.OCC.Synchronize();
            }

            foreach (BrepFace face in brep.Faces)
            {
                var wires = new List<int>();

                foreach (BrepLoop loop in face.Loops)
                {
                    var wire = new List<int>();
                    var test = new List<int>();

                    foreach (BrepTrim trim in loop.Trims)
                    {

                        //wire.Add(trims[trim.TrimIndex]);
                        //wire.Add(curves3d[trim.Edge.EdgeCurveIndex]);
                        //test.Add(trim.Edge.EdgeCurveIndex);

                        if (brep.Surfaces[face.SurfaceIndex] is PlaneSurface && false)
                        {
                            wire.Add(AddBSpline(trim.Edge.ToNurbsCurve()));
                        }
                        else
                        {
                            wire.Add(AddBSpline(trim.ToNurbsCurve()));
                        }
                    }

                    var wireTag = Gmsh.Model.OCC.AddWire(wire.ToArray(), -1, true);

                    wires.Add(wireTag);
                }

                if (brep.Surfaces[face.SurfaceIndex] is NurbsSurface || true)
                {
                    faces.Add(Gmsh.Model.OCC.AddTrimmedSurface(surfaces[face.SurfaceIndex], wires.ToArray(), false));
                    //faces.Add(surfaces[face.SurfaceIndex]);
                }
                else if (brep.Surfaces[face.SurfaceIndex] is PlaneSurface)
                {
                    faces.Add(Gmsh.Model.OCC.AddPlaneSurface(wires.ToArray()));
                }

                Gmsh.Model.OCC.Synchronize();
            }

            Gmsh.Model.OCC.Remove(surfaces.Select(x => new Pair(2, x)).ToArray(), true);
            Gmsh.Model.OCC.Synchronize();

            Gmsh.Model.OCC.Remove(curves3d.Select(x => new Pair(1, x)).ToArray(), true);
            Gmsh.Model.OCC.Synchronize();

            Gmsh.Model.OCC.Remove(verts.Select(x => new Pair(0, x)).ToArray(), true);
            Gmsh.Model.OCC.Synchronize();

            var faceLoop = Gmsh.Model.OCC.AddSurfaceLoop(faces.ToArray());

            int volume = -1;

            if (brep.IsSolid)
            {
                volume = Gmsh.Model.OCC.AddVolume(new int[] { faceLoop });
                Gmsh.Model.OCC.Synchronize();

            }
            //RhinoApp.WriteLine("Volume tag: {0}", volume);

            if (heal)
            {
                Pair[] outDimTags;
                Pair[] dimTags = new Pair[] { new Pair(3, volume) };

                // HealShapes returns alllll the shapes in the model, even if dimTags is populated...
                Gmsh.Model.OCC.HealShapes(out outDimTags, dimTags, 1e-06, true, true, true, true, true);

                // So assigning the result is meaningless. We assume that the tag doesn't change.
                //volume = outDimTags[0].Item2;
            }

            Gmsh.Model.OCC.Synchronize();

            return volume;
        }

        /// <summary>
        /// Get mesh for specific physical group.
        /// </summary>
        /// <param name="dim">Dimensions of physical group.</param>
        /// <param name="tag">Tag of physical group.</param>
        /// <returns></returns>
        public static Mesh GetMeshForPhysicalGroup(int dim, int tag)
        {
            IntPtr[] nodeTags;
            double[] coords;
            Gmsh.Model.Mesh.GetNodes(out nodeTags, out coords, 3, -1, true, false);

            var nodes = new Point3d[nodeTags.Length + 1];

            for (int i = 0; i < nodeTags.Length; ++i)
            {
                var pt = new Point3d(coords[i * 3], coords[i * 3 + 1], coords[i * 3 + 2]);
                nodes[(int)nodeTags[i]] = pt;
            }

            int[] elementTypes;
            IntPtr[][] elementTags, enodeTags;

            var entities = Gmsh.Model.GetEntitiesForPhysicalGroup(dim, tag);

            var mesh = new Mesh();
            mesh.Vertices.AddVertices(nodes);

            foreach (int ent in entities)
            {
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out enodeTags, dim, ent);

                for (int i = 0; i < elementTypes.Length; ++i)
                {
                    if (elementTypes[i] == 2)
                    {

                        for (int j = 0; j < enodeTags[i].Length; j += 3)
                        {
                            mesh.Faces.AddFace(
                              (int)enodeTags[0][j],
                              (int)enodeTags[0][j + 1],
                              (int)enodeTags[0][j + 2]);
                        }
                    }
                    else if (elementTypes[i] == 3)
                    {

                        for (int j = 0; j < enodeTags[i].Length; j += 4)
                        {
                            mesh.Faces.AddFace(
                              (int)enodeTags[0][j],
                              (int)enodeTags[0][j + 1],
                              (int)enodeTags[0][j + 2],
                              (int)enodeTags[0][j + 3]);
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            mesh.Compact();
            mesh.RebuildNormals();
            mesh.Unweld(0.1, true);

            return mesh;
        }

        /// <summary>
        /// Transfers a Rhino mesh to Gmsh as a 2D surface entity. 
        /// </summary>
        /// <param name="mesh">Surface mesh to be volumetrically meshed.</param>
        /// <param name="create_geometry">Sometimes the reconstruction of geometry fails, in which case this settings needs to be toggled.</param>
        public static int TransferMesh(Mesh mesh, bool create_geometry = false)
        {
            var entity = Gmsh.Model.AddDiscreteEntity(2, -1);

            mesh = mesh.DuplicateMesh();
            mesh.Weld(Math.PI);

            mesh.Faces.ConvertQuadsToTriangles();
            mesh.RebuildNormals();
            mesh.Compact();

            double[] coords = new double[mesh.Vertices.Count * 3];
            var tags = new IntPtr[mesh.Vertices.Count];
            for (int i = 0; i < tags.Length; ++i)
                tags[i] = (IntPtr)i + 1;

            for (int i = 0; i < mesh.Vertices.Count; ++i)
            {
                coords[i * 3 + 0] = mesh.Vertices[i].X;
                coords[i * 3 + 1] = mesh.Vertices[i].Y;
                coords[i * 3 + 2] = mesh.Vertices[i].Z;
            }

            Gmsh.Model.Mesh.AddNodes(2, entity, tags, coords);

            var faces3 = new IntPtr[mesh.Faces.TriangleCount];
            var faces4 = new IntPtr[mesh.Faces.QuadCount];

            var faceindices3 = new IntPtr[mesh.Faces.TriangleCount * 3];
            var faceindices4 = new IntPtr[mesh.Faces.QuadCount * 4];

            int i3 = 0, i4 = 0;

            for (int i = 0; i < mesh.Faces.Count; ++i)
            {
                var mf = mesh.Faces[i];

                if (mf.IsQuad)
                {
                    faces4[i4] = (IntPtr)i + 1;
                    faceindices4[i4 * 4] = (IntPtr)mf.A + 1;
                    faceindices4[i4 * 4 + 1] = (IntPtr)mf.B + 1;
                    faceindices4[i4 * 4 + 2] = (IntPtr)mf.C + 1;
                    faceindices4[i4 * 4 + 3] = (IntPtr)mf.D + 1;
                    i4++;
                }
                else
                {
                    faces3[i3] = (IntPtr)i + 1;
                    faceindices3[i3 * 3] = (IntPtr)mf.A + 1;
                    faceindices3[i3 * 3 + 1] = (IntPtr)mf.B + 1;
                    faceindices3[i3 * 3 + 2] = (IntPtr)mf.C + 1;
                    i3++;
                }
            }

            var elementTypes = new List<int>();
            var elementTags = new List<IntPtr[]>();
            var nodeTags = new List<IntPtr[]>();
            if (faces3.Length > 0)
            {
                elementTypes.Add(2);
                elementTags.Add(faces3);
                nodeTags.Add(faceindices3);
            }

            if (faces4.Length > 0)
            {
                elementTypes.Add(3);
                elementTags.Add(faces4);
                nodeTags.Add(faceindices4);
            }

            Gmsh.Model.Mesh.AddElements(2, entity, elementTypes.ToArray(),
              elementTags.ToArray(),
              nodeTags.ToArray());

            Gmsh.Model.Mesh.CreateTopology(true, true);
            Gmsh.Model.Mesh.ClassifySurfaces(0.1, true, true, 0.1, true);

            if (create_geometry)
                Gmsh.Model.Mesh.CreateGeometry();

            return entity;
        }

        public static List<Point3d> GetCentroids()
        {
            var centroids = new List<Point3d>();
            try
            {
                int[] elementTypes;
                IntPtr[][] elementTags, enodeTags;
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out enodeTags, 3, -1);

                for (int i = 0; i < elementTypes.Length; ++i)
                {
                    for (int j = 0; j < elementTags[i].Length; ++j)
                    {
                        var centroid = GmshCommon.Utility.GetCentroid(elementTags[i][j], elementTypes[i]);
                        centroids.Add(new Point3d(centroid[0], centroid[1], centroid[2]));
                    }
                }
            }
            catch (Exception e)
            {
                string msg = Gmsh.Logger.GetLastError();

                var log = Gmsh.Logger.Get();
                foreach (string l in log)
                    msg += String.Format("\n{0}", log);

                throw new Exception(msg);
            }

            return centroids;
        }

        /// <summary>
        /// Fragment the model and return the map of children entities.
        /// </summary>
        /// <param name="dimTags">Array of (dim, tag) pairs to include in the fragmentation.</param>
        /// <returns>A dictionary that maps the input entities with the new or modified output entities.</returns>
        public static Dictionary<Pair, List<Pair>> Fragment(Pair[] dimTags, out Pair[] dimTagsOut, out Pair[][] dimTagsMap)
        {
            if (dimTags.Length < 2)
            {
                dimTagsOut = new Pair[0];
                dimTagsMap = new Pair[0][];
                return null;
            }

            Array.Sort(dimTags); // Important for putting the lower dimension entities before higher ones

            Gmsh.Option.SetNumber("Geometry.OCCBooleanPreserveNumbering", 1);

            var obj = new Pair[] { dimTags[0] };
            var tools = new Pair[dimTags.Length - 1];
            Array.Copy(dimTags, 1, tools, 0, tools.Length);

            Gmsh.Model.OCC.Fragment(obj, tools, out dimTagsOut, out dimTagsMap, true, true);
            Gmsh.Model.OCC.Synchronize();


            var all = new Tuple<int, int>[obj.Length + tools.Length];
            Array.Copy(obj, 0, all, 0, obj.Length);
            Array.Copy(tools, 0, all, obj.Length, tools.Length);

            var Map = new Dictionary<Pair, List<Pair>>();

            for (int i = 0; i < all.Length; ++i)
            {
                Map[all[i]] = new List<Pair>();
                if (dimTagsMap[i].Length > 0)
                {
                    Map[all[i]].AddRange(dimTagsMap[i]);
                }
                else
                {
                    Map[all[i]].Add(new Pair(1, 1));
                    //Map[all[i]].Add(all[i]);
                }
            }

            return Map;
        }


    }

    public static class Helpers
    {
        public static Dictionary<U, List<T>> Invert<T, U>(Dictionary<T, List<U>> dict)
        {
            var inv = new Dictionary<U, List<T>>();
            foreach (var kvp in dict)
            {
                foreach (var i in kvp.Value)
                {
                    if (!inv.ContainsKey(i))
                        inv[i] = new List<T>();
                    inv[i].Add(kvp.Key);
                }
            }

            return inv;
        }

        public static string DictToString<T, U>(Dictionary<T, List<U>> dict)
        {
            var list = new List<string>();
            foreach (var kvp in dict)
            {
                string line = string.Format("{0} --> ", kvp.Key);
                line += string.Join(", ", kvp.Value);

                list.Add(line);
            }

            return string.Join("\n", list);
        }

    }
}
