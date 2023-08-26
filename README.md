<img src="img/GmshCommon_example01.png" width=640/>

# GmshCommon
.NET/CLI wrapper for Gmsh (https://gmsh.info)

- `GmshCommon.dll`: The .NET wrapper.
- `Gmsh.GH.gha`: A (very WIP) plug-in for Rhino/Grasshopper for meshing geometry within GH.

## Notes
See the Gmsh SDK installation notes for compiling with VS Studio:
```
  * The C++ API will only work if your compiler has an Application Binary
    Interface (ABI) that is compatible with the ABI of the compiler used to
    build this SDK (see above for the compiler ID and version).
  * If your C++ compiler does not have a compatible ABI and if there are no
    compatibility flags available, you can rename `gmsh.h_cwrap' as `gmsh.h':
    this implementation redefines the C++ API in terms of the C API. Using this
    header will lead to (slightly) reduced performance compared to using the
    native Gmsh C++ API from the original `gmsh.h' header, as it entails
    additional data copies between this C++ wrapper, the C API and the native
    C++ code.
```