# OSMP

Open System Management Protocol is an open communication protocol designed for easy exchange of information or data and control commands between networked systems. The protocol uses the open standard JSON for data exchange over a secure WebSocket transport. It is designed to be simple, secure, ubiquitous and extensible to make it suitable for use in any domain. OSMP provides a framework for extension with minimal effort.

## Protocol Design
The protocol is designed to be simple, secure, consistent and well-defined. All
communication is encrypted via SSL/TLS, the WebSocket connection is regularly monitored
by pings preventing half open or dead connections. A continually incremented message
sequence number guarantees that either side can always consistently assign received
responses to their respective sent commands. This allows commands to be asynchronous,
meaning that the reply can be sent immediately or after a very long time while streamed
progress messages send feedback about the long running operation and keep the client
from timing out while waiting for the operation to complete. A long-running command can be
cancelled by the client if necessary. The protocol allows multiple commands to be active at
the same time. Server-to-client events ensure immediate state change updates without the
need for polling which saves network bandwidth. Last but not least, the protocol is designed
to be extensible for the use in all kinds of domains and applications by allowing to extend the
standard instruction set by domain specific instruction sets.

## Reference Implementation
Eqqon provides an open source reference implementation for C# that can be used for
integrating the protocol into your products.

## Domain Specific Instruction Sets
The Open System Management Protocol can be extended with domain specific
instruction sets which allow every product that employs it to add or override its own custom
commands and instructions. Anyone may create own instruction sets for their application domain.
