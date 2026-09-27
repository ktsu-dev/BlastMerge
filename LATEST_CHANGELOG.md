## v1.10.0 (minor)

Changes since v1.9.0:

- Replace the nested ternary choosing the pattern result message ([@Claude](https://github.com/Claude))
- Build the binary-safety fixture paths with Path.Join ([@Claude](https://github.com/Claude))
- Use Path.Join for the per-repo test fixture paths ([@Claude](https://github.com/Claude))
- Use Assert.EndsWith in the trailing line ending test ([@Claude](https://github.com/Claude))
- Merge only files that share a name in ProcessBatch and ProcessSinglePattern ([@Claude](https://github.com/Claude))
- Pair deletions with insertions only within the same diff block ([@Claude](https://github.com/Claude))
- Keep the final line ending when writing a merged file ([@Claude](https://github.com/Claude))

