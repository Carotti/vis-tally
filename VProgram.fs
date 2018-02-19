﻿namespace VisualTest
module VProgram =

    open Expecto
    open VCommon
    open VLog
    open Visual
    open VTest
    open System.Threading
    open System.IO



    /// configuration for this testing framework      
    /// configuration for expecto. Note that by default tests will be run in parallel
    /// this is set by the fields oif testParas above
    let expectoConfig = { Expecto.Tests.defaultConfig with 
                            parallel = testParas.Parallel
                            parallelWorkers = 6 // try increasing this if CPU use is less than 100%
                    }

    let runVisualTests () = 
        initCaches testParas
        let rc = runTestsInAssembly expectoConfig [||]
        finaliseCaches testParas
        rc // return an integer exit code - 0 if all tests pass