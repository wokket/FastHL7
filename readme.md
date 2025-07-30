# Fast HL7 Library

[NHapi](https://github.com/nHapiNET/nHapi) is ridiculously fully featured, use it and love it if you want strongly typed spec compliant messages.

[HL7-V2](https://github.com/Efferent-Health/HL7-V2) has a nice design ethos and is much lighter than NHapi, but has many years of legacy code and forked history, _interesting_ coding standards and Net4.8 back compat to worry about.  As a previous contributor to this library you may see similarities :)

This library aims for the simplicity of HL7-V2, while being strictly modern .Net, and as lean as we can make it!  It is not intended to be the one HL7 library to rule them all!

It is currently focused entirely on _reading_ messages, not building/writing them.
It provides a set of tools for your toolkit, rather than being a fully integrated all-in-one framework.
 
# Things to do (in no particular order)

- [x] Create a message
- [x] Allow fetch of segments by index
- [x] Allow fetch of segments by name/ordinal (`message.GetSegment("PID")`)
  - [x] Allow fetch of segments by name/ordinal with index (`message.GetSegment("PID(2)")`) for repeating segments
- [x] Allow fetch of fields by index (`pid.GetField(4)`)
- [ ] Support for Components and Subcomponents (`^`, `&`)
- [ ] Support for Repeating fields (`~`)
- [ ] Sample of a MLLP listener that actually consumes and processes messages to prove out API 
- [ ] Low-alloc DateTime conversion helpers
- [ ] Query by path (`message.Query("NK1(2).4.1")` or similar)
- [ ] Support ILogger for places we swallow exceptions etc
- [ ] Escape sequences (https://docs.intersystems.com/latest/csp/docbook/DocBook.UI.Page.cls?KEY=EHL72_escape_sequences)
