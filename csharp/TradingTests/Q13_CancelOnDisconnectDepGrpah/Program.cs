using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Text;
using System;

// TODO Pearce-Kelly incremental topological order. Maintain...
/*
Q13 — Cancel-on-Disconnect Dependency Graph
Difficulty: Medium-hard | 50-60 min
Strategies depend on market data feeds and other strategies. A directed edge u → v means "if u disconnects, v must auto-cancel." Process events in time order:

STRAT s — register strategy s (initially CONNECTED)
FEED f — register feed f (initially CONNECTED)
DEPEND u v — v depends on u. u and v must be already registered; otherwise invalid (counted, no state change).
DOWN t x — x (feed or strategy) disconnects. Cascade: all CONNECTED nodes reachable from x along DEPEND edges become DISCONNECTED. Output: the number of newly disconnected nodes (excluding x itself if x was already disconnected — in which case no cascade and output 0).
UP t x — x reconnects. No cascade. Only x itself becomes CONNECTED. Its dependents stay disconnected unless they receive their own UP later.
STATUS t x — output CONNECTED or DISCONNECTED or UNKNOWN.
Constraints: N ≤ 2×10⁵, total distinct nodes ≤ 10⁵, edges ≤ 2×10⁵. A DEPEND that would create a cycle is invalid (counted, no state change).
Edge cases:

Cycle prevention requires reachability check at DEPEND time — naive O(V+E) per DEPEND would be O(N²)
A DOWN on an already-disconnected node: no cascade, output 0
Multiple paths to the same node — only count it once in the cascade Hint: cycle check on DEPEND can use the existing reachability — if v can reach u, adding u → v makes a cycle. For DOWN cascades, BFS/DFS from x along edges. The cycle check is the hard part; one approach is to maintain a topological order and check whether adding the edge violates it (incremental topo sort, Pearce-Kelly algorithm), but for the size constraints, a per-DEPEND DFS reachability check of O(V+E) is acceptable if total DEPEND count is bounded.
*/
/*
DEPEND event: 
  lazy build List<int>[] dependencies grpah. index is each node, list is its adjacent downstream nodeS
  
  "CYCLE DETECTION", if cycle is detected : count it, does not add the edge 
  

STRAT AND FEED are informatically the same thing
REGISTER :
 keep bool[] array

DOWN:
------ graph walk
output the nb of disconnected
need dfs/bfs to find nb of downstream nodes, count it ( already disconnected should not walk further)
Keep bool[] connected


UP:
update bool[] connected 

STATUS:
check bool[] connected

18
FEED md_eu
FEED md_us
STRAT vwap_eu
STRAT twap_us
STRAT smart_eu
DEPEND md_eu vwap_eu
DEPEND md_eu smart_eu
DEPEND vwap_eu smart_eu
DEPEND md_us twap_us
DEPEND smart_eu md_eu
STATUS 100 smart_eu
DOWN 101 md_eu
STATUS 102 vwap_eu
STATUS 102 twap_us
UP 103 vwap_eu
STATUS 104 vwap_eu
STATUS 104 smart_eu
DOWN 105 ghost_feed


DEPEND events ≤ 2×10⁵ (bounded by total events N ≤ 2×10⁵)
V ≤ 10⁵, E ≤ 2×10⁵
Worst case: 2×10⁵ DEPENDs × O(V+E) = 2×10⁵ × 3×10⁵ ≈ 6×10¹⁰ operations
*/

class Solution
{
    public static void Main(string[] args)
    {
        const string FEED = "FEED";
        const String STRAT = "STRAT";
        const string STATUS = "STATUS";
        const string DOWN = "DOWN";
        const string UP = "UP";
        const string DEP = "DEPEND";
        
       CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
       var fs = new FastScanner();
       var res=new StringBuilder();
       
       // state
       // ---------------------
       Dictionary<string, int> encode= new();
       int sz = (int)1e5 + 1; // BUG nodes count is not 2e5
       List<int>[] g = new List<int>[sz];
       
       bool[] connected= new bool[sz];
       int counter=0; 
       int invalidDep=0;
       int duplicatedRegistration=0;
       // ---------------------
       //for(int i=0; i< g.Length; i++) g[i] = new List<int>();
       var n = fs.NextInt();
       for(int i=0; i<n; i++)
       {
            string name= fs.Next();
            if(name == STATUS || name == DOWN || name == UP ){
                fs.NextInt(); // skip ts
            }
            
            switch(name)
            {
                case FEED: 
                case STRAT: 
                { 
                    int u = GetOrCreate(fs.Next()); 
                } break;
                
                case STATUS: 
                {
                    if(TryGet(fs.Next(), out int u)== false) res.AppendLine("UNKNOWN");
                    else {
                        if(connected[u] == true) res.AppendLine("CONNECTED");
                        else  res.AppendLine("DISCONNECTED");
                    }
                    
                } break;
                case DOWN: 
                {
/*                    ------ graph walk
output the nb of disconnected
need dfs/bfs to find nb of downstream nodes, count it ( already disconnected should not walk further)
bookKeeping bool[] connected */
                    if(TryGet(fs.Next(), out int u) == false)
                    {  /// BUG no output ! 
                        
                        res.AppendLine("0");
                        break; 
                    }
                    int disco = CountNewDisconnected(u);
                    res.AppendLine(disco.ToString());

                } break;
                case UP: // BUG not a check, stsate change ! 
                {
                    if(TryGet(fs.Next(), out int u))
                    {
                       connected[u] = true;
                    }
                } break;
                case DEP: 
                {
                    bool uok = TryGet(fs.Next(), out int u) ; bool vok=TryGet(fs.Next(), out int v); 
                    if( !uok || !vok ) { invalidDep++; break;} // MY initial bug, stream reading skipping. REMEMBER: read & declare var first !
                     
                    if(g[u] == null) g[u] = new List<int>();
                    if( g[u].Contains(v) == false && Cycle(u, v) == false )
                    {   
                         g[u].Add(v);
                    }
                    else 
                        invalidDep++;
                    
                } break;
            }
       }
     
       Console.Write(res);
       
       bool Cycle(int u, int v)
       {
         // test if u => v is added, a cycle will be formed
         // test if v=> u is possible ? yes : cycle: no_cycle
         // bfs just for fun
         if(u == v) return true; 
         HashSet<int> visited=new();
         Queue<int> q=new();
         q.Enqueue(v);
         while(q.Count > 0)
         {
            int deq = q.Dequeue(); // MY INTIIAL BUG RE ENQUEUE THE SUB GRAPH AGAIN IF HAVE DIAMOND
            var adj = g[deq];
            if(adj == null) continue;
            foreach(int n in adj)
            {
                if(visited.Contains(n) == true)
                    continue; // SHOULD NOT CHECK V U BUT N
                
                if(n == u) return true;
                visited.Add(n);
                q.Enqueue(n);
            }
         } 
         return false;
         
       }
       
       int CountNewDisconnected(int u) // u is diconnected ON DOWN
       {
            if(connected[u] == false) return 0;
            int disco = 0;
            Stack<int> stack=new();
            stack.Push(u);
            while(stack.Count > 0)
            {
                int pop = stack.Pop(); // is not connected
                // SPCIAL CASE diamond : "dfs frontier"
                if(connected[pop] == false)  // MY INITIAL BUG : revisted twice, the second time will counter double & but won't push the adjacent sub graph again
                    continue;
                
                // disconnect it ! 
                connected[pop] = false;
                disco++;

                List<int> adj = g[pop];
                if(adj == null) continue;
                foreach(var v in adj)
                { 
                    if(connected[v] == true)
                    {
                        stack.Push(v);
                    }
                }
            }
            return disco;
       }
       
       
        int GetOrCreate(string s)  
        {
            if(encode.TryGetValue(s, out int code) ==false )
            {
                code=++counter;
                encode[s] = code;
                connected[code] = true; // BUG, register : make it connected.
                return code;
            }
            duplicatedRegistration++; // BUG missing count;
            return code;
        }    
        
        bool TryGet(string s, out int code)  
        {
            if(encode.TryGetValue(s, out code) ==false )
            {   
                return false;
            }
            return true;
        }
    }
}

class FastScanner
{
    private readonly byte[] data = new byte[1 << 16];
    private int len, ptr;
    private readonly Stream stdin = Console.OpenStandardInput();
    private bool eof;

    public bool HasNext()
    {
        int c;
        do
        {
            c = Read();
        } while (c >= 0 && c <= 32);

        if (c < 0) return false;
        ptr--; // put it back
        return true;
    }

    private int Read()
    {
        if (eof) return -1;
        if (ptr >= len)
        {
            len = stdin.Read(data, 0, data.Length);
            ptr = 0;
            if (len <= 0)
            {
                eof = true;
                return -1;
            }
        }

        return data[ptr++];
    }

    public string Next()
    {
        int c;
        do
        {
            c = Read();
        } while (c <= 32 && c >= 0);

        var chars = new List<char>();
        while (c > 32)
        {
            chars.Add((char)c);
            c = Read();
        }

        return new string(chars.ToArray());
    }

    public int NextInt() => (int)NextLong();

    public long NextLong()
    {
        int c;
        do
        {
            c = Read();
        } while (c <= 32 && c >= 0);

        long v = 0;
        while (c > 32)
        {
            v = v * 10
                + c - '0';
            c = Read();
        }

        return v;
    }
    public double NextDouble()
    {
        int c;
        do
        {
            c = Read();
        } while (c <= 32 && c >= 0);

        // Handle optional sign
        bool negative = false;
        if (c == '-')
        {
            negative = true;
            c = Read();
        }
        else if (c == '+')
        {
            c = Read();
        }

        // Integer part
        double v = 0;
        while (c > 32 && c != '.' && c != 'e' && c != 'E')
        {
            v = v * 10 + (c - '0');
            c = Read();
        }

        // Fractional part
        if (c == '.')
        {
            c = Read();
            double factor = 0.1;
            while (c > 32 && c != 'e' && c != 'E')
            {
                v += (c - '0') * factor;
                factor *= 0.1;
                c = Read();
            }
        }

        // Exponent part (e.g., 1.5e-3)
        if (c == 'e' || c == 'E')
        {
            c = Read();
            bool expNegative = false;
            if (c == '-')
            {
                expNegative = true;
                c = Read();
            }
            else if (c == '+')
            {
                c = Read();
            }

            int exp = 0;
            while (c > 32)
            {
                exp = exp * 10 + (c - '0');
                c = Read();
            }

            v *= Math.Pow(10, expNegative ? -exp : exp);
        }

        return negative ? -v : v;
    }
    
    public decimal NextDecimal() => decimal.Parse(Next(), CultureInfo.InvariantCulture);
}
