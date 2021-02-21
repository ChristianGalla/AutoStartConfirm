using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace AutoStartConfirm.Helpers
{
    public class FolderChangeMonitor : IDisposable {
        #region Fields
        private bool disposedValue;
        #endregion

        #region Methods

        public void Start() {
            throw new NotImplementedException();
        }

        public void Stop() {
            throw new NotImplementedException();
        }

        #endregion

        // @todo
        //#region Events
        //public event FolderChangeHandler Changed;
        //public event FolderChangeHandler Error;
        //#endregion

        #region Dispose

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FolderChangeMonitor()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
