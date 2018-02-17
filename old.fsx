

            let ops = 
                match splitMult with
                | [rn; rlst] ->
                    let splitList = splitAny (rlst.Replace("}", "")) ','
                    qp splitList
                    let firstReg = rn.Replace(",", "")
                    qp firstReg
                    let matcher x =
                        match x with
                        | RegListMatch x -> x
                        | _ -> []

                    let rec doAll f list =
                        match list with
                        | [] -> []
                        | head :: tail -> f head :: doAll f tail

                    doAll matcher splitList |> qp
            // let ops = 
            //     match splitMult with
            //     | [rn; rlst] ->
            //         let splitList = splitAny (rlst.Replace("}", "")) ','
            //         qp splitList
            //         let firstReg = rn.Replace(",", "")
            //         qp firstReg
            //         match firstReg :: splitList with
            //         | head :: tail when (regsValid (head :: tail)) ->
            //             (Ok tail)
            //             |> consMemMult head tail
            //         | _ -> Error "Fail"
            //     | _ -> Error "Shit happened"
            
            // let ops = 
            //     match splitMult with
            //     | [rn; rlst] ->
            //         let list = splitAny (rlst.Replace("}", "")) ','
            //         let firstReg = rn.Replace(",", "")
            //         qp (firstReg :: list)
            //         let rec regListMatch lst = 
            //             match lst with
            //             | rn :: rlst when (regsValid (rn :: rlst)) ->
            //                 match rlst with
            //                 | head :: tail ->
            //                     match head with
            //                     | RegListMatch reg -> (Ok regListMatch tail) |> consMemMult rn 


            //                 | RegListMatch reg -> (Ok (regListMatch tail)) |> consMemMult reg tail
            //                 | _ -> Error "Lord"
            //             | _ -> Error "God Almighty"
            //         regListMatch list
            //     | _ -> Error "Shit happened"
                