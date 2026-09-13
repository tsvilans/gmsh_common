using System;
using System.Linq;
using System.Threading;

namespace GmshCommon
{
    /// <summary>
    /// Coordinates access to the single, process-wide Gmsh session.
    ///
    /// Gmsh itself is a global native library: gmsh::initialize()/finalize() bracket one
    /// session per process, and "the current model" is a single mutable pointer shared by
    /// every call into the API - not a handle you pass around, and not thread-safe.
    ///
    /// This class:
    ///  - initializes Gmsh lazily, once, on first use (never per-solve);
    ///  - lets callers work with their own named model without stomping on anyone else's;
    ///  - serializes ALL access behind one lock, so that "switch to my model" and "do my
    ///    work" always happen as one atomic step. Two components racing to set the current
    ///    model is a real failure mode in Grasshopper (multiple open documents, background
    ///    solving, parallel components), not just a theoretical one.
    ///
    /// Usage:
    ///   using (GmshSession.UseFreshModel(_modelName))
    ///   {
    ///       // ... build geometry/mesh from scratch in _modelName ...
    ///   }
    ///
    ///   using (GmshSession.UseModel(_modelName))
    ///   {
    ///       // ... _modelName already exists; just query it ...
    ///   }
    ///
    /// Do not call Gmsh.InitializeGmsh(), Gmsh.FinalizeGmsh() or Gmsh.Clear() anywhere else -
    /// Clear() in particular wipes every model in the session, not just the caller's own.
    /// </summary>
    public static class GmshSession
    {
        private static readonly object _gate = new object();
        private static bool _initialized;

        /// <summary>
        /// Enters the session for <paramref name="modelName"/>, creating it fresh: if a model
        /// with that name already exists it is removed first, then a new empty one is added
        /// and made current. Use this when you're rebuilding a component's model from scratch
        /// (e.g. new input geometry arrived on this solve).
        /// </summary>
        public static IDisposable UseFreshModel(string modelName)
        {
            Monitor.Enter(_gate);
            try
            {
                EnsureInitialized();

                if (ModelExistsUnsafe(modelName))
                {
                    Gmsh.Model.SetCurrent(modelName);
                    Gmsh.Model.Remove();
                }

                Gmsh.Model.Add(modelName);
                return new Releaser();
            }
            catch
            {
                Monitor.Exit(_gate);
                throw;
            }
        }

        /// <summary>
        /// Enters the session for <paramref name="modelName"/>, which must already exist
        /// (created previously via <see cref="UseFreshModel"/>). Use this to repeatedly query
        /// a model that's being kept alive across solves, without rebuilding it.
        /// </summary>
        public static IDisposable UseModel(string modelName)
        {
            Monitor.Enter(_gate);
            try
            {
                EnsureInitialized();
                Gmsh.Model.SetCurrent(modelName);
                return new Releaser();
            }
            catch
            {
                Monitor.Exit(_gate);
                throw;
            }
        }

        /// <summary>
        /// Removes <paramref name="modelName"/> if it exists. Safe to call even if Gmsh was
        /// never initialized (e.g. a component removed from the canvas before ever solving).
        /// Call this from a component's RemovedFromDocument override.
        /// </summary>
        public static void RemoveModel(string modelName)
        {
            lock (_gate)
            {
                if (!_initialized) return;

                if (ModelExistsUnsafe(modelName))
                {
                    Gmsh.Model.SetCurrent(modelName);
                    Gmsh.Model.Remove();
                }
            }
        }

        /// <summary>True if a model with this name currently exists in the session.</summary>
        public static bool ModelExists(string modelName)
        {
            lock (_gate)
            {
                if (!_initialized) return false;
                return ModelExistsUnsafe(modelName);
            }
        }

        private static bool ModelExistsUnsafe(string modelName)
        {
            return Gmsh.Model.List().Contains(modelName);
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            Gmsh.InitializeGmsh();
            _initialized = true;
        }

        private sealed class Releaser : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Monitor.Exit(_gate);
            }
        }
    }
}
