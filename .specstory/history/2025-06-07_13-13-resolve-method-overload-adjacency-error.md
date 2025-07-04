<!-- Generated by SpecStory -->

# Resolve method overload adjacency error (2025-06-07 23:13:53)

_**User**_

For the code present, we get this error:
```
All 'ProcessSinglePatternWithPaths' method overloads should be adjacent. [+1 location]
```
How can I resolve this? If you propose a fix, please make it concise.

---

_**Assistant**_



---

Looking at the code, there are two `ProcessSinglePatternWithPaths` method overloads that are separated by other methods. The linter wants all overloads of the same method to be adjacent.

I need to move the second `ProcessSinglePatternWithPaths` method (the one taking `ProcessingCallbacks`) to be right after the first one.

---



---



---

Read file: BlastMerge.Core/Services/BatchProcessor.cs

---

Now I need to insert the second `ProcessSinglePatternWithPaths` overload right after the first one. I can see there are multiple blank lines that need to be fixed too.

---



---



---

Perfect! I've successfully resolved the linter error by moving the second `ProcessSinglePatternWithPaths` overload to be adjacent to the first one, which satisfies the requirement that all method overloads should be adjacent. I also fixed the multiple blank lines issue that was introduced.

The two `ProcessSinglePatternWithPaths` overloads are now properly adjacent, and the file follows the coding standards.

---

