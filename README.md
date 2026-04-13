The build is already silent — that's normal for a successful compilation with no warnings. If you want verbose output showing what the compiler is doing:

g++ -std=c++17 -O2 -Wall -Wextra -v -o trading.exe main.cpp

The -v flag shows the full compiler pipeline. 
Or did you mean you want to see the program output too? In that case just chain it:

g++ -std=c++17 -O2 -Wall -Wextra -o trading.exe main.cpp && ./trading.exe




cpp basic

  Momentum:      145.5 ns/tick
  MeanReversion:  143.4 ns/tick
  MarketMaking:   198.6 ns/tick


cpp no virtual dispatch

 Momentum:      112.2 ns/tick
  MeanReversion:  112.3 ns/tick
  MarketMaking:   153.7 ns/tick
  
  
csharp

  Momentum:      303.5 ns/tick
  MeanReversion:  170.2 ns/tick
  MarketMaking:   232.1 ns/tick