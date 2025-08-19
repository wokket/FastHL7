# Fast HL7 Library

There are (at least) two other .Net HL7 libraries that are well known in the community:
- [NHapi](https://github.com/nHapiNET/nHapi) is ridiculously fully featured, use it and love it if you want strongly typed spec compliant messages.
- [HL7-V2](https://github.com/Efferent-Health/HL7-V2) has a nice design ethos and is much lighter than NHapi, but has many years of legacy code and forked history, _interesting_ coding standards and Net4.8 back compat to worry about.  As a previous contributor to this library you may see similarities :)

These are both fantastic libraries, with different trade-offs, and I've used both really successfully in the past.

This library aims for the simplicity of HL7-V2, while being strictly modern .Net, and as lean as we can make it!  It is not intended to be the one HL7 library to rule them all!

It is currently focused on _reading_ messages, not building/writing them., and is mainly an exercise in self-learning to play with 
the new hotness in modern .NET.

It aims provides a set of tools for your toolkit, rather than being a fully integrated all-in-one framework, and rewards sparse access
to the message structure.  If you need _every single field_ in the message, one of the other libraries may be a better fit, or maybe not.
 
# Things to do (in no particular order)

- [x] Create a message
- [x] Support for Components (`^`)
- [x] and Sub-Components (`&`)
- [x] Support for Repeating fields (`~`)

- Querying (this whole space needs some cleaning up)
  - [x] Allow fetch of segments by index
  - [x] Allow fetch of segments by name/ordinal (`message.GetSegment("PID")`)
    - [x] Allow fetch of segments by name/ordinal with index (`message.GetSegment("PID(2)")`) for repeating segments
    - [x] Allow fetch of segments by name/ordinal with index and field (`message.QueryValue("PID(2).4")`?)  We don't know whether to return a Field, Segment, Component etc, so just the `ReadOnlySpan<char>` ?....
  - [x] Allow fetch of fields by index (`pid.GetField(4)`)
  - [x] Query by path (`message.Query("NK1(2).4.1")` or similar)


- Samples: 
  - [ ] MLLP listener that actually consumes and processes messages to prove out API 
- [x] Low-alloc DateTime conversion helpers

- [ ] Support MEL.ILogger for places we swallow exceptions etc
- [ ] Nuget when vaguely ready
 
- [x] Escape sequences for delimiter chars (https://docs.intersystems.com/latest/csp/docbook/DocBook.UI.Page.cls?KEY=EHL72_escape_sequences)
  - [x] Hex char support via Hex Encoding (0xA2) (https://web.archive.org/web/20160422163547/https://corepointhealth.com/resource-center/hl7-resources/hl7-escape-sequences)
  - [ ] Unicode char support via Html Encoding (&#162;) (https://hl7.org.au/archive/hl7v2wg/1278287.html#Appendix1ParsingHL7v2(Informative)-8Unicodecharacters) 
    - (Leaving for the caller for now, just use `WebUtility.HtmlDecode`)

- [ ] Support for efficiently _building_ messages (maybe)