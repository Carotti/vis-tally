match evalOp1 op1 with
| Error e -> Error e
| Ok val1 -> match evalOp2 op2 with
             | Error e -> Error e
             | Ok val2 -> match evalFunction val1 val2 with
                          | Error e -> Error e
                          | Ok result -> writeBack result

Result.map (writeBack) (evalFunction val1 val2)