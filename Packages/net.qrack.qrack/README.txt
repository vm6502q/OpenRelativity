Thank you for using Qrack (for quantum computer simulation)!

This package was originally designed to operate whether with or without the OpenRelativity package.
If the scripts in the Qrack package do not compile, you likely don't have the OpenRelativity package installed.
You can delete csc.rsp in the Qrack package root, remove the dependency on "net.qrack.openrelativity" in package.json,
and clear the `"references"` `GUID` array in the `net.qrack.qrack.asmdef` file.
Otherwise, you can simply install and use the OpenRelativity package alongside the Qrack package.

QuantumBridge wraps Qrack's QBridge approximation method, (a chain or ring of whole simulator "patches," joined by truncated bonds,)
with the same space-time clock as QuantumSystem. It has the general single-qubit gates, Mtrx() and U(), and the Clifford couplers,
CNOT(), CY(), CZ(), their "anti-" controlled variants, SWAP(), ISWAP(), and AdjISWAP(), between any two qubits. It is read out,
without collapse, by MeasureShots(), PauliExpectation(), Prob(), and Prob3Axis(). A RealTimeQasmProgram runs on one if its
"QuantumBridge" field is set, instead of its "QuantumSystem" field.

Happy Qracking! You rock!