// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    /// <summary>
    /// A LIFO semaphore implemented using Win32 IO Completion Ports.
    /// </summary>
    /// <remarks>
    /// IO Completion ports release waiting threads in LIFO order, so we can use them to create a LIFO semaphore.
    /// See https://msdn.microsoft.com/en-us/library/windows/desktop/aa365198(v=vs.85).aspx under How I/O Completion Ports Work.
    /// From the docs "Threads that block their execution on an I/O completion port are released in last-in-first-out (LIFO) order."
    /// </remarks>
    internal sealed class LowLevelLifoSemaphore : IDisposable
    {
        private IntPtr _completionPort;

        public LowLevelLifoSemaphore(int initialSignalCount, int maximumSignalCount)
        {
            Debug.Assert(initialSignalCount >= 0, "Windows LowLevelLifoSemaphore does not support a negative signal count"); // TODO: Track actual signal count to enable this
            Debug.Assert(maximumSignalCount > 0);
            Debug.Assert(initialSignalCount <= maximumSignalCount);

            _completionPort =
                Interop.Kernel32.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, UIntPtr.Zero, maximumSignalCount);
            if (_completionPort == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                var exception = new OutOfMemoryException();
                exception.HResult = error;
                throw exception;
            }
            Release(initialSignalCount);
        }

        ~LowLevelLifoSemaphore()
        {
            if (_completionPort != IntPtr.Zero)
            {
                Dispose();
            }
        }

        public bool Wait(int timeoutMs)
        {
            Debug.Assert(timeoutMs >= -1);

            bool success = Interop.Kernel32.GetQueuedCompletionStatus(_completionPort, out var numberOfBytes, out var completionKey, out var pointerToOverlapped, timeoutMs);
            Debug.Assert(success || (Marshal.GetLastWin32Error() == WaitHandle.WaitTimeout));
            return success;
        }

        public int Release(int count)
        {
            Debug.Assert(count > 0);

            for (int i = 0; i < count; i++)
            {
                if(!Interop.Kernel32.PostQueuedCompletionStatus(_completionPort, 1, UIntPtr.Zero, IntPtr.Zero))
                {
                    var lastError = Marshal.GetLastWin32Error();
                    var exception = new OutOfMemoryException();
                    exception.HResult = lastError;
                    throw exception;
                }
            }
            return 0; // TODO: Track actual signal count to calculate this
        }

        public void Dispose()
        {
            Debug.Assert(_completionPort != IntPtr.Zero);

            Interop.Kernel32.CloseHandle(_completionPort);
            _completionPort = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }
    }
}
