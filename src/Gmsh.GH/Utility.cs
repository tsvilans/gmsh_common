﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using Grasshopper.Kernel.Types;

using Pair = System.Tuple<int, int>;

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
            Gmsh.Mesh.GetNodes(out nodeTags, out coords, dim, tag, true, false);

            if (nodeTags == null || nodeTags.Length < 1) throw new Exception("Bad nodeTags in GetMesh()");

            var nodes = new Point3d[nodeTags.Length + 1];

            for (int i = 0; i < nodeTags.Length; ++i)
            {
                var pt = new Point3d(coords[i * 3], coords[i * 3 + 1], coords[i * 3 + 2]);
                nodes[(int)nodeTags[i]] = pt;
            }

            int[] elementTypes;
            IntPtr[][] elementTags, enodeTags;

            
            Gmsh.Mesh.GetElements(out elementTypes, out elementTags, out enodeTags, 2, tag);

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
            return GetMesh(new Pair ( dim, tag ));
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
                    Gmsh.GetBoundary(new Tuple<int, int>[] { dimTag }, out boundaries, true, false, false);
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
                Gmsh.Mesh.GetNodes(out nodeTags, out coords, 2, dimTag.Item2, true, false);

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

                Gmsh.Mesh.GetElements(out elementTypes, out elementTags, out enodeTags, 2, dimTag.Item2);

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


        public static void BrepToGmsh(Brep brep)
        {

            var verts = new List<int>();
            var edges = new List<int>();
            var curves3d = new List<int>();
            var loops = new List<int>();
            var surfaces = new List<int>();
            var faces = new List<int>();

            foreach (BrepVertex vertex in brep.Vertices)
            {
                Gmsh.OCC.AddPoint(vertex.Location.X, vertex.Location.Y, vertex.Location.Z, -1);
            }

            foreach (var edge in brep.Edges)
            {
                var crv = edge.EdgeCurve;
                var bspline = crv.ToNurbsCurve();
                var pointTags = new List<int>();
                var weights = new List<double>();

                foreach (ControlPoint cpt in bspline.Points)
                {

                    pointTags.Add(Gmsh.OCC.AddPoint(cpt.X, cpt.Y, cpt.Z));
                    weights.Add(cpt.Weight);
                }
                curves3d.Add(Gmsh.OCC.AddBSpline(pointTags.ToArray(), -1, crv.Degree, weights.ToArray()));
            }

            foreach (Surface srf in brep.Surfaces)
            {
                var bsrf = srf.ToNurbsSurface();
                var pointTags = new List<int>();
                var weights = new List<double>();

                foreach (ControlPoint cpt in bsrf.Points)
                {
                    pointTags.Add(Gmsh.OCC.AddPoint(cpt.X, cpt.Y, cpt.Z));
                    weights.Add(cpt.Weight);
                }

                surfaces.Add(Gmsh.OCC.AddBSplineSurface(pointTags.ToArray(), bsrf.Points.CountV, -1, bsrf.Degree(1), bsrf.Degree(0),
                  weights.ToArray()));
            }


            foreach (BrepFace face in brep.Faces)
            {
                var wires = new List<int>();

                foreach (BrepLoop loop in face.Loops)
                {
                    var wire = new List<int>();

                    foreach (BrepTrim trim in loop.Trims)
                    {
                        wire.Add(curves3d[trim.Edge.EdgeCurveIndex]);
                    }

                    wires.Add(Gmsh.OCC.AddWire(wire.ToArray(), -1, true));
                }

                faces.Add(Gmsh.OCC.AddTrimmedSurface(surfaces[face.SurfaceIndex], wires.ToArray()));

                Gmsh.OCC.Synchronize();

            }

            Gmsh.OCC.Remove(surfaces.Select(x => new Pair(2, x)).ToArray(), true);
            Gmsh.OCC.Remove(curves3d.Select(x => new Pair(1, x)).ToArray(), true);
            Gmsh.OCC.Remove(verts.Select(x => new Pair(0, x)).ToArray(), true);

            var faceLoop = Gmsh.OCC.AddSurfaceLoop(faces.ToArray(), -1);

            if (brep.IsSolid)
            {
                var volume = Gmsh.OCC.AddVolume(new int[] { faceLoop });

            }

            Gmsh.OCC.Synchronize();

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
            Gmsh.Mesh.GetNodes(out nodeTags, out coords, 3, -1, true, false);

            var nodes = new Point3d[nodeTags.Length + 1];

            for (int i = 0; i < nodeTags.Length; ++i)
            {
                var pt = new Point3d(coords[i * 3], coords[i * 3 + 1], coords[i * 3 + 2]);
                nodes[(int)nodeTags[i]] = pt;
            }

            int[] elementTypes;
            IntPtr[][] elementTags, enodeTags;

            var entities = Gmsh.GetEntitiesForPhysicalGroup(dim, tag);

            var mesh = new Mesh();
            mesh.Vertices.AddVertices(nodes);

            foreach (int ent in entities)
            {
                Gmsh.Mesh.GetElements(out elementTypes, out elementTags, out enodeTags, dim, ent);

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
            var entity = Gmsh.AddDiscreteEntity(2, -1);

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

            Gmsh.Mesh.AddNodes(2, entity, tags, coords);

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

            Gmsh.Mesh.AddElements(2, entity, elementTypes.ToArray(),
              elementTags.ToArray(),
              nodeTags.ToArray());

            Gmsh.Mesh.CreateTopology(true, true);
            Gmsh.Mesh.ClassifySurfaces(0.1, true, true, 0.1, true);

            if (create_geometry)
                Gmsh.Mesh.CreateGeometry();

            return entity;
        }

        public static List<Point3d> GetCentroids()
        {
            var centroids = new List<Point3d>();
            try
            {
                int[] elementTypes;
                IntPtr[][] elementTags, enodeTags;
                Gmsh.Mesh.GetElements(out elementTypes, out elementTags, out enodeTags, 3, -1);

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

            Gmsh.SetNumber("Geometry.OCCBooleanPreserveNumbering", 1);

            var obj = new Pair[] { dimTags[0] };
            var tools = new Pair[dimTags.Length - 1];
            Array.Copy(dimTags, 1, tools, 0, tools.Length);

            Gmsh.OCC.Fragment(obj, tools, out dimTagsOut, out dimTagsMap, true, true);
            Gmsh.OCC.Synchronize();


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
