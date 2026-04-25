/*
this sequence odd/even technique can have problem when Tread > TWrite

Write
0-------> 1 --------> 2
   start Wr        finishs Wr    

read :
   if read odd then spin
   if read even s1
      then read qty/price 
	  then read seq again s2
	  if s1 == s2, read is ok
	  else spin


long   qty;     // 8 bytes
double price;   // 8 bytes
long   ts;      // 8 bytes (the timestamp I included)
                // ─────────
                // 24 bytes total

*/
public bool TryReadLatest(out long qty, out double price, out long ts,
                          int maxAttempts = 16)
{
    SpinWait sw = default;
    for (int attempt = 0; attempt < maxAttempts; attempt++)
    {
        long s1 = Volatile.Read(ref _sequence);
        if ((s1 & 1) == 0)
        {
            qty   = _qty;
            price = _price;
            ts    = _timestampTicks;

            long s2 = Volatile.Read(ref _sequence);
            if (s1 == s2) return true;
        }
        sw.SpinOnce();
    }

    qty = 0; price = 0; ts = 0;
    return false;   // caller decides what to do
}


/*
what is the writer reordering problem here ?
_sequence = seq + 1;     // mark "writing"  ← could be moved AFTER the field writes
_qty = qty;              //                  on a weak memory model
_price = price;
_sequence = seq + 2;     // mark "done"     ← could be moved BEFORE the field writes



Possible reordered

t0: _sequence = 1
t1: _sequence = 2   ← D committed BEFORE B and C!
t2: _qty = newQty
t3: _price = newPrice


The CPU can execute and commit stores to its store buffer in one order, but flush them to the cache (where other cores see them) in a different order — on weakly-ordered architectures like ARM.


The __reordering___ question is entirely about the architecture family, not the bitness.




pin the producer thread on a sibling/nearby core => what do you mean by sibling ?you mean physical core or logical core? 


explain double buffering


The takeaway: when reasoning about memory reordering, always think "Intel/AMD vs ARM," never "32-bit vs 64-bit."


*/


 TSO (Total Store Order)  => x86/x86-64
 

Bitness (32-bit vs 64-bit): 
the width of registers, pointers, and the address space


A 32-bit Intel CPU and a 64-bit Intel CPU have the same strong memory ordering. A 32-bit ARM CPU and a 64-bit ARM CPU have the same weak memory ordering. The bitness is irrelevant.

why we designed cpu of weak memory model which reorders instructions ?
why .net JIT designs to reorder ? 

does memory bairrier has a cost ? is slower? 




Neeraj remarques :

NUMA node, hyper threading, cpu binding, sibling