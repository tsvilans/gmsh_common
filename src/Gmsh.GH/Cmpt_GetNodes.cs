
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
    public class Cmpt_GetNodes : GH_Component
    {
        public Cmpt_GetNodes()
            : base("Get Nodes", "GetNodes",
                "List every node (tag and position) currently in a Gmsh model.",
                "Gmsh", "Meshing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Model", "G", "Name of the Gmsh model to inspect.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Note: Gmsh node tags are 1-based. Node IDs[i] is the tag of the node at
            // Node Positions[i] - it is NOT a 0-based index into whatever point list you
            // originally fed a meshing component. To match a tag back to your own input
            // points list, use (tag - 1) as the index.
            pManager.AddIntegerParameter("Node IDs", "NI", "Tags of every node in the model (1-based).", GH_ParamAccess.list);
            pManager.AddPointParameter("Node Positions", "NP", "Positions of every node, in the same order as Node IDs.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string modelName = null;

            if (!DA.GetData("Model", ref modelName)) return;

            if (string.IsNullOrEmpty(modelName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Gmsh model name provided.");
                return;
            }

            if (!GmshSession.ModelExists(modelName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"No Gmsh model named '{modelName}' exists. Has the upstream component solved yet?");
                return;
            }

            using (GmshSession.UseModel(modelName))
            {
                IntPtr[] nodeTags;
                double[] coord;

                // dim = -1, tag = -1: every node in the model, regardless of which entity
                // it's classified on.
                Gmsh.Model.Mesh.GetNodes(out nodeTags, out coord, -1, -1, true, false);

                var ids = nodeTags.Select(t => (int)t).ToList();
                var positions = new List<Point3d>(nodeTags.Length);
                for (int i = 0; i < nodeTags.Length; ++i)
                {
                    positions.Add(new Point3d(coord[i * 3 + 0], coord[i * 3 + 1], coord[i * 3 + 2]));
                }

                DA.SetDataList("Node IDs", ids);
                DA.SetDataList("Node Positions", positions);
            }
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
            get { return new Guid("835592A7-5F5C-40A2-AE66-4658A28C4BA6"); }
        }
    }
}
