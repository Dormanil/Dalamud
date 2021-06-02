using System;
using System.Runtime.CompilerServices;

/*
using CoreHook;

namespace Dalamud.Hooking
{
    /// <summary>
    /// Manages a hook which can be used to intercept a call to native function.
    /// This class is basically a thin wrapper around the LocalHook type to provide helper functions.
    /// </summary>
    /// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
    public sealed class Hook<T> : IDisposable, IDalamudHook where T : Delegate
    {
        private readonly IntPtr address;
        private readonly IHook<T> hookInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hook{T}"/> class.
        /// Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="address">A memory address to install a hook.</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        public Hook(IntPtr address, T detour)
        {
            this.hookInfo = HookFactory.CreateHook(address, detour);
            this.address = address;
        }

        /// <summary>
        /// Gets a memory address of the target function.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
        public IntPtr Address
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.CheckDisposed();
                return this.address;
            }
        }

        /// <summary>
        /// Gets a delegate function that can be used to call the actual function as if function is not hooked yet.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
        public T Original
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.CheckDisposed();
                return this.hookInfo.Original;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the hook is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                this.CheckDisposed();
                return this.hookInfo.ThreadACL.IsExclusive;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the hook has been disabled.
        /// </summary>
        public bool IsDisabled => !this.IsEnabled;

        /// <summary>
        /// Gets a value indicating whether or not the hook has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
        /// Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="moduleName">A name of the module currently loaded in the memory (e.g. ws2_32.dll).</param>
        /// <param name="exportName">A name of the exported function name (e.g. send).</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <returns>A new hook.</returns>
        public static Hook<T> FromSymbol(string moduleName, string exportName, T detour)
        {
            var address = LocalHook.GetProcAddress(moduleName, exportName);

            return new Hook<T>(address, detour);
        }

        /// <summary>
        /// Remove a hook from the current process.
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.hookInfo.Dispose();

            this.IsDisposed = true;
        }

        /// <summary>
        /// Starts intercepting a call to the function.
        /// </summary>
        public void Enable()
        {
            this.CheckDisposed();

            this.hookInfo.ThreadACL.SetExclusiveACL(null);
        }

        /// <summary>
        /// Stops intercepting a call to the function.
        /// </summary>
        public void Disable()
        {
            this.CheckDisposed();

            this.hookInfo.ThreadACL.SetInclusiveACL(null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException("Hook is already disposed.");
            }
        }
    }
}
*/

using Reloaded.Hooks.Definitions;

namespace Dalamud.Hooking
{
    /// <summary>
    /// Manages a hook which can be used to intercept a call to native function.
    /// This class is basically a thin wrapper around the LocalHook type to provide helper functions.
    /// </summary>
    /// <typeparam name="T">Delegate type to represents a function prototype. This must be the same prototype as original function do.</typeparam>
    public sealed class Hook<T> : IDisposable, IDalamudHook where T : Delegate
    {
        private readonly IntPtr address;
        private readonly IHook<T> hookInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hook{T}"/> class.
        /// A hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="address">A memory address to install a hook.</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        public Hook(IntPtr address, T detour)
        {
            this.hookInfo = new Reloaded.Hooks.Hook<T>(detour, address.ToInt64());
            this.address = address;
        }

        /// <summary>
        /// Gets a memory address of the target function.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
        public IntPtr Address
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.CheckDisposed();
                return this.address;
            }
        }

        /// <summary>
        /// Gets a delegate function that can be used to call the actual function as if function is not hooked yet.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Hook is already disposed.</exception>
        public T Original
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.CheckDisposed();
                return this.hookInfo.OriginalFunction;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the hook is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                this.CheckDisposed();
                return this.hookInfo.IsHookEnabled;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the hook has been disabled.
        /// </summary>
        public bool IsDisabled
        {
            get
            {
                this.CheckDisposed();
                return !this.hookInfo.IsHookEnabled;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the hook has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Creates a hook. Hooking address is inferred by calling to GetProcAddress() function.
        /// Hook is not activated until Enable() method is called.
        /// </summary>
        /// <param name="moduleName">A name of the module currently loaded in the memory (e.g. ws2_32.dll).</param>
        /// <param name="exportName">A name of the exported function name (e.g. send).</param>
        /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
        /// <returns>A new hook.</returns>
        public static Hook<T> FromSymbol(string moduleName, string exportName, T detour)
        {
            // Get a function address from the symbol name.
            var moduleHandle = NativeFunctions.GetModuleHandleW(moduleName);
            if (moduleHandle == IntPtr.Zero)
            {
                throw new DllNotFoundException($"The given library is not loaded into the current process: {moduleName}");
            }

            var address = NativeFunctions.GetProcAddress(moduleHandle, exportName);
            if (address == IntPtr.Zero)
            {
                throw new MissingMethodException($"The given method does not exist: {moduleName}.{exportName}");
            }

            return new Hook<T>(address, detour);
        }

        /// <summary>
        /// Remove a hook from the current process.
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed)
                return;

            this.hookInfo.Disable();

            this.IsDisposed = true;
        }

        /// <summary>
        /// Starts intercepting a call to the function.
        /// </summary>
        public void Enable()
        {
            if (!this.hookInfo.IsHookActivated)
            {
                this.hookInfo.Activate();
            }

            this.CheckDisposed();

            this.hookInfo.Enable();
        }

        /// <summary>
        /// Stops intercepting a call to the function.
        /// </summary>
        public void Disable()
        {
            this.CheckDisposed();

            this.hookInfo.Disable();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException("Hook is already disposed.");
            }
        }
    }
}
