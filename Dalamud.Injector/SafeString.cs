using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Injector
{
    /// <summary>
    /// A string that allocates and frees itself.
    /// </summary>
    internal class SafeString : IDisposable
    {
        private readonly Process process;
        private readonly byte[] data;
        private IntPtr addr = IntPtr.Zero;

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeString"/> class.
        /// This class handles allocating and freeing a UTF-8 string from memory.
        /// </summary>
        /// <param name="process">Process to interact with.</param>
        /// <param name="data">String to allocate.</param>
        public SafeString(Process process, string data)
        {
            this.process = process;
            this.data = Encoding.UTF8.GetBytes(data + '\0');

            this.Alloc();
            this.Write();
        }

        /// <summary>
        /// Gets the allocated address.
        /// </summary>
        public IntPtr Address => this.addr;

        /// <summary>
        /// Dispose of this object.
        /// </summary>
        public void Dispose()
        {
            if (this.addr != IntPtr.Zero)
            {
                this.Free();
            }
        }

        private static bool CheckLastWin32Error()
        {
            return Marshal.GetLastWin32Error() != 0;
        }

        /// <summary>
        /// Allocate the data.
        /// </summary>
        private void Alloc()
        {
            this.addr = NativeFunctions.VirtualAllocEx(
                this.process.Handle,
                IntPtr.Zero,
                this.data.Length,
                NativeFunctions.AllocationType.Commit,
                NativeFunctions.MemoryProtection.ReadWrite);

            if (this.addr == IntPtr.Zero || CheckLastWin32Error())
            {
                throw new Exception($"Unable to alloc memory");
            }
        }

        /// <summary>
        /// Write the data.
        /// </summary>
        private void Write()
        {
            var result = NativeFunctions.WriteProcessMemory(
                this.process.Handle,
                this.addr,
                this.data,
                this.data.Length,
                out var _);

            if (!result || CheckLastWin32Error())
            {
                throw new Exception($"Unable to write memory");
            }
        }

        /// <summary>
        /// Free the data.
        /// </summary>
        private void Free()
        {
            var result = NativeFunctions.VirtualFreeEx(
                this.process.Handle,
                this.addr,
                0,
                NativeFunctions.AllocationType.Release);

            if (!result || CheckLastWin32Error())
            {
                throw new Exception($"Unable to free memory");
            }

            this.addr = IntPtr.Zero;
        }
    }
}
