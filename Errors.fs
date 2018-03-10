module Errors    
    open Helpers
    open CommonData
    open CommonLex
    
    /// Error types for parsing.
    type ErrInstr =
        | ``Invalid literal``       of string
        | ``Invalid register``      of string
        | ``Invalid shift``         of string
        | ``Invalid flexible second operand``  of string
        | ``Invalid suffix``        of string
        | ``Invalid instruction``   of string
        | ``Syntax error``          of string
