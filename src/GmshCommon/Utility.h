#pragma once

#include "gmsh.h"
#include <msclr\marshal_cppstd.h>

using System::IntPtr;
using System::Runtime::InteropServices::Marshal;

namespace GmshCommon {
	public ref class Utility
	{
	public:
		static array<double>^ GetCentroid(IntPtr elementTag, int elementType)
		{
			size_t eTag(elementTag.ToInt64());
			std::vector<size_t> nodeTags;
			int dim, tag;

			gmsh::model::mesh::getElement(eTag, elementType, nodeTags, dim, tag);

			std::vector<double> coord, parametricCoord;
			//gmsh::model::mesh::getNodes(nodeTags, coord, parametricCoord, dim, tag, false, true);

			double x = 0, y = 0, z = 0;

			for (int i = 0; i < nodeTags.size(); ++i)
			{
				gmsh::model::mesh::getNode(nodeTags[i], coord, parametricCoord, dim, tag);

				x += coord[0];
				y += coord[1];
				z += coord[2];
			}

			x /= nodeTags.size();
			y /= nodeTags.size();
			z /= nodeTags.size();

			return gcnew array<double> { x, y, z };
		}
	};
}

