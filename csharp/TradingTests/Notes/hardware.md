dotnet
when you pin your own thread to logical core, keep in mind, a managed thread can migrate to another OS level
thread, do this :

````csharp
[DllImport("kernel32.dll")]
private static extern UIntPtr SetThreadAffinityMask(IntPtr hThread, UIntPtr dwThreadAffinityMask);

[DllImport("kernel32.dll")]
private static extern IntPtr GetCurrentThread();

public static void PinCurrentThreadToLogical(int logicalProcessor)
{
    Thread.BeginThreadAffinity(); // CLR won't migrate this managed thread off its OS thread
    UIntPtr mask = (UIntPtr)(1UL << logicalProcessor);
    UIntPtr previous = SetThreadAffinityMask(GetCurrentThread(), mask);
    if (previous == UIntPtr.Zero)
    throw new InvalidOperationException("SetThreadAffinityMask failed: " + Marshal.GetLastWin32Error());
    // call Thread.EndThreadAffinity() when the thread is tearing down
}
````