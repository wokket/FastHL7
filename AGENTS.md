# FastHL7 repository instructions

## Build and test

- The repository uses the SDK pinned in `global.json` (`11.0.100`, with patch roll-forward).
- Run commands from the repository root unless noted otherwise.
- Restore and build the full solution:
  - `dotnet restore src/FastHl7.slnx`
  - `dotnet build src/FastHl7.slnx -c Release --no-restore`
- The CI build is effectively `dotnet restore`, then `dotnet build --no-restore -c Release` from `src`.
- Run the complete unit-test project:
  - `dotnet run --project src/UnitTests/UnitTests.csproj -c Release`
- For one xUnit v3 test method, pass its fully qualified name to the Microsoft Testing Platform runner:
  - `dotnet run --project src/UnitTests/UnitTests.csproj -c Release -- --filter-method UnitTests.QueryTests.TestMessageQuery`
- For a whole test class:
  - `dotnet run --project src/UnitTests/UnitTests.csproj -c Release -- --filter-class UnitTests.QueryTests`
- The tests also support coverage/report flags used by CI: `dotnet test -c Release --no-build --report-xunit-trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml`, run from `src/UnitTests`.
- No separate lint or formatting command is configured. Formatting and analyzer behavior comes from `src/.editorconfig` and the normal build.
- BenchmarkDotNet benchmarks are not unit tests:
  - `dotnet run --project src/Benchmarks/Benchmarks.csproj -c Release -f net11.0`

## Architecture

- `src/FastHl7/` is the distributable NuGet library. Its public API is intentionally small and is focused on reading HL7 V2 messages rather than validating or building strongly typed messages.
- `Message` is the top-level stack-only (`readonly ref struct`) entry point. It parses segment ranges without copying the original character data, derives per-message delimiters from MSH, and delegates nested queries through segments and fields.
- `Segment`, `Field`, and `Delimiters` form the parsing hierarchy:
  - `Message` finds segments and handles segment-repeat names such as `PID(2)`.
  - `Segment` handles field indices and field repeats.
  - `Field` handles components and sub-components.
- `SplitHelper` centralizes range-based splitting. Keep allocation-sensitive parsing on `ReadOnlySpan<char>`/`Range` paths where possible.
- `MllpReader` and `MllpWriter` frame streams or `PipeReader`/`PipeWriter` data using MLLP VT/FS/CR bytes. Reader handlers have both synchronous `ReadOnlySpan<char>` and allocating `string` overloads; span handlers must finish processing before returning.
- `DateTimeExtensions` and `EscapeSequenceExtensions` provide low-allocation conversions and HL7 escape decoding. `EscapeSequenceExtensions` uses the delimiters from the parsed message.
- `src/UnitTests/` is an xUnit v3/Microsoft Testing Platform executable project. Tests commonly compare FastHl7 behavior directly with the `HL7-V2` package for compatibility.
- `src/Benchmarks/` contains BenchmarkDotNet comparisons for parsing, queries, escaping, date conversion, and MLLP I/O. `src/samples/SocketReader/` demonstrates a socket-based MLLP listener.
- `src/Directory.Build.props` applies the net11.0 target and nullable/implicit-usings defaults to all projects. `src/Directory.Packages.props` centrally versions test and benchmark dependencies.
- The library is packaged as `FastHl7` and enables AOT/trimming verification and XML documentation generation. Preserve public API compatibility and package metadata when changing `FastHl7.csproj`.

## Repository-specific conventions

- Preserve the zero/one-based indexing rules documented in `docs/design-notes.md`:
  - segment names are used by name; repeating segments such as `PID(1)` and `PID(2)` are 1-based;
  - field 0 is the segment name, with the first real field at index 1;
  - field repeats and components/sub-components are 1-based in HL7 query paths.
- Dot-delimited queries follow `SEGMENT[(repeat)].FIELD[(repeat)].COMPONENT.SUBCOMPONENT`, for example `PID(2).5.2` or `PV1.9(2).2`.
- Missing segments return an empty `Segment`; missing data should be represented through existing `HasValue`/empty-span conventions where possible. Invalid indexes and malformed query syntax throw `ArgumentOutOfRangeException`.
- The MSH segment is special: MSH field 1 is the field delimiter and MSH field 2 contains the encoding characters. Do not change the offsets used in `Segment.GetField(int)` without updating MSH-specific tests.
- Custom delimiters are message-specific: use `Delimiters` derived from MSH, not hard-coded `|`, `^`, `~`, `\`, or `&`.
- `Message`, `Segment`, and returned field/component values are span-oriented. Avoid adding APIs that require the ref structs to escape their stack lifetime or adding intermediate string allocations to hot parsing paths.
- Private fields use `_camelCase`, namespace declarations are file-scoped, and `src/.editorconfig` is authoritative for analyzer and formatting settings.
- Unit tests use xUnit `Fact`/`Theory` and raw multiline HL7 fixtures. Place behavior-specific cases in the existing test file that owns that parsing area (message, segment, field, query, MLLP, escaping, or date/time).
