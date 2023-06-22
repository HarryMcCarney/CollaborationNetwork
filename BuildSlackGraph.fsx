#r "nuget: fsharp.data"
#r "nuget: fsharp.fgl"
#r "nuget: FSharp.FGL.ArrayAdjacencyGraph"

open System.IO
open FSharp.Data
open FSharp.Data.JsonExtensions 
open System.Text.RegularExpressions
open System
open FSharp.FGL
open FSharp.FGL.ArrayAdjacencyGraph

let private path = "C:/Users/harry/SlackNetwork/SlackExportYear"
let private root = DirectoryInfo path

let private  channelFile = Path.Combine(root.FullName, "channels.json")


let private  channels = 
    seq {
    for c in JsonValue.Load(channelFile) do
    yield  (c?id.AsString()),(c?name.AsString())
    }
    |>Map

let private userFile = Path.Combine(root.FullName, "users.json")

let private  users = 
    seq {
    for c in JsonValue.Load(userFile) do
    yield  (c?id.AsString()),(c?name.AsString())
    }
    |>Map

type private  Message = {
    Channel : string
    Date : DateTime 
    User: string option
    Recipients: string list 
    Text: string 
}

let private stripRecipients text = 
    seq{
    for u in Regex.Matches (text, "(?<=\<)(.*?)(?=\>)") do
        yield users.TryFind(u.Value.Replace("@", ""))
    }
    |> Seq.choose(fun u -> u) // not sure why the users dont exit
    |> Seq.toList

let private getMessages (projects: string list) (start: DateTime) (finish: DateTime) = 
    seq {for d in Directory.EnumerateDirectories path do
            for f in Directory.EnumerateFiles d do 
              for m in JsonValue.Load(f) do
                if (
                    m.TryGetProperty "user").IsSome 
                    && ((projects = [] || projects |> List.contains(DirectoryInfo(d).Name))
                    && DateTime.Parse((Path.GetFileNameWithoutExtension f)) < finish
                    && DateTime.Parse((Path.GetFileNameWithoutExtension f)) > start
                    ) then
                    {
                    Channel = d;
                    Date = DateTime.Parse((Path.GetFileNameWithoutExtension f));
                    User =  users.TryFind(m?user.AsString()); 
                    Text = (m?text.AsString()); 
                    Recipients = (stripRecipients (m?text.AsString()))
                    }

    }

let private getNodes messages : LVertex<string, string> list = 
    messages
    |> Seq.choose(fun m -> m.User)
    |> Seq.append(
        messages
        |> Seq.map(fun m -> m.Recipients)
        |> Seq.concat
        )
    |> Seq.distinct
    |> Seq.map(fun u -> u, u )
    |> Seq.toList


let private  getEdges messages : LEdge<string, float> list = 
    messages 
    |> Seq.filter(fun m -> m.User.IsSome)
    |> Seq.map(fun m -> m.Recipients |> List.map(fun rs -> m.User.Value, rs) )
    |> Seq.concat
    |> Seq.countBy (fun (f, t ) -> f , t)
    |> Seq.map(fun (e, w) -> (fst e), (snd e), float w)
    |> Seq.toList

let buildGraph (projects: string list) (from: DateTime) (finish: DateTime) =
    let messages = getMessages projects from finish
    let nodes = getNodes messages
    let edges = getEdges messages
    ArrayAdjacencyGraph(nodes, edges)




