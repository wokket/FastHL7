# Design Notes

Aiming to:

* Exploit `Span<T>` and `Range`, `stackalloc` etc to avoid alloc'ing intermediate strings
* Don't alloc the values of structures the caller doesn't touch (ie no `private List<string> \_fields` etc)
* Be fully AoT Compatible if at all possible (Source Generators ftw)
* Comply with the Spec (no hacks!)
* Minimal actual spec reading on our side (ie no strongly typed segments)
* Provide spec compliant helpers for things like date time parsing, escape sequences etc


## Versioning
- This library is versioned using [Semantic Versioning](https://semver.org/).
- Major version number aligns with the version of .Net it targets (eg V9 for .Net 9, V10 for .Net 10 etc).
- Support for previous versions of .Net effectively terminates when the next major release (targeting the next .Net release) is made.
  - Security patches may be made to the previous major version, but no new features will be added.
- There is _no_ support for versions of .Net that MS also doesn't support.  
  - If you're stuck using an old unsupported version of .Net you have my condolences, but just don't take any upgrades... clearly frequent upgrades aren't your forte anyway!

 
