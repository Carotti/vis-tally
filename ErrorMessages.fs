module ErrorMessages

    /// Failure message for impossible match cases
    let alwaysMatchesFM = "Should never happen! Match statement always matches."

    let noErrorsFM = "Should never happen! No errors at this stage."

    // *************************************************************************
    // Error messages for instruction parsing
    // *************************************************************************

    /// Error message for `Invalid register`
    let notValidRegEM = " is not a valid register."

    /// Error message for `Invalid offset`
    let notValidOffsetEM = " is not a valid offset."

    /// Error message for `Invalid instruction`
    let notValidFormatEM = " is not a valid instruction format."

    /// Error message for `Invalid literal`
    let notValidLiteralEM = " is not a valid literal."

    /// Error message for `Invalid shift` or `Invalid second operand`
    let notValidRegLitEM = " is not a valid literal or register."

    /// Error message for `Invalid flexible second operand`
    let notValidFlexOp2EM = " is an invalid flexible second operand"

    /// Error message for `Invalid suffix`
    let notValidSuffixEM = " is not a valid suffix for this instruction."

    // *************************************************************************
    // Error messages for instruction execution
    // *************************************************************************

