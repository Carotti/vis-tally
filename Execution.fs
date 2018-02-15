module Execution
    open CommonData
    open CommonLex

    /// f will be called on ins if the condition in ins
    /// is met by the flags in data else g is called
    let condExecute f g ins data =
        let (n, c, z, v) = (data.Fl.N, data.Fl.C, data.Fl.Z, data.Fl.V)
        let sw = function
            | true -> f ins data // Condition true
            | false -> g ins data // Condition false
        match ins.PCond with
        | Cal -> sw true
        | Cnv -> sw false
        | Ceq -> sw z
        | Cne -> sw (not z)
        | Chs -> sw c
        | Clo -> sw (not c)
        | Cmi -> sw n
        | Cpl -> sw (not n)
        | Cvs -> sw v
        | Cvc -> sw (not v)
        | Chi -> sw (c && not z)
        | Cls -> sw (not c || z)
        | Cge -> sw (n = v)
        | Clt -> sw (n <> v)
        | Cgt -> sw (not z && (n = v))
        | Cle -> sw (z || (n <> v))