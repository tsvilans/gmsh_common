using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;

namespace GmshCommon.GH
{
    public static class Extensions
    {
        public static int[] GetDuplicateFaces(this Mesh mesh)
        {
            var N = 0;
            var isDup = new bool[mesh.Faces.Count];

            //var counter = 0;
            for (int i = 0; i < mesh.Faces.Count; ++i)
            {
                if (isDup[i]) continue;

                for (int j = i + 1; j < mesh.Faces.Count; ++j)
                {
                    if (isDup[j]) continue;

                    var diff = new int[3]{
                      mesh.Faces[i].A,
                      mesh.Faces[i].B,
                      mesh.Faces[i].C
                      }.Except(new int[3]{
                      mesh.Faces[j].A,
                      mesh.Faces[j].B,
                      mesh.Faces[j].C
                      }).ToArray();

                    //Print("length of diff: {0}", diff.Length);

                    //Print("    face0: {0}", mesh.Faces[i]);
                    //Print("    face1: {0}", mesh.Faces[j]);

                    if (diff.Length == 0)
                    {
                        //Print("DUP  {0} {1}", i, j);
                        isDup[i] = true;
                        isDup[j] = true;
                        //break;
                    }

                    //counter++;
                }
            }

            N = isDup.Where(x => x).ToArray().Length;

            var dups = new int[N];

            for (int i = 0, j = 0; i < mesh.Faces.Count; ++i)
            {
                if (isDup[i])
                {
                    dups[j] = i;
                    j++;
                }
            }

            return dups;
        }

    }
}
