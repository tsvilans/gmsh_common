#pragma once

#include "gmsh.h"
#include <iostream>
#include <msclr\marshal_cppstd.h>

using System::IntPtr; 
using System::Runtime::InteropServices::Marshal;

namespace GmshCommon {

	public 	delegate double MeshSizeCallback(int, int, double, double, double, double);

	public ref class Gmsh
	{
	public:

		static void InitializeGmsh()
		{
			gmsh::initialize();
		}

		static void FinalizeGmsh()
		{
			gmsh::finalize();
		}

		static void Clear()
		{
			gmsh::clear();
		}

		static void Write(System::String^ filepath)
		{
			gmsh::write(msclr::interop::marshal_as<std::string>(filepath));
		}

		static void Open(System::String^ filepath)
		{
			gmsh::open(msclr::interop::marshal_as<std::string>(filepath));
		}

		static void Merge(System::String^ filepath)
		{
			gmsh::merge(msclr::interop::marshal_as<std::string>(filepath));
		}

		ref class Logger
		{
		public:
			static System::String^ GetLastError()
			{
				std::string error;
				gmsh::logger::getLastError(error);

				return gcnew System::String(error.c_str());
			}

			static void Start()
			{
				gmsh::logger::start();
			}

			static void Stop()
			{
				gmsh::logger::stop();
			}

			static void Write(System::String^ message, System::String^ level)
			{
				gmsh::logger::write(msclr::interop::marshal_as<std::string>(message), msclr::interop::marshal_as<std::string>(level));
			}

			static array<System::String^>^ Get()
			{
				std::vector<std::string> messages;
				gmsh::logger::get(messages);
				array<System::String^>^ log = gcnew array<System::String^>(messages.size());
				for (int i = 0; i < log->Length; ++i)
				{
					log[i] = gcnew System::String(messages[i].c_str());
				}

				return log;
			}
		};

		ref class Model
		{
		public:
			static void Add(System::String^ name)
			{
				gmsh::model::add(msclr::interop::marshal_as<std::string>(name));
			}

			static void SetCurrent(System::String^ name)
			{
				gmsh::model::setCurrent(msclr::interop::marshal_as<std::string>(name));
			}

			static System::String^ GetCurrent()
			{
				std::string name;

				gmsh::model::getCurrent(name);

				return gcnew System::String(name.c_str());
			}

			static void Generate(int dim)
			{
				gmsh::model::mesh::generate(dim);
			}

			static int AddDiscreteEntity(int dim, int tag)
			{
				return gmsh::model::addDiscreteEntity(dim, tag);
			}

			static void GetEntities([System::Runtime::InteropServices::Out] array<System::Tuple<int, int>^>^% dimTags)
			{
				return GetEntities(dimTags, -1);
			}

			static void GetEntities([System::Runtime::InteropServices::Out] array<System::Tuple<int, int>^>^% dimTags, int dim)
			{
				gmsh::vectorpair nDimTags;
				//for (int i = 0; i < dimTags->Length; ++i)
				//	nDimTags[i] = std::pair<int, int>(dimTags[i]->Item1, dimTags[i]->Item2);

				gmsh::model::getEntities(nDimTags, dim);

				dimTags = gcnew array<System::Tuple<int, int>^>(nDimTags.size());
				for (int i = 0; i < dimTags->Length; ++i)
					dimTags[i] = gcnew System::Tuple<int, int>(nDimTags[i].first, nDimTags[i].second);
			}

			static void GetBoundary(array<System::Tuple<int, int>^>^ tags, [System::Runtime::InteropServices::Out] array<System::Tuple<int, int>^>^% outDimTags, System::Boolean combined, System::Boolean oriented, System::Boolean recursive)
			{
				gmsh::vectorpair dimTags, nOutDimTags;
				for (int i = 0; i < tags->Length; ++i)
				{
					dimTags.push_back(std::pair<int, int>(tags[i]->Item1, tags[i]->Item2));
				}
				gmsh::model::getBoundary(dimTags, nOutDimTags, combined, oriented, recursive);

				outDimTags = gcnew array<System::Tuple<int, int>^>(nOutDimTags.size());
				for (int i = 0; i < nOutDimTags.size(); ++i)
					outDimTags[i] = gcnew System::Tuple<int, int>(nOutDimTags[i].first, nOutDimTags[i].second);
			}

			static System::String^ GetType(int dim, int tag)
			{
				std::string value;
				gmsh::model::getType(dim, tag, value);
				return gcnew System::String(value.c_str());
			}

			static void DeleteEntities(array<System::Tuple<int, int>^>^ tags)
			{
				DeleteEntities(tags, false);
			}

			static void DeleteEntities(array<System::Tuple<int, int>^>^ tags, System::Boolean recursive)
			{
				gmsh::vectorpair dimTags;
				for (int i = 0; i < tags->Length; ++i)
				{
					dimTags.push_back(std::pair<int, int>(tags[i]->Item1, tags[i]->Item2));
				}

				gmsh::model::removeEntities(dimTags, recursive);
			}

			static System::String^ GetEntityName(int dim, int tag)
			{
				std::string name;
				gmsh::model::getEntityName(dim, tag, name);

				return gcnew System::String(name.c_str());
			}

			static void SetEntityName(int dim, int tag, System::String^ name)
			{
				gmsh::model::setEntityName(dim, tag, msclr::interop::marshal_as<std::string>(name));
			}

			static int AddPhysicalGroup(int dim, array<int>^ tags, System::String^ name)
			{
				return AddPhysicalGroup(dim, tags, -1, name);
			}

			static int AddPhysicalGroup(int dim, array<int>^ tags, int tag, System::String^ name)
			{
				std::vector<int> ntags(tags->Length);
				Marshal::Copy(tags, 0, IntPtr(ntags.data()), tags->Length);

				return gmsh::model::addPhysicalGroup(dim, ntags, tag, msclr::interop::marshal_as<std::string>(name));
			}

			static System::String^ GetPhysicalName(int dim, int tag)
			{
				std::string name;
				gmsh::model::getPhysicalName(dim, tag, name);
				return gcnew System::String(name.c_str());
			}

			static void SetPhysicalName(int dim, int tag, System::String^ name)
			{
				gmsh::model::setPhysicalName(dim, tag, msclr::interop::marshal_as<std::string>(name));
			}

			static array<System::Tuple<int, int>^>^ GetPhysicalGroups(int dim)
			{
				gmsh::vectorpair nDimTags;
				gmsh::model::getPhysicalGroups(nDimTags, dim);

				array<System::Tuple<int, int>^>^ dimTags = gcnew array<System::Tuple<int, int>^>(nDimTags.size());
				for (int i = 0; i < nDimTags.size(); ++i)
					dimTags[i] = gcnew System::Tuple<int, int>(nDimTags[i].first, nDimTags[i].second);

				return dimTags;
			}

			static array<int>^ GetEntitiesForPhysicalGroup(int dim, int tag)
			{
				std::vector<int> tags;
				gmsh::model::getEntitiesForPhysicalGroup(dim, tag, tags);

				array<int>^ entities = gcnew array<int>(tags.size());

				Marshal::Copy(IntPtr(tags.data()), entities, 0, entities->Length);

				return entities;
			}

			static void RemovePhysicalGroups(array<System::Tuple<int, int>^>^ dimTags)
			{
				gmsh::vectorpair nDimTags;

				for (int i = 0; i < dimTags->Length; ++i)
				{
					nDimTags.push_back(std::pair<int, int>(dimTags[i]->Item1, dimTags[i]->Item2));
				}

				gmsh::model::removePhysicalGroups(nDimTags);
			}

			static array<double>^ GetValue(int dim, int tag, array<double>^ parametricCoord)
			{
				std::vector<double> nParametricCoord(parametricCoord->Length);
				Marshal::Copy(parametricCoord, 0, IntPtr(nParametricCoord.data()), parametricCoord->Length);

				std::vector<double> nCoord;
				gmsh::model::getValue(dim, tag, nParametricCoord, nCoord);

				array<double>^ coord = gcnew array<double>(nCoord.size());
				Marshal::Copy(IntPtr(nCoord.data()), coord, 0, nCoord.size());


				return coord;
			}

			ref class Mesh
			{
			public:
				static void AffineTransform(array<double>^ affineTransform, array<array<int>^>^ dimTags)
				{
					std::vector<double> af(affineTransform->Length);
					Marshal::Copy(affineTransform, 0, IntPtr(&af[0]), affineTransform->Length);

					//std::vector<int> dt(dimTags->Length);
					//Marshal::Copy(dimTags, 0, IntPtr(&dt[0]), dimTags->Length);

					gmsh::model::mesh::affineTransform(af);
				}

				static void AddNodes(int dim, int tag, array<IntPtr>^ nodeTags, array<double>^ coordinates)
				{
					std::vector<size_t> nnodeTags(nodeTags->Length);
					Marshal::Copy(nodeTags, 0, IntPtr(nnodeTags.data()), nodeTags->Length);

					std::vector<double> coord(coordinates->Length);
					Marshal::Copy(coordinates, 0, IntPtr(coord.data()), coordinates->Length);

					gmsh::model::mesh::addNodes(dim, tag, nnodeTags, coord);
				}

				static void AddFaces(int faceType, array<IntPtr>^ faceTags, array<IntPtr>^ faceNodes)
				{
					std::vector<size_t> nFaceTags(faceTags->Length), nFaceNodes(faceNodes->Length);

					Marshal::Copy(faceTags, 0, IntPtr(nFaceTags.data()), faceTags->Length);
					Marshal::Copy(faceNodes, 0, IntPtr(nFaceNodes.data()), faceNodes->Length);

					gmsh::model::mesh::addFaces(faceType, nFaceTags, nFaceNodes);
				}

				static void AddElements(int dim, int tag, array<int>^ elementTypes, array < array<IntPtr>^>^ elementTags, array < array<IntPtr>^>^ nodeTags)
				{
					std::vector<int> nElementTypes(elementTypes->Length);
					Marshal::Copy(elementTypes, 0, IntPtr(nElementTypes.data()), elementTypes->Length);

					std::vector < std::vector<size_t>> nElementTags(elementTags->Length);
					for (int i = 0; i < elementTags->Length; ++i)
					{
						nElementTags[i] = std::vector<size_t>(elementTags[i]->Length);
						Marshal::Copy(elementTags[i], 0, IntPtr(nElementTags[i].data()), elementTags[i]->Length);
					}

					std::vector < std::vector<size_t>> nNodeTags(nodeTags->Length);
					for (int i = 0; i < nodeTags->Length; ++i)
					{
						nNodeTags[i] = std::vector<size_t>(nodeTags[i]->Length);
						Marshal::Copy(nodeTags[i], 0, IntPtr(nNodeTags[i].data()), nodeTags[i]->Length);
					}

					gmsh::model::mesh::addElements(dim, tag, nElementTypes, nElementTags, nNodeTags);
				}

				static void ClassifySurfaces(double angle, System::Boolean boundary, System::Boolean forReparametrization, double curveAngle, System::Boolean exportDiscrete)
				{
					gmsh::model::mesh::classifySurfaces(angle, boundary, forReparametrization, curveAngle, exportDiscrete);
				}

				static void CreateGeometry()
				{
					gmsh::model::mesh::createGeometry();
				}

				static void CreateTopology()
				{
					CreateTopology(true, true);
				}

				static void CreateTopology(System::Boolean makeSimplyConnected, System::Boolean exportDiscrete)
				{
					gmsh::model::mesh::createTopology(makeSimplyConnected, exportDiscrete);

				}

				static void GetIntegrationPoints(int elementType, System::String^ integrationType,
					[System::Runtime::InteropServices::Out] array<double>^% localCoord,
					[System::Runtime::InteropServices::Out] array<double>^% weights)
				{
					std::vector<double> nLocalCoord, nWeights;
					gmsh::model::mesh::getIntegrationPoints(elementType, msclr::interop::marshal_as<std::string>(integrationType), nLocalCoord, nWeights);


					localCoord = gcnew array<double>(nLocalCoord.size());
					Marshal::Copy(IntPtr(nLocalCoord.data()), localCoord, 0, nLocalCoord.size());

					weights = gcnew array<double>(nWeights.size());
					Marshal::Copy(IntPtr(nWeights.data()), weights, 0, nWeights.size());
				}

				static void GetElementTypes([System::Runtime::InteropServices::Out] array<int>^% types, int dim)
				{
					GetElementTypes(types, dim, -1);
				}

				static void GetElementTypes([System::Runtime::InteropServices::Out] array<int>^% types)
				{
					GetElementTypes(types, -1, -1);
				}

				static void GetElementTypes([System::Runtime::InteropServices::Out] array<int>^% types, int dim, int tag)
				{
					std::vector<int> nTypes;
					gmsh::model::mesh::getElementTypes(nTypes, dim, tag);

					types = gcnew array<int>(nTypes.size());
					Marshal::Copy(IntPtr(nTypes.data()), types, 0, nTypes.size());
				}

				static void GetNodes([System::Runtime::InteropServices::Out] array<IntPtr>^% nodeTags, [System::Runtime::InteropServices::Out] array<double>^% coord, int dim, int tag, System::Boolean includeBoundary, System::Boolean returnParametricCoord)
				{
					std::vector<size_t> nodeTags_native;
					std::vector<double> coord_native, parametricCoord_native;
					gmsh::model::mesh::getNodes(nodeTags_native, coord_native, parametricCoord_native, dim, tag, includeBoundary, returnParametricCoord);

					coord = gcnew array<double>(coord_native.size());
					if (coord_native.size() > 0)
						Marshal::Copy(IntPtr(coord_native.data()), coord, 0, coord_native.size());

					//parametricCoord = gcnew array<double>(parametricCoord_native.size());
					//if (parametricCoord_native.size() > 0)
					//	Marshal::Copy(IntPtr(parametricCoord_native.data()), parametricCoord, 0, parametricCoord_native.size());

					nodeTags = gcnew array<IntPtr>(nodeTags_native.size());
					if (nodeTags_native.size() > 0)
						Marshal::Copy(IntPtr(nodeTags_native.data()), nodeTags, 0, nodeTags_native.size());
				}

				static void GetElement(IntPtr elementTag, int elementType,
					[System::Runtime::InteropServices::Out] array<IntPtr>^% nodeTags,
					[System::Runtime::InteropServices::Out] int% dim,
					[System::Runtime::InteropServices::Out] int% tag)
				{
					size_t eTag(elementTag.ToInt64());
					std::vector<size_t> nodeTags_native;
					int ndim, ntag;

					gmsh::model::mesh::getElement(eTag, elementType, nodeTags_native, ndim, ntag);
					dim = ndim;
					tag = ntag;

					nodeTags = gcnew array<IntPtr>(nodeTags_native.size());
					if (nodeTags_native.size() > 0)
						Marshal::Copy(IntPtr(nodeTags_native.data()), nodeTags, 0, nodeTags_native.size());
				}

				static System::String^ GetElementProperties(
					int elementType,
					[System::Runtime::InteropServices::Out] int% dim, [System::Runtime::InteropServices::Out] int% order,
					[System::Runtime::InteropServices::Out] int% numNodes, [System::Runtime::InteropServices::Out] array<double>^% localNodeCoords,
					[System::Runtime::InteropServices::Out] int% numPrimaryNodes)
				{
					int dimTemp, orderTemp, numNodesTemp, numPrimaryNodesTemp;
					std::string elementNameTemp;
					std::vector<double> localNodeCoordTemp;

					gmsh::model::mesh::getElementProperties(elementType, elementNameTemp, dimTemp, orderTemp, numNodesTemp, localNodeCoordTemp, numPrimaryNodesTemp);

					localNodeCoords = gcnew array<double>(localNodeCoordTemp.size());
					if (localNodeCoordTemp.size() > 0)
						Marshal::Copy(IntPtr(localNodeCoordTemp.data()), localNodeCoords, 0, localNodeCoordTemp.size());

					dim = dimTemp;
					order = orderTemp;
					numNodes = numNodesTemp;
					numPrimaryNodes = numPrimaryNodesTemp;
					return gcnew System::String(elementNameTemp.c_str());


				}

				static void GetElements(
					[System::Runtime::InteropServices::Out] array<int>^% elementTypes,
					[System::Runtime::InteropServices::Out] array< array<IntPtr>^>^% elementTags,
					[System::Runtime::InteropServices::Out] array< array<IntPtr>^>^% nodeTags,
					int dim, int tag)
				{
					std::vector<int> elementTypesN;
					std::vector<std::vector<size_t>> elementTagsN, nodeTagsN;

					gmsh::model::mesh::getElements(elementTypesN, elementTagsN, nodeTagsN, dim, tag);

					for (int i = 0; i < nodeTagsN.size(); ++i)
					{
						for (int j = 0; j < nodeTagsN[i].size(); ++j)
						{
							if (nodeTagsN[i][j] == 0)
							{
								std::cout << "Bad index found in node tags: " << nodeTagsN[i][j] << " (i " << i << ", j " << j << ")" << std::endl;
								//throw std::exception("Abject failure.");
							}
						}
					}

					elementTypes = gcnew array<int>(elementTypesN.size());
					if (elementTypesN.size() > 0)
						Marshal::Copy(IntPtr(elementTypesN.data()), elementTypes, 0, elementTypesN.size());

					if (elementTagsN.size() < 1)
						elementTags = gcnew array <array<IntPtr>^>(1);
					else
						elementTags = gcnew array< array<IntPtr>^>(elementTagsN.size());

					for (int i = 0; i < elementTagsN.size(); ++i)
					{
						elementTags[i] = gcnew array<IntPtr>(elementTagsN[i].size());
						Marshal::Copy(IntPtr(elementTagsN[i].data()), elementTags[i], 0, elementTagsN[i].size());

						//for (int j = 0; j < elementTagsN[i].size(); ++j)
						//	elementTags[i][j] = static_cast<long>(elementTagsN[i][j]);
					}

					if (nodeTagsN.size() < 1)
						nodeTags = gcnew array< array<IntPtr>^>(1);
					else
						nodeTags = gcnew array< array<IntPtr>^>(nodeTagsN.size());

					for (int i = 0; i < nodeTagsN.size(); ++i)
					{
						nodeTags[i] = gcnew array<IntPtr>(nodeTagsN[i].size());
						Marshal::Copy(IntPtr(nodeTagsN[i].data()), nodeTags[i], 0, nodeTagsN[i].size());

						//for (int j = 0; j < nodeTagsN[i].size(); ++j)
						//{
						//	nodeTags[i][j] = (IntPtr)static_cast<long>(nodeTagsN[i][j]);
						//}
					}

				}

				static void RemoveDuplicateNodes()
				{
					gmsh::model::mesh::removeDuplicateNodes();
				}

				static void RemoveDuplicateElements()
				{
					gmsh::model::mesh::removeDuplicateElements();
				}

				static void CreateFaces()
				{
					gmsh::model::mesh::createFaces();
				}

				static void GetAllFaces(int dim, [System::Runtime::InteropServices::Out] array<IntPtr>^% faceTags, [System::Runtime::InteropServices::Out] array<IntPtr>^% faceNodes)
				{
					std::vector<size_t> face_tags, face_nodes;
					gmsh::model::mesh::getAllFaces(dim, face_tags, face_nodes);

					faceTags = gcnew array<IntPtr>(face_tags.size());
					Marshal::Copy(IntPtr(face_tags.data()), faceTags, 0, face_tags.size());

					//for (int i = 0; i < face_tags.size(); ++i)
					//	faceTags[i] = static_cast<long>(face_tags[i]);

					faceNodes = gcnew array<IntPtr>(face_nodes.size());
					Marshal::Copy(IntPtr(face_nodes.data()), faceNodes, 0, face_nodes.size());

					//for (int i = 0; i < face_nodes.size(); ++i)
					//	faceNodes[i] = static_cast<long>(face_nodes[i]);
				}

				static void GetJacobians(int elementType, int tag, array<double>^ localCoord,
					[System::Runtime::InteropServices::Out] array<double>^% jacobians,
					[System::Runtime::InteropServices::Out] array<double>^% determinants,
					[System::Runtime::InteropServices::Out] array<double>^% coord)
				{
					std::vector<double> nLocalCoord(localCoord->Length), nJacobians, nDeterminants, nCoord;
					Marshal::Copy(localCoord, 0, IntPtr(nLocalCoord.data()), localCoord->Length);

					gmsh::model::mesh::getJacobians(elementType, nLocalCoord, nJacobians, nDeterminants, nCoord, tag);

					jacobians = gcnew array<double>(nJacobians.size());
					determinants = gcnew array<double>(nDeterminants.size());
					coord = gcnew array<double>(nCoord.size());

					Marshal::Copy(IntPtr(nJacobians.data()), jacobians, 0, nJacobians.size());
					Marshal::Copy(IntPtr(nDeterminants.data()), determinants, 0, nDeterminants.size());
					Marshal::Copy(IntPtr(nCoord.data()), coord, 0, nCoord.size());
				}

				static array<IntPtr>^ Triangulate(array<double>^ coords)
				{
					std::vector<double> nCoords(coords->Length);
					Marshal::Copy(coords, 0, IntPtr(nCoords.data()), coords->Length);

					std::vector<size_t> nTris;
					gmsh::model::mesh::triangulate(nCoords, nTris);

					array<IntPtr>^ tris = gcnew array<IntPtr>(nTris.size());
					Marshal::Copy(IntPtr(nTris.data()), tris, 0, nTris.size());

					return tris;
				}

				static array<IntPtr>^ Tetrahedralize(array<double>^ coords)
				{
					if (coords == nullptr || coords->Length < 4) throw gcnew System::Exception("Invalid points for tetrahedralization.");

					std::vector<double> nCoords(coords->Length);
					Marshal::Copy(coords, 0, IntPtr(nCoords.data()), coords->Length);

					std::vector<size_t> nTetra;
					gmsh::model::mesh::tetrahedralize(nCoords, nTetra);

					array<IntPtr>^ tetra = gcnew array<IntPtr>(nTetra.size());
					Marshal::Copy(IntPtr(nTetra.data()), tetra, 0, nTetra.size());

					return tetra;
				}

				static void GetLocalCoordinatesInElement(int tag, double x, double y, double z, 
					[System::Runtime::InteropServices::Out] double u, [System::Runtime::InteropServices::Out] double v, [System::Runtime::InteropServices::Out] double w)
				{
					gmsh::model::mesh::getLocalCoordinatesInElement(tag, x, y, z, u, v, w);
				}

				static array<double>^ GetBarycenters(int elementType, int tag, bool fast, bool primary, int task, int numTasks)
				{
					std::vector<double> coords;
					gmsh::model::mesh::getBarycenters(elementType, tag, fast, primary, coords, task, numTasks);

					array<double>^ coordsOut = gcnew array<double>(coords.size());
					Marshal::Copy(IntPtr(coords.data()), coordsOut, 0, coords.size());

					return coordsOut;
				}

				static void SetSizeCallback(MeshSizeCallback^ callback)
				{
					IntPtr fptr = Marshal::GetFunctionPointerForDelegate(callback);

					typedef double(__stdcall* NativeCallback)(int, int, double, double, double, double);

					NativeCallback nativeCallback = static_cast<NativeCallback>(fptr.ToPointer());

					gmsh::model::mesh::setSizeCallback(nativeCallback);
				}

				static void GetElementFaceNodes(int elementType, int faceType, [System::Runtime::InteropServices::Out] array<IntPtr>^% nodeTags)
				{
					GetElementFaceNodes(elementType, faceType, nodeTags, -1, false);
				}

				static void GetElementFaceNodes(int elementType, int faceType, [System::Runtime::InteropServices::Out] array<IntPtr>^% nodeTags, int tag)
				{
					GetElementFaceNodes(elementType, faceType, nodeTags, tag, false);
				}

				static void GetElementFaceNodes(int elementType, int faceType, [System::Runtime::InteropServices::Out] array<IntPtr>^% nodeTags, int tag, bool primary)
				{
					std::vector<size_t> nNodeTags;

					gmsh::model::mesh::getElementFaceNodes(elementType, faceType, nNodeTags, tag, primary);

					nodeTags = gcnew array<IntPtr>(nNodeTags.size());
					Marshal::Copy(IntPtr(nNodeTags.data()), nodeTags, 0, nNodeTags.size());
				}


				ref class Field
				{
				public:
					static int Add(System::String^ fieldType, int tag)
					{
						return gmsh::model::mesh::field::add(msclr::interop::marshal_as<std::string>(fieldType), tag);
					}

					static double GetNumber(int tag, System::String^ option)
					{
						double value;
						gmsh::model::mesh::field::getNumber(tag, msclr::interop::marshal_as<std::string>(option), value);

						return value;
					}

					static array<double>^ GetNumbers(int tag, System::String^ option)
					{
						std::vector<double> values;
						gmsh::model::mesh::field::getNumbers(tag, msclr::interop::marshal_as<std::string>(option), values);

						array<double>^ valuesOut = gcnew array<double>(values.size());
						Marshal::Copy(IntPtr(values.data()), valuesOut, 0, values.size());

						return valuesOut;
					}

					static System::String^ GetString(int tag, System::String^ option)
					{
						std::string value;
						gmsh::model::mesh::field::getString(tag, msclr::interop::marshal_as<std::string>(option), value);

						return gcnew System::String(value.c_str());
					}

					static System::String^ GetType(int tag)
					{
						std::string type;
						gmsh::model::mesh::field::getType(tag, type);

						return gcnew System::String(type.c_str());
					}

					static array<int>^ List()
					{
						std::vector<int> tags;
						gmsh::model::mesh::field::list(tags);

						array<int>^ tagsOut = gcnew array<int>(tags.size());
						Marshal::Copy(IntPtr(tags.data()), tagsOut, 0, tags.size());

						return tagsOut;
					}

					static void Remove(int tag)
					{
						gmsh::model::mesh::field::remove(tag);
					}

					static void SetAsBackgroundMesh(int tag)
					{
						gmsh::model::mesh::field::setAsBackgroundMesh(tag);
					}

					static void SetAsBoundaryLayer(int tag)
					{
						gmsh::model::mesh::field::setAsBoundaryLayer(tag);
					}

					static void SetNumber(int tag, System::String^ option, double value)
					{
						gmsh::model::mesh::field::setNumber(tag, msclr::interop::marshal_as<std::string>(option), value);
					}

					static void SetNumbers(int tag, System::String^ option, array<double>^ values)
					{
						std::vector<double> nValues(values->Length);
						Marshal::Copy(values, 0, IntPtr(nValues.data()), values->Length);
						gmsh::model::mesh::field::setNumbers(tag, msclr::interop::marshal_as<std::string>(option), nValues);
					}

					static void SetString(int tag, System::String^ option, System::String^ value)
					{
						gmsh::model::mesh::field::setString(tag, msclr::interop::marshal_as<std::string>(option), msclr::interop::marshal_as<std::string>(value));
					}

				};
			};


			ref class Geo
			{
			public:
				static void Synchronize()
				{
					gmsh::model::geo::synchronize();
				}

				static int AddVolume(array<int>^ shellTags)
				{
					return AddVolume(shellTags, -1);
				}

				static int AddVolume(array<int>^ shellTags, int tag)
				{
					std::vector<int> nShellTags(shellTags->Length);
					Marshal::Copy(shellTags, 0, IntPtr(nShellTags.data()), shellTags->Length);

					return gmsh::model::geo::addVolume(nShellTags, tag);
				}


				static int AddSurfaceLoop(array<int>^ surfaceTags)
				{
					return AddSurfaceLoop(surfaceTags, -1);
				}

				static int AddSurfaceLoop(array<int>^ surfaceTags, int tag)
				{
					std::vector<int> nSurfaceTags(surfaceTags->Length);
					Marshal::Copy(surfaceTags, 0, IntPtr(nSurfaceTags.data()), surfaceTags->Length);

					return gmsh::model::geo::addSurfaceLoop(nSurfaceTags, tag);
				}

				static array<System::Tuple<int, int>^>^ GetBoundary(int dim, array<System::Tuple<int, int>^>^ tags)
				{
					gmsh::vectorpair dimTags(tags->Length);
					for (int i = 0; i < tags->Length; ++i)
					{
						dimTags[i] = std::pair<int, int>(tags[i]->Item1, tags[i]->Item2);
					}

					gmsh::vectorpair outDimTags;
					gmsh::model::getBoundary(dimTags, outDimTags, true, true, false);

					array<System::Tuple<int, int>^>^ output = gcnew array<System::Tuple<int, int>^>(outDimTags.size());
					for (int i = 0; i < outDimTags.size(); ++i)
					{
						output[i] = gcnew System::Tuple<int, int>(outDimTags[i].first, outDimTags[i].second);
					}

					return output;
				}
			};

			ref class OCC
			{
			public:

				static void Synchronize()
				{
					gmsh::model::occ::synchronize();
				}

				static void ImportShapes(System::String^ fileName, [System::Runtime::InteropServices::Out] array<System::Tuple<int, int>^>^% dimTags, System::Boolean highestDimOnly, System::String^ format)
				{
					gmsh::vectorpair outDimTags;
					gmsh::model::occ::importShapes(msclr::interop::marshal_as<std::string>(fileName), outDimTags, highestDimOnly, msclr::interop::marshal_as<std::string>(format));

					dimTags = gcnew array<System::Tuple<int, int>^>(outDimTags.size());
					for (int i = 0; i < outDimTags.size(); ++i)
						dimTags[i] = gcnew System::Tuple<int, int>(outDimTags[i].first, outDimTags[i].second);
				}

				static array<System::Tuple<int, int>^>^ GetEntities(int dim)
				{
					gmsh::vectorpair outDimTags;
					gmsh::model::occ::getEntities(outDimTags, dim);

					array<System::Tuple<int, int>^>^ dimTags = gcnew array<System::Tuple<int, int>^>(outDimTags.size());
					for (int i = 0; i < outDimTags.size(); ++i)
						dimTags[i] = gcnew System::Tuple<int, int>(outDimTags[i].first, outDimTags[i].second);

					return dimTags;
				}

				static void Remove(array<System::Tuple<int, int>^>^ dimTags)
				{
					Remove(dimTags, false);
				}

				static void Remove(array<System::Tuple<int, int>^>^ dimTags, System::Boolean recursive)
				{
					gmsh::vectorpair udimTags(dimTags->Length);
					for (int i = 0; i < dimTags->Length; ++i)
						udimTags.push_back(std::pair<int, int>(dimTags[i]->Item1, dimTags[i]->Item2));

					gmsh::model::occ::remove(udimTags, recursive);
				}

				static void RemoveAllDuplicates()
				{
					gmsh::model::occ::removeAllDuplicates();
				}

				static void Fragment(
					array<System::Tuple<int, int>^>^ objectDimTags,
					array<System::Tuple<int, int>^>^ toolDimTags,
					[System::Runtime::InteropServices::Out] array<System::Tuple<int, int>^>^% outDimTags,
					[System::Runtime::InteropServices::Out] array<array<System::Tuple<int, int>^>^>^% outDimTagsMap,
					System::Boolean removeObject,
					System::Boolean removeTool
				)
				{
					Fragment(objectDimTags, toolDimTags, outDimTags, outDimTagsMap, -1, removeObject, removeTool);
				}

				static void Fragment(
					array<System::Tuple<int, int>^>^ objectDimTags,
					array<System::Tuple<int, int>^>^ toolDimTags,
					[System::Runtime::InteropServices::Out] array<System::Tuple<int, int>^>^% outDimTags,
					[System::Runtime::InteropServices::Out] array<array<System::Tuple<int, int>^>^>^% outDimTagsMap,
					int tag,
					System::Boolean removeObject,
					System::Boolean removeTool
				)
				{
					gmsh::vectorpair noutDimTags, nobjectDimTags, ntoolDimTags;
					std::vector<gmsh::vectorpair> noutDimTagsMap;

					for (int i = 0; i < objectDimTags->Length; ++i)
						nobjectDimTags.push_back(std::pair<int, int>(objectDimTags[i]->Item1, objectDimTags[i]->Item2));

					for (int i = 0; i < toolDimTags->Length; ++i)
						ntoolDimTags.push_back(std::pair<int, int>(toolDimTags[i]->Item1, toolDimTags[i]->Item2));

					gmsh::model::occ::fragment(nobjectDimTags, ntoolDimTags, noutDimTags, noutDimTagsMap, tag, removeObject, removeTool);

					outDimTags = gcnew array < System::Tuple<int, int>^>(noutDimTags.size());
					for (int i = 0; i < noutDimTags.size(); ++i)
						outDimTags[i] = gcnew System::Tuple<int, int>(noutDimTags[i].first, noutDimTags[i].second);

					outDimTagsMap = gcnew array< array < System::Tuple<int, int>^>^>(noutDimTagsMap.size());
					for (int i = 0; i < noutDimTagsMap.size(); ++i)
					{
						outDimTagsMap[i] = gcnew array<System::Tuple<int, int>^>(noutDimTagsMap[i].size());
						for (int j = 0; j < noutDimTagsMap[i].size(); ++j)
							outDimTagsMap[i][j] = gcnew System::Tuple<int, int>(noutDimTagsMap[i][j].first, noutDimTagsMap[i][j].second);
					}

				}

				static void HealShapes(
					[System::Runtime::InteropServices::Out] array<System::Tuple<int, int>^>^% outDimTags,
					array<System::Tuple<int, int>^>^ dimTags,
					double tolerance, System::Boolean fixDegenerate,
					System::Boolean fixSmallEdges, System::Boolean fixSmallFaces,
					System::Boolean sewFaces, System::Boolean makeSolids)
				{
					gmsh::vectorpair noutDimTags, nDimTags;
					for (int i = 0; i < dimTags->Length; ++i)
						nDimTags.push_back(std::pair<int, int>(dimTags[i]->Item1, dimTags[i]->Item2));

					gmsh::model::occ::healShapes(noutDimTags, nDimTags, tolerance, fixDegenerate, fixSmallEdges, fixSmallFaces, sewFaces, makeSolids);

					//std::string msg = "Num dim tags: " + std::to_string(nDimTags.size()) + "   num outdimtags: " + std::to_string(noutDimTags.size());
					//throw gcnew System::Exception(gcnew System::String(msg.c_str()));


					outDimTags = gcnew array < System::Tuple<int, int>^>(noutDimTags.size());
					for (int i = 0; i < noutDimTags.size(); ++i)
						outDimTags[i] = gcnew System::Tuple<int, int>(noutDimTags[i].first, noutDimTags[i].second);
				}


				static void Intersect(
					array<System::Tuple<int, int>^>^ objectDimTags,
					array<System::Tuple<int, int>^>^ toolDimTags,
					[System::Runtime::InteropServices::Out] array<System::Tuple<int, int>^>^% outDimTags,
					[System::Runtime::InteropServices::Out] array<array<System::Tuple<int, int>^>^>^% outDimTagsMap,
					int tag,
					System::Boolean removeObject,
					System::Boolean removeTool
				)
				{
					gmsh::vectorpair noutDimTags, nobjectDimTags, ntoolDimTags;
					std::vector<gmsh::vectorpair> noutDimTagsMap;

					for (int i = 0; i < objectDimTags->Length; ++i)
						nobjectDimTags.push_back(std::pair<int, int>(objectDimTags[i]->Item1, objectDimTags[i]->Item2));

					for (int i = 0; i < toolDimTags->Length; ++i)
						ntoolDimTags.push_back(std::pair<int, int>(toolDimTags[i]->Item1, toolDimTags[i]->Item2));

					gmsh::model::occ::intersect(nobjectDimTags, ntoolDimTags, noutDimTags, noutDimTagsMap, tag, removeObject, removeTool);

					outDimTags = gcnew array < System::Tuple<int, int>^>(noutDimTags.size());
					for (int i = 0; i < noutDimTags.size(); ++i)
						outDimTags[i] = gcnew System::Tuple<int, int>(noutDimTags[i].first, noutDimTags[i].second);

					outDimTagsMap = gcnew array< array < System::Tuple<int, int>^>^>(noutDimTagsMap.size());
					for (int i = 0; i < noutDimTagsMap.size(); ++i)
					{
						outDimTagsMap[i] = gcnew array<System::Tuple<int, int>^>(noutDimTagsMap[i].size());
						for (int j = 0; j < noutDimTagsMap[i].size(); ++j)
							outDimTagsMap[i][j] = gcnew System::Tuple<int, int>(noutDimTagsMap[i][j].first, noutDimTagsMap[i][j].second);
					}
				}

				static void Cut(
					array<System::Tuple<int, int>^>^ objectDimTags,
					array<System::Tuple<int, int>^>^ toolDimTags,
					[System::Runtime::InteropServices::Out] array<System::Tuple<int, int>^>^% outDimTags,
					[System::Runtime::InteropServices::Out] array<array<System::Tuple<int, int>^>^>^% outDimTagsMap,
					int tag,
					System::Boolean removeObject,
					System::Boolean removeTool
				)
				{
					gmsh::vectorpair noutDimTags, nobjectDimTags, ntoolDimTags;
					std::vector<gmsh::vectorpair> noutDimTagsMap;

					for (int i = 0; i < objectDimTags->Length; ++i)
						nobjectDimTags.push_back(std::pair<int, int>(objectDimTags[i]->Item1, objectDimTags[i]->Item2));

					for (int i = 0; i < toolDimTags->Length; ++i)
						ntoolDimTags.push_back(std::pair<int, int>(toolDimTags[i]->Item1, toolDimTags[i]->Item2));

					gmsh::model::occ::cut(nobjectDimTags, ntoolDimTags, noutDimTags, noutDimTagsMap, tag, removeObject, removeTool);

					outDimTags = gcnew array < System::Tuple<int, int>^>(noutDimTags.size());
					for (int i = 0; i < noutDimTags.size(); ++i)
						outDimTags[i] = gcnew System::Tuple<int, int>(noutDimTags[i].first, noutDimTags[i].second);

					outDimTagsMap = gcnew array< array < System::Tuple<int, int>^>^>(noutDimTagsMap.size());
					for (int i = 0; i < noutDimTagsMap.size(); ++i)
					{
						outDimTagsMap[i] = gcnew array<System::Tuple<int, int>^>(noutDimTagsMap[i].size());
						for (int j = 0; j < noutDimTagsMap[i].size(); ++j)
							outDimTagsMap[i][j] = gcnew System::Tuple<int, int>(noutDimTagsMap[i][j].first, noutDimTagsMap[i][j].second);
					}
				}

				static int AddBox(double x, double y, double z, double dx, double dy, double dz)
				{
					return gmsh::model::occ::addBox(x, y, z, dx, dy, dz, -1);
				}

				static int AddBox(double x, double y, double z, double dx, double dy, double dz, int tag)
				{
					return gmsh::model::occ::addBox(x, y, z, dx, dy, dz, tag);
				}

				static int AddPoint(double x, double y, double z, int tag)
				{
					return AddPoint(x, y, z, 0.0, tag);
				}

				static int AddPoint(double x, double y, double z)
				{
					return AddPoint(x, y, z, 0, -1);
				}

				static int AddPoint(double x, double y, double z, double meshSize, int tag)
				{
					return gmsh::model::occ::addPoint(x, y, z, meshSize, tag); 
				}

				static int AddRectangle(double x, double y, double z, double dx, double dy, int tag, double roundedRadius)
				{
					return gmsh::model::occ::addRectangle(x, y, z, dx, dy, tag, roundedRadius);
				}

				static int AddRectangle(double x, double y, double z, double dx, double dy, double roundedRadius)
				{
					return gmsh::model::occ::addRectangle(x, y, z, dx, dy, -1, roundedRadius);
				}

				static int AddRectangle(double x, double y, double z, double dx, double dy)
				{
					return gmsh::model::occ::addRectangle(x, y, z, dx, dy, -1);
				}

				static int AddLine(int startTag, int endTag, int tag)
				{
					return gmsh::model::occ::addLine(startTag, endTag, tag);
				}

				static int AddLine(int startTag, int endTag)
				{
					return gmsh::model::occ::addLine(startTag, endTag);
				}

				static int AddBSpline(array<int>^ pointTags, int tag, int degree, array<double>^ weights, array<double>^ knots, array<int>^ multiplicities)
				{
					std::vector<int> pointTags_native(pointTags->Length), multiplicities_native(multiplicities->Length);
					std::vector<double> weights_native(weights->Length), knots_native(knots->Length);

					Marshal::Copy(pointTags, 0, IntPtr(pointTags_native.data()), pointTags->Length);
					Marshal::Copy(weights, 0, IntPtr(weights_native.data()), weights->Length);
					Marshal::Copy(knots, 0, IntPtr(knots_native.data()), knots->Length);
					Marshal::Copy(multiplicities, 0, IntPtr(multiplicities_native.data()), multiplicities->Length);

					return gmsh::model::occ::addBSpline(pointTags_native, tag, degree, weights_native, knots_native, multiplicities_native);
				}

				static int AddBSpline(array<int>^ pointTags, int tag, int degree, array<double>^ weights)
				{
					std::vector<int> pointTags_native(pointTags->Length);
					std::vector<double> weights_native(weights->Length);

					Marshal::Copy(pointTags, 0, IntPtr(pointTags_native.data()), pointTags->Length);
					Marshal::Copy(weights, 0, IntPtr(weights_native.data()), weights->Length);


					return gmsh::model::occ::addBSpline(pointTags_native, tag, degree, weights_native);
				}

				static int AddTrimmedSurface(int surfaceTag, array<int>^ wireTags, bool wire3D, int tag)
				{
					std::vector<int> wireTags_native(wireTags->Length);
					Marshal::Copy(wireTags, 0, IntPtr(wireTags_native.data()), wireTags->Length);

					return gmsh::model::occ::addTrimmedSurface(surfaceTag, wireTags_native, wire3D, tag);
				}

				static int AddTrimmedSurface(int surfaceTag, array<int>^ wireTags, bool wire3D)
				{
					return AddTrimmedSurface(surfaceTag, wireTags, wire3D, -1);
				}

				static int AddTrimmedSurface(int surfaceTag, array<int>^ wireTags)
				{
					return AddTrimmedSurface(surfaceTag, wireTags, true, -1);
				}


				static int AddBSplineSurface(
					array<int>^ pointTags, int numPointsU,
					int tag, int degreeU, int degreeV,
					array<double>^ weights,
					array<double>^ knotsU, array<double>^ knotsV,
					array<int>^ multiplicitiesU, array<int>^ multiplicitiesV,
					array<int>^ wireTags, bool wire3d)
				{
					std::vector<int> pointTags_native(pointTags->Length),
						multiplicitiesU_native(multiplicitiesU->Length), multiplicitiesV_native(multiplicitiesV->Length),
						wireTags_native(wireTags->Length);
					std::vector<double> weights_native(weights->Length), knotsU_native(knotsU->Length), knotsV_native(knotsV->Length);

					Marshal::Copy(pointTags, 0, IntPtr(pointTags_native.data()), pointTags->Length);
					Marshal::Copy(weights, 0, IntPtr(weights_native.data()), weights->Length);
					Marshal::Copy(knotsU, 0, IntPtr(knotsU_native.data()), knotsU->Length);
					Marshal::Copy(knotsV, 0, IntPtr(knotsV_native.data()), knotsV->Length);
					Marshal::Copy(multiplicitiesU, 0, IntPtr(multiplicitiesU_native.data()), multiplicitiesU->Length);
					Marshal::Copy(multiplicitiesV, 0, IntPtr(multiplicitiesV_native.data()), multiplicitiesV->Length);
					Marshal::Copy(wireTags, 0, IntPtr(wireTags_native.data()), wireTags->Length);

					return gmsh::model::occ::addBSplineSurface(pointTags_native, numPointsU, tag, degreeU, degreeV, weights_native,
						knotsU_native, knotsV_native,
						multiplicitiesU_native, multiplicitiesV_native,
						wireTags_native, wire3d);

				}

				static int AddBSplineSurface(
					array<int>^ pointTags, int numPointsU,
					int tag, int degreeU, int degreeV,
					array<double>^ weights,
					array<double>^ knotsU, array<double>^ knotsV,
					array<int>^ multiplicitiesU, array<int>^ multiplicitiesV)
				{
					std::vector<int> pointTags_native(pointTags->Length),
						multiplicitiesU_native(multiplicitiesU->Length), multiplicitiesV_native(multiplicitiesV->Length);
					std::vector<double> weights_native(weights->Length), knotsU_native(knotsU->Length), knotsV_native(knotsV->Length);

					Marshal::Copy(pointTags, 0, IntPtr(pointTags_native.data()), pointTags->Length);
					Marshal::Copy(weights, 0, IntPtr(weights_native.data()), weights->Length);
					Marshal::Copy(knotsU, 0, IntPtr(knotsU_native.data()), knotsU->Length);
					Marshal::Copy(knotsV, 0, IntPtr(knotsV_native.data()), knotsV->Length);
					Marshal::Copy(multiplicitiesU, 0, IntPtr(multiplicitiesU_native.data()), multiplicitiesU->Length);
					Marshal::Copy(multiplicitiesV, 0, IntPtr(multiplicitiesV_native.data()), multiplicitiesV->Length);

					return gmsh::model::occ::addBSplineSurface(pointTags_native, numPointsU, tag, degreeU, degreeV, weights_native,
						knotsU_native, knotsV_native,
						multiplicitiesU_native, multiplicitiesV_native);

				}

				static int AddPlaneSurface(array<int>^ wireTags)
				{
					return AddPlaneSurface(wireTags, -1);
				}

				static int AddPlaneSurface(array<int>^ wireTags, int tag)
				{
					std::vector<int> wireTags_native(wireTags->Length);
					Marshal::Copy(wireTags, 0, IntPtr(wireTags_native.data()), wireTags->Length);

					return gmsh::model::occ::addPlaneSurface(wireTags_native, tag);
				}

				static int AddBSplineSurface(
					array<int>^ pointTags, int numPointsU,
					int tag, int degreeU, int degreeV,
					array<double>^ weights,
					array<int>^ wireTags, bool wire3d)
				{
					std::vector<int> pointTags_native(pointTags->Length),
						wireTags_native(wireTags->Length);
					std::vector<double> weights_native(weights->Length);

					Marshal::Copy(pointTags, 0, IntPtr(pointTags_native.data()), pointTags->Length);
					Marshal::Copy(weights, 0, IntPtr(weights_native.data()), weights->Length);
					Marshal::Copy(wireTags, 0, IntPtr(wireTags_native.data()), wireTags->Length);

					return gmsh::model::occ::addBSplineSurface(pointTags_native, numPointsU, tag, degreeU, degreeV, weights_native,
						std::vector<double>(), std::vector<double>(),
						std::vector<int>(), std::vector<int>(),
						wireTags_native, wire3d);
				}

				static int AddBSplineSurface(
					array<int>^ pointTags, int numPointsU,
					int tag, int degreeU, int degreeV,
					array<double>^ weights)
				{
					std::vector<int> pointTags_native(pointTags->Length);
					std::vector<double> weights_native(weights->Length);

					Marshal::Copy(pointTags, 0, IntPtr(pointTags_native.data()), pointTags->Length);
					Marshal::Copy(weights, 0, IntPtr(weights_native.data()), weights->Length);

					return gmsh::model::occ::addBSplineSurface(pointTags_native, numPointsU, tag, degreeU, degreeV, weights_native,
						std::vector<double>(), std::vector<double>(),
						std::vector<int>(), std::vector<int>());
				}

				static int AddCone(double x, double y, double z, double dx, double dy, double dz, double r1, double r2, int tag, double angle)
				{
					return gmsh::model::occ::addCone(x, y, z, dx, dy, dz, r1, r2, tag, angle);
				}

				static int AddCone(double x, double y, double z, double dx, double dy, double dz, double r1, double r2)
				{
					return gmsh::model::occ::addCone(x, y, z, dx, dy, dz, r1, r2, -1);
				}

				static int AddCircle(double x, double y, double z, double r, int tag, double angle1, double angle2, array<double>^ zAxis, array<double>^ xAxis)
				{
					if (zAxis->Length != 3 || xAxis->Length != 3) throw gcnew System::Exception("Axes need to be length 3.");

					std::vector<double> zAxis_native(3), xAxis_native(3);
					Marshal::Copy(zAxis, 0, IntPtr(zAxis_native.data()), zAxis->Length);
					Marshal::Copy(xAxis, 0, IntPtr(xAxis_native.data()), xAxis->Length);

					return gmsh::model::occ::addCircle(x, y, z, r, tag, angle1, angle2, zAxis_native, xAxis_native);
				}

				static int AddCircle(double x, double y, double z, double r, int tag, double angle1, double angle2, array<double>^ zAxis)
				{
					if (zAxis->Length != 3) throw gcnew System::Exception("Axes need to be length 3.");

					std::vector<double> zAxis_native(3);
					Marshal::Copy(zAxis, 0, IntPtr(zAxis_native.data()), zAxis->Length);

					return gmsh::model::occ::addCircle(x, y, z, r, tag, angle1, angle2, zAxis_native);
				}

				static int AddCircle(double x, double y, double z, double r)
				{
					return gmsh::model::occ::addCircle(x, y, z, r);
				}

				static array < System::Tuple<int, int>^>^ Extrude(array<double>^ dimTags, double dx, double dy, double dz)
				{
					gmsh::vectorpair dimTags_native(dimTags->Length), outDimTags_native;
					Marshal::Copy(dimTags, 0, IntPtr(dimTags_native.data()), dimTags->Length);

					gmsh::model::occ::extrude(dimTags_native, dx, dy, dz, outDimTags_native);

					array < System::Tuple<int, int>^>^ outDimTags = gcnew array < System::Tuple<int, int>^>(outDimTags_native.size());

					for (int i = 0; i < outDimTags_native.size(); ++i)
						outDimTags[i] = gcnew System::Tuple<int, int>(outDimTags_native[i].first, outDimTags_native[i].second);

					return outDimTags;
				}

				static int AddWire(array<int>^ curveTags, int tag, bool checkClosed)
				{
					std::vector<int> curveTags_native(curveTags->Length);
					Marshal::Copy(curveTags, 0, IntPtr(curveTags_native.data()), curveTags->Length);

					return gmsh::model::occ::addWire(curveTags_native, tag, checkClosed);
				}

				static int AddSurfaceLoop(array<int>^ surfaceTags)
				{
					return AddSurfaceLoop(surfaceTags, -1);
				}

				static int AddSurfaceLoop(array<int>^ surfaceTags, int tag)
				{
					std::vector<int> nSurfaceTags(surfaceTags->Length);
					Marshal::Copy(surfaceTags, 0, IntPtr(nSurfaceTags.data()), surfaceTags->Length);

					return gmsh::model::occ::addSurfaceLoop(nSurfaceTags, tag);
				}

				static int AddVolume(array<int>^ shellTags)
				{
					return AddVolume(shellTags, -1);
				}

				static int AddVolume(array<int>^ shellTags, int tag)
				{
					std::vector<int> nShellTags(shellTags->Length);
					Marshal::Copy(shellTags, 0, IntPtr(nShellTags.data()), shellTags->Length);

					return gmsh::model::occ::addVolume(nShellTags, tag);
				}
			};

			ref class Field
			{
			public:

				int Add(System::String^ fieldName)
				{
					return Add(fieldName, -1);
				}

				int Add(System::String^ fieldName, int tag)
				{
					return gmsh::model::mesh::field::add(msclr::interop::marshal_as<std::string>(fieldName), tag);
				}

				double GetNumber(int tag, System::String^ option)
				{
					double value;
					gmsh::model::mesh::field::getNumber(tag, msclr::interop::marshal_as<std::string>(option), value);
					return value;
				}

				array<double>^ GetNumbers(int tag, System::String^ option)
				{
					std::vector<double> nValues;
					gmsh::model::mesh::field::getNumbers(tag, msclr::interop::marshal_as<std::string>(option), nValues);

					array<double>^ values = gcnew array<double>(nValues.size());
					Marshal::Copy(IntPtr(nValues.data()), values, 0, nValues.size());

					return values;
				}

				System::String^ GetString(int tag, System::String^ option)
				{
					std::string value;
					gmsh::model::mesh::field::getString(tag, msclr::interop::marshal_as<std::string>(option), value);
					return gcnew System::String(value.c_str());
				}

				System::String^ GetType(int tag)
				{
					std::string value;
					gmsh::model::mesh::field::getType(tag, value);
					return gcnew System::String(value.c_str());
				}

				array<int>^ List()
				{
					std::vector<int> nTags;
					gmsh::model::mesh::field::list(nTags);

					array<int>^ tags = gcnew array<int>(nTags.size());
					Marshal::Copy(IntPtr(nTags.data()), tags, 0, nTags.size());

					return tags;
				}

				void Remove(int tag)
				{
					gmsh::model::mesh::field::remove(tag);
				}

				void SetAsBackgroundMesh(int tag)
				{
					gmsh::model::mesh::field::setAsBackgroundMesh(tag);
				}

				void SetAsBoundaryLayer(int tag)
				{
					gmsh::model::mesh::field::setAsBoundaryLayer(tag);
				}

				void SetNumber(int tag, System::String^ option, double value)
				{
					gmsh::model::mesh::field::setNumber(tag, msclr::interop::marshal_as<std::string>(option), value);
				}

				void SetNumbers(int tag, System::String^ option, array<double>^ values)
				{
					std::vector<double> nValues(values->Length);
					Marshal::Copy(values, 0, IntPtr(nValues.data()), values->Length);

					gmsh::model::mesh::field::setNumbers(tag, msclr::interop::marshal_as<std::string>(option), nValues);
				}

				void SetString(int tag, System::String^ option, System::String^ value)
				{
					gmsh::model::mesh::field::setString(tag, msclr::interop::marshal_as<std::string>(option), msclr::interop::marshal_as<std::string>(value));
				}
			};
		};

		ref class Option
		{
		public:
			static void SetNumber(System::String^ parameter, double value)
			{
				gmsh::option::setNumber(msclr::interop::marshal_as<std::string>(parameter), value);
			}

			static void SetString(System::String^ parameter, System::String^ value)
			{
				gmsh::option::setString(msclr::interop::marshal_as<std::string>(parameter), msclr::interop::marshal_as<std::string>(value));
			}

			static double GetNumber(System::String^ parameter)
			{
				double value = 0;
				gmsh::option::getNumber(msclr::interop::marshal_as<std::string>(parameter), value);
				return value;
			}

			static System::String^ GetString(System::String^ parameter)
			{
				std::string value;
				gmsh::option::getString(msclr::interop::marshal_as<std::string>(parameter), value);

				return gcnew System::String(value.c_str());
			}
		};



	};
}
