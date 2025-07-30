# Design Notes

Aiming to:

* Exploit `Span<T>` and `Range` etc to avoid alloc'ing intermediate strings
* Don't alloc the values of structures the caller doesn't touch (ie no `private List<string> \_fields` etc)
* Be fully AoT Compatible if at all possible (Source Generators ftw)
* Comply with the Spec (no hacks!)
* Minimal actual spec reading on our side (ie no strongly typed segments)
* Provide spec compliant helpers for things like date time parsing
