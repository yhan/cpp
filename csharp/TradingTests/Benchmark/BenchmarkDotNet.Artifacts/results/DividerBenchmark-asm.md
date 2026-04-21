## .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3 (Job: DefaultJob)

```assembly
; DividerBenchmark.NativeDivide()
       xor       r8d,r8d
       mov       rcx,[rcx+8]
       mov       r10d,[rcx+8]
       test      r10d,r10d
       jle       short M00_L01
       add       rcx,10
M00_L00:
       mov       r9d,[rcx]
       mov       edx,92492493
       mov       eax,edx
       imul      r9d
       add       edx,r9d
       mov       eax,edx
       shr       eax,1F
       sar       edx,2
       add       eax,edx
       add       r8d,eax
       add       rcx,4
       dec       r10d
       jne       short M00_L00
M00_L01:
       mov       eax,r8d
       ret
; Total bytes of code 62
```

## .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3 (Job: DefaultJob)

```assembly
; DividerBenchmark.FastDivide()
       xor       eax,eax
       mov       rcx,[rcx+8]
       mov       edx,[rcx+8]
       test      edx,edx
       jle       short M00_L01
       add       rcx,10
       nop       dword ptr [rax]
       nop       dword ptr [rax+rax]
M00_L00:
       mov       r8d,[rcx]
       movsxd    r8,r8d
       imul      r8,24924925
       shr       r8,20
       add       eax,r8d
       add       rcx,4
       dec       edx
       jne       short M00_L00
M00_L01:
       ret
; Total bytes of code 61
```

## .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3 (Job: DefaultJob)

```assembly
; DividerBenchmark.NativeModulo()
       xor       r8d,r8d
       mov       rcx,[rcx+8]
       mov       r10d,[rcx+8]
       test      r10d,r10d
       jle       short M00_L01
       add       rcx,10
M00_L00:
       mov       r9d,[rcx]
       mov       edx,92492493
       mov       eax,edx
       imul      r9d
       add       edx,r9d
       mov       eax,edx
       shr       eax,1F
       sar       edx,2
       add       eax,edx
       lea       edx,[rax*8]
       sub       edx,eax
       sub       r9d,edx
       add       r8d,r9d
       add       rcx,4
       dec       r10d
       jne       short M00_L00
M00_L01:
       mov       eax,r8d
       ret
; Total bytes of code 74
```

## .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3 (Job: DefaultJob)

```assembly
; DividerBenchmark.FastModulo()
       xor       eax,eax
       mov       rcx,[rcx+8]
       mov       edx,[rcx+8]
       test      edx,edx
       jle       short M00_L01
       add       rcx,10
M00_L00:
       mov       r8d,[rcx]
       movsxd    r10,r8d
       imul      r10,24924925
       shr       r10,20
       lea       r9d,[r10*8]
       sub       r9d,r10d
       sub       r8d,r9d
       add       eax,r8d
       add       rcx,4
       dec       edx
       jne       short M00_L00
M00_L01:
       ret
; Total bytes of code 60
```

