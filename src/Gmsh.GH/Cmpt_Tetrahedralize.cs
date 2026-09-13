
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

    public class Cmpt_Tetrahedralize : GH_Component
    {
        // A private, per-instance Gmsh model. Generated once when this component instance is
        // created (including on document reload, so we never rely on a name from a previous
        // Rhino session) and kept for the component's lifetime.
        private readonly string _modelName = "Gmsh.GH_Tetrahedralize_" + Guid.NewGuid().ToString("N");

        public Cmpt_Tetrahedralize()
            : base("Tetrahedralize", "Tetrahedralize",
                "Tetrahedralize points.",
                "Gmsh", "Meshing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Points", "P", "Points to tetrahedralize.", GH_ParamAccess.list);
            pManager.AddNumberParameter("MaxEdge", "ME", "Maximum edge length.", GH_ParamAccess.item, 100);
            pManager.AddNumberParameter("MaxAniso", "MA", "Maximum anisotropy of elements.", GH_ParamAccess.item, 1e5);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Tetrahedralized mesh.", GH_ParamAccess.item);
            pManager.AddTextParameter("Model", "G", "Name of this component's Gmsh model, for use with the Query component.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Always report this component's model name, even if we return early below -
            // it's stable for the component's lifetime regardless of whether it has solved.
            DA.SetData("Model", _modelName);

            var points = new List<Point3d>();
            double maxEdgeLength = 100, maxAnisotropy = 1e5;

            DA.GetDataList("Points", points);
            DA.GetData("MaxEdge", ref maxEdgeLength);
            DA.GetData("MaxAniso", ref maxAnisotropy);

            Mesh mesh;

            // Fresh model every solve: the tetrahedralization is fully rebuilt from the
            // current points each time, so we remove any previous version of this
            // component's model and start clean. This never touches other components'
            // models, and is safe even if another component is using Gmsh concurrently.
            using (GmshSession.UseFreshModel(_modelName))
            {
                mesh = Tetra.GetTetrahedralizedShell(points, maxEdgeLength, 1e-5, maxAnisotropy);
            }

            DA.SetData("Mesh", mesh);

        }

        public override void RemovedFromDocument(GH_Document document)
        {
            GmshSession.RemoveModel(_modelName);
            base.RemovedFromDocument(document);
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
            get { return new Guid("E75A6DE7-E402-4325-94B5-215A71F08054"); }
        }
    }
}
