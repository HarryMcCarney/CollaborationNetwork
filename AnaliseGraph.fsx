#load "./BuildSlackGraph.fsx"

open BuildSlackGraph

let graohs = 
    [0..1..11]
    |> List.map(fun i -> 
        let start  =  DateTime.Now.AddMonths(0-(i+1))
        let finish = DateTime.Now.AddMonths(-i)
        buildGraph ["tradekart"] start finish
        
    )

graohs
|> List.map(fun g -> 
    let edgesToRemove =  
        g.GetEdges() 
        |> Array.filter(fun (f,t,w) -> w < 40.0)
    g.RemoveManyEdges edgesToRemove)
|> List.map(fun g -> g.Degree "jason")
