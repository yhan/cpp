# Pin thread to cpu core
Also ensure other threads can not work in PINed cpu core
1. prevent the os migrating my thread to another core
    refill L1/L2 and Core-local TLB(*)
1. stay in fixed core, cache line can't be overwritten by other threads, lower cache miss

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

explications NUMA / cache / TLB avec exemples,