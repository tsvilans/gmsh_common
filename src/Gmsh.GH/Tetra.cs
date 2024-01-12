using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmshCommon.GH
{
    public static class Tetra
    {
        public static Mesh GetTetrahedralizedShell(List<Point3d> points, double maxEdgeLength = 100, double volumeThreshold = 1e-5, double maxAnisotropy=1e5, double angleToleranceFacetOverlap=0.3)
        {
            if (points == null || points.Count < 4) return null;

            // Flatten points list
            var ptsFlat3d = new double[points.Count * 3];

            for (int i = 0; i < points.Count; ++i)
            {
                var pt = points[i];
                ptsFlat3d[i * 3 + 0] = pt.X;
                ptsFlat3d[i * 3 + 1] = pt.Y;
                ptsFlat3d[i * 3 + 2] = pt.Z;
            }

            // Do the gee mesh
            Gmsh.InitializeGmsh();

            Gmsh.Option.SetNumber("Mesh.AngleToleranceFacetOverlap", angleToleranceFacetOverlap);
            Gmsh.Option.SetNumber("Mesh.AnisoMax", maxAnisotropy);

            var tetra = Gmsh.Model.Mesh.Tetrahedralize(ptsFlat3d).Select(x => (int)x - 1).ToArray();


            // Filter tetras for quality, edge length, volume
            var tetraList = new List<int[]>();

            for (int i = 0; i < tetra.Length; i += 4)
            {
                double volume, gamma;

                GetTetraQuality(
                    points[tetra[i + 0]],
                    points[tetra[i + 1]],
                    points[tetra[i + 2]],
                    points[tetra[i + 3]],
                    out volume, out gamma);

                var maxEdge = GetMaxEdgeLength(
                    points[tetra[i + 0]],
                    points[tetra[i + 1]],
                    points[tetra[i + 2]],
                    points[tetra[i + 3]]);


                if (gamma > 40)
                {
                    //TO DO: Figure out appropriate tetra quality measure

                    //continue;
                }
                if (maxEdge > maxEdgeLength)
                {
                    continue;
                }
                if (volume < volumeThreshold)
                {
                    continue;
                }

                tetraList.Add(new int[]{
                    tetra[i + 0],
                    tetra[i + 1],
                    tetra[i + 2],
                    tetra[i + 3]});
            }

            var nTetra = tetraList.Count;

            var comparer = new CompareIntArray();

            var cellSet = new HashBucket<int[]>(comparer);

            foreach (var tet in tetraList)
            {
                var c0 = new int[] { tet[0], tet[1], tet[2] };
                Array.Sort(c0);
                cellSet.Add(c0);

                var c1 = new int[] { tet[1], tet[2], tet[3] };
                Array.Sort(c1);
                cellSet.Add(c1);

                var c2 = new int[] { tet[2], tet[3], tet[0] };
                Array.Sort(c2);
                cellSet.Add(c2);

                var c3 = new int[] { tet[3], tet[0], tet[1] };
                Array.Sort(c3);
                cellSet.Add(c3);
            }

            var faces = cellSet.GetUnique();
            /*
            faces.Sort((f0, f1) =>
            {
                int xdiff = f0[0].CompareTo(f1[0]);
                if (xdiff != 0) return xdiff;
                else
                {
                    int ydiff = f0[1].CompareTo(f1[1]);
                    if (ydiff != 0) return ydiff;
                    else
                        return f0[2].CompareTo(f1[2]);
                }
            });
            */

            var mesh3d = new Mesh();
            mesh3d.Vertices.AddVertices(points);

            foreach (var face in faces)
            {
                mesh3d.Faces.AddFace(face[0], face[1], face[2]);
            }

            mesh3d.Compact();

            mesh3d.UnifyNormals();
            mesh3d.Normals.ComputeNormals();

            mesh3d.RebuildNormals();
            Gmsh.FinalizeGmsh();

            return mesh3d;
        }


        public static double GetTetraVolume(Point3d a, Point3d b, Point3d c, Point3d d)
        {
            return Math.Abs((a - d) * Vector3d.CrossProduct((b - d), (c - d))) / 6;
        }

        public static void GetTetraQuality(Point3d a, Point3d b, Point3d c, Point3d d, out double volume, out double gamma)
        {
            var ab = a.DistanceToSquared(b);
            var ac = a.DistanceToSquared(c);
            var ad = a.DistanceToSquared(d);

            var bc = b.DistanceToSquared(c);
            var bd = b.DistanceToSquared(d);

            var cd = c.DistanceToSquared(d);

            volume = Math.Abs((a - d) * Vector3d.CrossProduct((b - d), (c - d))) / 6;

            var srms = Math.Sqrt((ab + ac + ad + bc + bd + cd) / 6);
            gamma = Math.Pow(srms, 3) / (8.479670 * volume);
        }

        public static double GetTetraGammaAspect(Point3d a, Point3d b, Point3d c, Point3d d)
        {
            var ab = a.DistanceToSquared(b);
            var ac = a.DistanceToSquared(c);
            var ad = a.DistanceToSquared(d);

            var bc = b.DistanceToSquared(c);
            var bd = b.DistanceToSquared(d);

            var cd = c.DistanceToSquared(d);

            var volume = Math.Abs((a - d) * Vector3d.CrossProduct((b - d), (c - d))) / 6;

            var srms = Math.Sqrt((ab + ac + ad + bc + bd + cd) / 6);
            return Math.Pow(srms, 3) / (8.479670 * volume);
        }

        public static double GetMaxEdgeLength(Point3d a, Point3d b, Point3d c, Point3d d)
        {
            double max = 0;

            max = Math.Max(max, a.DistanceToSquared(b));
            max = Math.Max(max, a.DistanceToSquared(c));
            max = Math.Max(max, a.DistanceToSquared(d));

            max = Math.Max(max, b.DistanceToSquared(c));
            max = Math.Max(max, b.DistanceToSquared(d));

            max = Math.Max(max, c.DistanceToSquared(d));

            return Math.Sqrt(max);
        }


    }
}
