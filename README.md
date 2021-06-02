
# Enum.StringExtensionGeneration
Source Generator to create extension methods for enums that allow easy lookup of localised strings for enum values.

This project aims to solve three problems experienced at Cogniva.

1. Given an enum value, provide a localized string that corresponds to the value (e.g. ErrorCode.AccessDenied might be "You do not have permissions to access this system" in English, and "Vous n'êtes pas autorisé à accéder à ce système" in French)
2. Sometimes we need different strings depending on context (e.g. a plain string, and another that has replacers so additional information can be injected, like inner exception messages)
3. If those lookups are done by concatenating strings and then calling `ResourceManager.GetString(myConcatenatedStringName)`, then it becomes extremely hard to tell if any given string is in use, since there's no actual code reference to it, and even its name never appears in full anywhere in the codebase

We're using the new C# Source Generators option to create a generator that will allow us to mark enum types as needing localized string lookups. The source generator will then generate extension methods that will allow you to easily look up appropriate localized strings, something like:

```csharp
ErrorCode resultingError = queryResult.ErrorCode;
string errorMessage = resultingError.GetDescription();
```
