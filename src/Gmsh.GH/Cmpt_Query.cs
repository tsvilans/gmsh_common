
/*
 * Gmsh.GH
 * Copyright 2023 Tom Svilans
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Rhino;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper;

using GmshCommon;

namespace GmshCommon.GH
{
    public class Cmpt_Query : GH_Component
    {
        public Cmpt_Query()
            : base("Query", "Query",
                "Query a persisted Gmsh model for the element containing (or closest to) a point.",
                "Gmsh", "Meshing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Model", "G", "Name of the Gmsh model to query (from a meshing component's Model output).", GH_ParamAccess.item);
            pManager.AddPointParameter("Point", "P", "Point to sample.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Element", "CE", "Tag of the containing (or closest) element.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Vertices", "V", "Tags of the element's vertices.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Weights", "W", "Relative weight of influence of each vertex, in the same order as Vertices.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string modelName = null;
            Point3d pt = Point3d.Unset;

            if (!DA.GetData("Model", ref modelName)) return;
            if (!DA.GetData("Point", ref pt)) return;

            if (string.IsNullOrEmpty(modelName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Gmsh model name provided.");
                return;
            }

            // Query only - this model should already have been built by an upstream
            // meshing component. We never rebuild or remove it from here.
            if (!GmshSession.ModelExists(modelName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"No Gmsh model named '{modelName}' exists. Has the upstream component solved yet?");
                return;
            }

            using (GmshSession.UseModel(modelName))
            {
                IntPtr elementTag;
                int elementType;
                IntPtr[] nodeTags;
                double u, v, w;
                bool contained;

                // strict = false already applies Gmsh's own (small) search tolerance, which
                // only ever catches points essentially on/inside an element.
                //
                // Search dim=3 and dim=2 separately rather than dim=-1 (every dimension at
                // once). Tetrahedralize's model now holds both its tets AND a coincident,
                // zero-thickness 2D boundary shell covering the same surface (see Tetra.cs) -
                // with dim=-1, Gmsh's octree would sometimes match a point squarely inside a
                // boundary tet to the flat triangle sitting right on that tet's own face
                // instead of the tet itself, since the two element populations occupy the same
                // space. Trying solids first keeps a genuinely-contained point resolving to its
                // tet; only a point dim=3 can't place (nothing there, or a pure surface model
                // with no volume elements at all) falls through to dim=2.
                try
                {
                    Gmsh.Model.Mesh.GetElementByCoordinates(pt.X, pt.Y, pt.Z,
                        out elementTag, out elementType, out nodeTags, out u, out v, out w,
                        3, false);
                    contained = true;
                }
                catch (Exception)
                {
                    try
                    {
                        Gmsh.Model.Mesh.GetElementByCoordinates(pt.X, pt.Y, pt.Z,
                            out elementTag, out elementType, out nodeTags, out u, out v, out w,
                            2, false);
                        contained = true;
                    }
                    catch (Exception)
                    {
                        contained = false;
                        elementTag = IntPtr.Zero;
                        elementType = -1;
                        nodeTags = null;
                        u = v = w = 0;
                    }
                }

                if (!contained)
                {
                    // Genuinely off the mesh (e.g. a point out of plane from a 2D surface, or
                    // outside a volume mesh's bounds): fall back to a real nearest-element
                    // search over every element in the model, rather than giving up.
                    if (!TryFindNearestElement(pt.X, pt.Y, pt.Z, out elementTag, out elementType, out nodeTags))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Model '{modelName}' has no elements to query.");
                        return;
                    }

                    // Local coordinates of the point within the chosen element, extrapolated
                    // beyond its bounds since the point isn't actually inside it. This is an
                    // exact affine inverse for the linear triangles/tets these components
                    // produce, so it's well defined even outside the element.
                    Gmsh.Model.Mesh.GetLocalCoordinatesInElement((int)elementTag, pt.X, pt.Y, pt.Z, out u, out v, out w);
                }

                int numComponents, numOrientations;
                double[] basisFunctions;

                // Order-1 (linear) Lagrange shape functions evaluated at the point's local
                // coordinates within the element are exactly its vertices' barycentric-style
                // interpolation weights (they can fall outside [0, 1] and stop summing
                // cleanly to 1 once the point is extrapolated past the element itself).
                Gmsh.Model.Mesh.GetBasisFunctions(elementType, new double[] { u, v, w }, "Lagrange",
                    out numComponents, out basisFunctions, out numOrientations);

                var vertices = nodeTags.Select(tag => (int)tag).ToList();
                var weights = basisFunctions.ToList();

                DA.SetData("Element", (int)elementTag);
                DA.SetDataList("Vertices", vertices);
                DA.SetDataList("Weights", weights);
            }
        }

        /// <summary>
        /// Brute-force search over every triangle currently in the model for the one
        /// geometrically closest to (x, y, z) - i.e. the minimum distance from the point to the
        /// triangle itself, not to its centroid. Used when Gmsh's own tolerance-based octree
        /// search in GetElementByCoordinates comes up empty, meaning the point is genuinely
        /// outside the whole mesh. Fine for the model sizes these components produce; if that
        /// ever becomes a bottleneck, this is the place to swap in a proper spatial index.
        ///
        /// Ranking by centroid distance (the previous approach) works for surface triangles,
        /// since a triangle's centroid lies right on the surface a query point is near, but real
        /// point-to-triangle distance is exact rather than an approximation that happens to work.
        /// Solid (tet) elements never reach this search at all: a point outside the whole mesh
        /// can only ever be closest to an exposed boundary face, and volume-producing components
        /// (see Tetra.GetTetrahedralizedShell) already register their boundary shell as its own
        /// 2D entity for exactly this reason, so ordinary triangles are all this needs to test.
        /// </summary>
        private static bool TryFindNearestElement(double x, double y, double z,
            out IntPtr elementTag, out int elementType, out IntPtr[] nodeTags)
        {
            elementTag = IntPtr.Zero;
            elementType = -1;
            nodeTags = null;

            var p = new Point3d(x, y, z);

            // Node-tag -> position lookup, built once, so each candidate element's actual
            // shape (not just its centroid) can be tested against the query point.
            IntPtr[] allNodeTags;
            double[] allCoords;
            Gmsh.Model.Mesh.GetNodes(out allNodeTags, out allCoords, -1, -1, true, false);

            var nodePositions = new Dictionary<long, Point3d>(allNodeTags.Length);
            for (int i = 0; i < allNodeTags.Length; ++i)
            {
                nodePositions[(long)allNodeTags[i]] = new Point3d(
                    allCoords[i * 3 + 0], allCoords[i * 3 + 1], allCoords[i * 3 + 2]);
            }

            int[] elementTypes;
            IntPtr[][] elementTagsByType;
            IntPtr[][] nodeTagsByType;

            // dim = -1, tag = -1: every element in the model, regardless of dimension.
            Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTagsByType, out nodeTagsByType, -1, -1);

            double bestDistSq = double.MaxValue;
            IntPtr bestTag = IntPtr.Zero;
            int bestType = -1;

            for (int t = 0; t < elementTypes.Length; ++t)
            {
                var tags = elementTagsByType[t];
                if (tags.Length == 0) continue;

                // Gmsh returns element connectivity flattened per element type: one fixed
                // node count per element of this type, tags.Length elements in a row.
                var flatNodeTags = nodeTagsByType[t];
                int nodesPerElement = flatNodeTags.Length / tags.Length;

                // This fallback only ever runs once GetElementByCoordinates has already failed
                // to find a containing element, i.e. the point is genuinely outside the whole
                // mesh - so the nearest thing can only be a boundary face, never a solid tet.
                // Meshing components that produce volumes (Tetra.GetTetrahedralizedShell)
                // already register their boundary shell as its own 2D discrete entity
                // alongside the tets (computed once, at build time, from the same face list
                // used for the Rhino output mesh), so plain triangles are all we need to test
                // here - the same search that already works for purely-2D models.
                if (nodesPerElement != 3) continue;

                var verts = new Point3d[nodesPerElement];

                for (int i = 0; i < tags.Length; ++i)
                {
                    bool complete = true;
                    for (int n = 0; n < nodesPerElement; ++n)
                    {
                        if (!nodePositions.TryGetValue((long)flatNodeTags[i * nodesPerElement + n], out verts[n]))
                        {
                            complete = false;
                            break;
                        }
                    }
                    if (!complete) continue; // unresolved connectivity - skip rather than fake a (0,0,0) vertex

                    double distSq = ElementDistanceSquared(p, verts);

                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestTag = tags[i];
                        bestType = elementTypes[t];
                    }
                }
            }

            if (bestType < 0) return false;

            Gmsh.Model.Mesh.GetElement(bestTag, bestType, out nodeTags, out int dim, out int entityTag);
            elementTag = bestTag;
            elementType = bestType;
            return true;
        }

        /// <summary>
        /// Squared distance from a point to a (triangular) element's boundary, given its vertex
        /// positions. Only ever called with 3 vertices in practice - the fallback above skips
        /// any element type it isn't - but falls back to distance-to-centroid for anything else
        /// rather than throwing.
        /// </summary>
        private static double ElementDistanceSquared(Point3d p, Point3d[] verts)
        {
            if (verts.Length == 3)
            {
                return DistanceSqPointTriangle(p, verts[0], verts[1], verts[2]);
            }

            double cx = 0, cy = 0, cz = 0;
            foreach (var v in verts) { cx += v.X; cy += v.Y; cz += v.Z; }
            var centroid = new Point3d(cx / verts.Length, cy / verts.Length, cz / verts.Length);
            return p.DistanceToSquared(centroid);
        }

        /// <summary>
        /// Exact squared distance from a point to a triangle in 3D (Ericson, "Real-Time
        /// Collision Detection", ch. 5): finds which Voronoi region of the triangle (a vertex,
        /// an edge, or the face itself) the point projects into and returns the distance to the
        /// corresponding closest point.
        /// </summary>
        private static double DistanceSqPointTriangle(Point3d p, Point3d a, Point3d b, Point3d c)
        {
            Vector3d ab = b - a;
            Vector3d ac = c - a;
            Vector3d ap = p - a;

            double d1 = ab * ap;
            double d2 = ac * ap;
            if (d1 <= 0 && d2 <= 0) return p.DistanceToSquared(a);

            Vector3d bp = p - b;
            double d3 = ab * bp;
            double d4 = ac * bp;
            if (d3 >= 0 && d4 <= d3) return p.DistanceToSquared(b);

            double vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                double v = d1 / (d1 - d3);
                return p.DistanceToSquared(a + v * ab);
            }

            Vector3d cp = p - c;
            double d5 = ab * cp;
            double d6 = ac * cp;
            if (d6 >= 0 && d5 <= d6) return p.DistanceToSquared(c);

            double vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                double w = d2 / (d2 - d6);
                return p.DistanceToSquared(a + w * ac);
            }

            double va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
            {
                double w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return p.DistanceToSquared(b + w * (c - b));
            }

            double denom = 1.0 / (va + vb + vc);
            double vv = vb * denom;
            double ww = vc * denom;
            return p.DistanceToSquared(a + ab * vv + ac * ww);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
                //  return Properties.Resources.GridSample_01;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("54178F33-90A6-474D-92A5-55330E23077E"); }
        }
    }
}
