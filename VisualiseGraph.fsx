#r "nuget: Cyjs.NET"
#load "./BuildSlackGraph.fsx"

#load "./BuildMeetsGraph.fsx"


open System
open Cyjs.NET
open BuildMeetsGraph


//let graph = buildGraph ["tradekart"] (DateTime.Now.AddMonths(-6)) (DateTime.Now)
let graph = buildGraph()

let nodeElements = 
    let format name degree = [ CyParam.color "blue"; CyParam.label (string name); CyParam.weight degree ]
    graph.GetVertices()
    |> Array.map(fun n -> Elements.node n (format n (graph.Degree n)))


let edgeElements = 
    graph.GetEdges()
    |> Array.filter(fun (v1, v2, weight) -> weight > 30000.0)
    |> Array.map (fun (v1, v2, weight) ->
        Elements.edge (string v1 + "_" + string v2) (string v1) (string v2) [ CyParam.weight (weight) ])


CyGraph.initEmpty ()
|> CyGraph.withElements edgeElements
|> CyGraph.withElements nodeElements
    |> CyGraph.withStyle "node" 
        [
            CyParam.content =. CyParam.label
            CyParam.width =. CyParam.weight
            CyParam.height =. CyParam.weight
            CyParam.Border.color "#A00975"
            CyParam.Border.width 3
        ]
    |> CyGraph.withStyle "edge" 
        [
            CyParam.Line.color "#3D1244"
         
        ]
    |> CyGraph.withSize (1200, 800)
    //|> CyGraph.withLayout (Layout.initCose (Layout.LayoutOptions.Cose(NodeOverlap = 400)))
    |> CyGraph.withLayout (Layout.initCircle (Layout.LayoutOptions.Generic()))
|> CyGraph.show
