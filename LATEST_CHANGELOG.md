## v1.9.0 (minor)

Changes since v1.8.0:

- Use Path.Join for the nested search path in the overlap test ([@Claude](https://github.com/Claude))
- Count a file matched by more than one pattern or search path once ([@Claude](https://github.com/Claude))
- test: assert the similarity bounds with IsLessThan [patch] ([@Claude](https://github.com/Claude))
- refactor: sum the matched line counts instead of accumulating in a loop [patch] ([@Claude](https://github.com/Claude))
- fix: score line similarity as a multiset, not a set [patch] ([@Claude](https://github.com/Claude))
- test: use the newer MSTest assertions in the diff tests [patch] ([@Claude](https://github.com/Claude))
- fix: emit correct unified diff hunks and headers [patch] ([@Claude](https://github.com/Claude))

