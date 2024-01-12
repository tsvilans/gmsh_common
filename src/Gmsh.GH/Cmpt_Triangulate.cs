
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

    public class Cmpt_Triangulate : GH_Component
    {
        public Cmpt_Triangulate()
            : base("Triangulate", "Triangulate",
                "Triangulate points.",
                "Gmsh", "Meshing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Points", "P", "Points to triangulate.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Triangulated mesh.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            var points = new List<Point3d>();

            DA.GetDataList("Points", points);

            if (points.Count < 1) return;

            Gmsh.InitializeGmsh();

            var ptsFlat2d = new double[points.Count * 2];

            for (int i = 0; i < points.Count; ++i)
            {
                var pt = points[i];
                ptsFlat2d[i * 2 + 0] = pt.X;
                ptsFlat2d[i * 2 + 1] = pt.Y;
            }

            var tris = Gmsh.Model.Mesh.Triangulate(ptsFlat2d);
            var nTris = tris.Length / 3;

            var mesh2d = new Mesh();
            mesh2d.Vertices.AddVertices(points);


            for (int i = 0; i < nTris; ++i)
            {
                int a = (int)tris[i * 3 + 0] - 1,
                  b = (int)tris[i * 3 + 1] - 1,
                  c = (int)tris[i * 3 + 2] - 1;
                mesh2d.Faces.AddFace(a, b, c);
            }


            DA.SetData("Mesh", mesh2d);

            Gmsh.FinalizeGmsh();
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
            /*
                0fe48fc4-68ff-484a-949c-37bae7f882c1
                45094c4f-ef59-4a0a-b30a-6a3fcc21a024
             */
            get { return new Guid("D18B3F3B-7FEF-4E62-A538-FA66E0C4D6DE"); }
        }
    }
}
