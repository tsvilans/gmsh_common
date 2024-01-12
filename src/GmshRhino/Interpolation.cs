using Rhino.Geometry;
using Rhino.Render.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmshRhino
{
    public class Interpolation
    {

        public double[] TriBarycentric(Point3d pt, Point3d[] points)
        {
            Vector3d v0 = points[1] - points[0], v1 = points[2] - points[0], v2 = pt - points[0];

            double d00 = v0 * v0;
            double d01 = v0 * v1;
            double d11 = v1 * v1;
            double d20 = v2 * v0;
            double d21 = v2 * v1;

            double denom = d00 * d11 - d01 * d01;

            double v = (d11 * d20 - d01 * d21) / denom;
            double w = (d00 * d21 - d01 * d20) / denom;
            double u = 1.0 - v - w;

            return new double[] { u, v, w };
        }

        public double[] TetraBaryentric(Point3d pt, Point3d[] points)
        {

                Vector3d vap = pt - points[0];
                Vector3d vbp = pt - points[1];

                Vector3d vab = points[1] - points[0];
                Vector3d vac = points[2] - points[0];
                Vector3d vad = points[3] - points[0];

                Vector3d vbc = points[2] - points[1];
                Vector3d vbd = points[3] - points[1];

                double va6 = Vector3d.CrossProduct(vbp, vbd) * vbc;
                double vb6 = Vector3d.CrossProduct(vap, vac) * vad;
                double vc6 = Vector3d.CrossProduct(vap, vad) * vab;
                double vd6 = Vector3d.CrossProduct(vap, vab) * vac;
                double v6 = 1.0 / (Vector3d.CrossProduct(vab, vac) * vad);

                var weights = new double[] { va6 * v6, vb6 * v6, vc6 * v6, vd6 * v6 };

            return weights;
        }

        public double[] Interpolate1D(Point3d pt, Point3d[] points)
        {
            Vector3d v0 = points[1] - points[0], v1 = points[0] - pt;

            return new double[] { v0 * v1 };
        }

        public double[] BoxTrilinear(Point3d pt, Point3d[] points)
        {

            throw new NotImplementedException();
        }
    }
}
