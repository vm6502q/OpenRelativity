# Real Time QASM Language Reference

This README outlines the "Real Time QASM" micro-language for Unity3D. The language is built on the vm6502q/qrack quantum computing simulation framework. Real Time QASM optionally integrates with the OpenRelativity fork by WrathfulSpatula (Daniel Strano). OpenRelativity integration is its primary purpose, but the micro-language is suitable for quantum computer simulation in an Entity-Component-System ("ECS") environment even without relativistic considerations, and the package can stand alone.

## Summary

The absolute and relative timing of quantum computing and control operations become paradigmatically important when simulating a real time quantum computer in Entity-Component-System. In ECS, state updates happen in timed loops, and a quantum computer simulation, such as for pedagogical purposes, becomes stateful based on the timing loop. It is possible to use a framework like vm6502q/qrack to implement a quantum computing plugin, then write a custom ECS script for each new quantum computer program, but this tends to lead to a proliferation of unique components for each new quantum program, which becomes hard to manage. The point of Real Time QASM is to provide an API via a micro-language in order to maximize reuse of original components, by assigning text-based program assets to entities instead of subclassing components. The scope of these programs is intended to be pedagogical or for entertainment, (though vm6502q/qrack itself is suitable for high-performance applications).

Each instruction line in Real Time QASM is prefaced with an absolute time (in seconds) or a relative time offset from the last program line (with a leading "+" sign). The ECS update loop time (or a custom local entity clock time, as with OpenRelativity RelativisticObject instances) determines when quantum and classical instructions are run.

For maximum reuse and self-containment, Real Time QASM encapsulates "classical" (in addition to "quantum") control "hardware," which is arbitrarily extensible within the bounds of the program, only limited by physical hardware resources and practicality.

## Addressing modes

There are 3 addressing modes in Real Time QASM:

I - Immediate. Immediate values are integer and float literals. This mode is limited in use to (integer) "accumulator register" and "float register" arithmetic, and loop control.
A - Absolute. Absolute values use an integer literal to point to an absolute address of a bit, float, or accumulator register. This is the predominant addressing mode of the language.
X - Indirect. Indirect values use a "$" character in front a literal in an absolute argument to indicate that the absolute value should be read from a VARIABLE instead of being read as an absolute literal.

Basically, "$0", "$1", "$2", etc. are the only "variable names" available in the language. "$2" means "Use the value stored in integer accumulator 2, (or float address 2)." (Note that, depending on context, "$0" for example points to both integer accumulator address 0 and float register address 0, as instruction argument context will only allow one or the other, never both.)

## Syntax

- Comments are indicated by the "#" symbol and must be on their own line.
- Instructions (and comments) can be preceded by any amount of white space.
- After white space, the first part of the instruction must be a parsable time in (floating-point) seconds, such as "0.0" or "1.5". These are ABSOLUTE times, for the ECS update loop timing. Alternatively, RELATIVE times have a leading "+" sign (like "+0.5") and indicate a relative time offset from the previous line, in seconds. (The program clock can be directly set within the program.)
- Times can be out-of-order between clock-setting instructions, if conditional, looping, and relative time structures otherwise make sense. Multiple instructions can be dispatched at the same absolute or relative time, under the same conditions.
- Starting from the time value, each word must be followed by exactly ONE "space" character, or end-of-line.
- End-of-line terminates all instructions.
- "$" before an immediate argument makes it absolute; "$" before an absolute argument makes it indirect.

## API reference
(TBD)

## Copyright, License, and Acknowledgements

Real Time QASM is:

Copyright (c) Daniel Strano 2020. All rights reserved.

Real Time QASM is licensed under the MIT License.

Qrack is:

Copyright (c) Daniel Strano and the Qrack contributors 2017-2020. All rights reserved.

Qrack is licensed under the GNU Lesser General Public License V3.

See LICENSE.md in this and Qrack's project root for details.
