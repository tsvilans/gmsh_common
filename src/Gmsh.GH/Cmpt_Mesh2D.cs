
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

    public class Cmpt_Mesh2D : GH_Component
    {
        public Cmpt_Mesh2D()
            : base("Gmsh2D", "Gmsh2D",
                "Mesh 2D entities.",
                "Gmsh", "Meshing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Mesh", "M", "Mesh to remesh.", GH_ParamAccess.item);
            pManager.AddNumberParameter("SizeMin", "Min", "Minimum mesh size.", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("SizeMax", "Max", "Maximum mesh size.", GH_ParamAccess.item, 100.0);
            pManager.AddBooleanParameter("Remesh", "R", "Remesh mesh geometry.", GH_ParamAccess.item, false);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Meshed object.", GH_ParamAccess.list);

            pManager.AddIntegerParameter("Node IDs", "NI", "Node IDs.", GH_ParamAccess.list);
            pManager.AddPointParameter("Node Positions", "NP", "Node positions.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Element IDs", "EI", "Element IDs.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Element Nodes", "EN", "Element node IDs.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double size_min = 10.0, size_max = 100.0;
            bool create_geometry = false;

            DA.GetData("SizeMin", ref size_min);
            DA.GetData("SizeMax", ref size_max);
            DA.GetData("Remesh", ref create_geometry);

            Mesh mesh = null;

            DA.GetData("Mesh", ref mesh);

            if (mesh == null) return;

            Gmsh.InitializeGmsh();
            Gmsh.Clear();
            Gmsh.Logger.Start();

            var mesh_id = -1;

            // Add mesh data
            try
            {
                mesh_id = GmshCommon.GeometryExtensions.TransferMesh(mesh, create_geometry);
            }
            catch (Exception e)
            {
                string msg = Gmsh.Logger.GetLastError();

                var log = Gmsh.Logger.Get();
                foreach (string l in log)
                    msg += String.Format("\n    {0}", log);

                throw new Exception(msg);
            }

            if (mesh_id < 0) return;


            // Get 2D entities (the mesh we just transferred)
             Tuple<int, int>[] surfaceTags;
             Gmsh.GetEntities(out surfaceTags, 2);
            //Message = String.Format("{0} : {1}", mesh_id, surfaceTags[0].Item2);

            var loop = Gmsh.Geo.AddSurfaceLoop(surfaceTags.Select(x => x.Item2).ToArray());
            
            //var loop = Gmsh.Geo.AddSurfaceLoop(new int[] { mesh_id });
            var vol = Gmsh.Geo.AddVolume(new int[] { loop });

            Gmsh.Geo.Synchronize();

            // Set mesh sizes
            int element_order = 1;
            Gmsh.SetNumber("Mesh.MeshSizeMin", size_min);
            Gmsh.SetNumber("Mesh.MeshSizeMax", size_max);

            Gmsh.SetNumber("Mesh.SaveAll", 0);
            Gmsh.SetNumber("Mesh.SaveGroupsOfElements", -1001);
            Gmsh.SetNumber("Mesh.SaveGroupsOfNodes", 2);
            Gmsh.SetNumber("Mesh.ElementOrder", element_order);

            // Generate mesh
            Gmsh.Generate(3);

            mesh = GmshCommon.GeometryExtensions.GetMesh();

            mesh.Compact();
            List<Mesh> meshes = new List<Mesh>();

            meshes.Add(mesh);

            DA.SetDataList(0, meshes);

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
            get { return new Guid("242113a3-bccd-476b-89b1-306c18d4e930"); }
        }
    }
}
