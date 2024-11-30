Thank you for using Qrack (for quantum computer simulation)!

This package was originally designed to operate whether with or without the OpenRelativity package.
If the scripts in the Qrack package do not compile, you likely don't have the OpenRelativity package installed.
You can delete csc.rsp in the Qrack package root, remove the dependency on "net.qrack.openrelativity" in package.json,
and clear the `"references"` `GUID` array in the `net.qrack.qrack.asmdef` file.
Otherwise, you can simply install and use the OpenRelativity package alongside the Qrack package.

Happy Qracking! You rock!