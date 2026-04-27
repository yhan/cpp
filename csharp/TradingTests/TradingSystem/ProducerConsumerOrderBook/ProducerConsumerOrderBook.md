recap : you introduced several solutions to resolve multiple threading order book questions:

1) odd/even sequence
   problem with this solution is the reader can spin forever
2) Read-Copy-Update (RCU):
3) combination of reader slots + writer published slot + writer target slot
   we approved that to manage single reader ( writer is always single ), two slots is not enough
   that's why R + 2 slots is the solution
