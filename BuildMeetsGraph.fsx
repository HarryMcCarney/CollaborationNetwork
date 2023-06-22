#r "nuget: FSharp.Data"
#r "nuget: fsharp.fgl"
#r "nuget: FSharp.FGL.ArrayAdjacencyGraph"

open System
open System.IO
open FSharp.Data
open FSharp.FGL
open FSharp.FGL.ArrayAdjacencyGraph


let  path = "C:/Users/harry/SlackNetwork/GoogleExport"
let  root = DirectoryInfo path

type MeetEvent = {
    Date : DateTime
    MeetingCode : string
    Event : string 
    Actor : string 
    Duration : float 
}

type Attendence = {
    Name : string 
    Duration : (DateTime  *  DateTime) 
} 

type Meet = {
    Start :  DateTime
    End :  DateTime option
    Participants: Attendence list 
    MeetId : string
}

let file = Path.Combine(root.FullName, "MeetsExport.csv")

let rows = CsvFile.Load(file)



let parseFile file = 
    let meets : Meet list = []

    rows.Rows
    |> Seq.map(fun r -> 
        {
            Date = DateTime.Parse((r.GetColumn "Date"));
            MeetingCode = (r.GetColumn "Meeting code");
            Event = (r.GetColumn "Event");
            Actor = (r.GetColumn "Actor");
            Duration = float (r.GetColumn"Duration (seconds)")
        }
    )
    |> Seq.filter(fun me -> not (String.IsNullOrEmpty(me.Actor)))
    |> Seq.sortBy(fun me -> me.Date)
    |> Seq.fold(fun (meetList: Meet list) (me: MeetEvent) -> 
        match me.Event with 
        | "Presentation started" -> 
            
                meetList 
                |> List.map(fun m -> if m.MeetId = me.MeetingCode && m.End.IsNone then {m with End = Some me.Date } else m )
                |> List.append([{Start = me.Date; End = None; Participants = []; MeetId = me.MeetingCode}])

        | "Presentation stopped" -> 

                meetList 
                |> List.map(fun m -> if m.MeetId = me.MeetingCode && m.End.IsNone then {m with End = Some me.Date } else m )
        
        | "Endpoint left" ->  
                meetList 
                |> List.map(fun m -> 
                    if m.MeetId = me.MeetingCode && m.End.IsNone 
                        then {m with Participants = m.Participants |> List.append ([{Name = me.Actor; Duration = ((me.Date.AddSeconds(0.0 - me.Duration), me.Date ))}]) } 
                    else m )

        | "Knocking accepted" | "Recording activity" | "Whiteboard started" | "Invitation sent" -> meetList

        | _ ->
            printfn "unknown event: %A"  me
            failwith "unknown event"

        ) meets

let parsedMeets = parseFile file

let rec combinations acc size set = seq {
  match size, set with 
  | n, x::xs -> 
      if n > 0 then yield! combinations (x::acc) (n - 1) xs
      if n >= 0 then yield! combinations acc n xs 
  | 0, [] -> yield acc 
  | _, [] -> () }

let getEdges() = 
    

    parsedMeets
    |> List.map(fun m -> combinations [] 2 m.Participants )
    |> Seq.concat
    |> Seq.map(fun al -> 
        let earliestEndDdate = (al |> List.minBy(fun a -> snd a.Duration)).Duration |> snd
        let latestStartDate = (al |> List.maxBy(fun a -> fst a.Duration)).Duration |> fst
        let delta = (earliestEndDdate - latestStartDate).TotalSeconds
        let weight = if delta > 0 then delta else 0 
        al[0].Name, al[1].Name, weight
        )
    |> Seq.groupBy(fun (f,t,w ) -> f,t )
    |> Seq.map(fun e -> e, (snd e) |> Seq.sumBy(fun (f,t,w)-> w) )
    |> Seq.map(fun (e,w) -> (fst e) , w)
    |> Seq.map(fun (e, w) -> (fst e), (snd e), w)
    |> Seq.toList


let buildGraph() =
    let edges = getEdges()
    let nodes = 
        edges 
        |> List.map(fun (f,t,w) -> f) 
        |> List.append(edges |> List.map(fun (f,t,w) -> t)) 
        |> List.map(fun n -> n, n)
        |> List.distinct

    ArrayAdjacencyGraph(nodes, edges)






