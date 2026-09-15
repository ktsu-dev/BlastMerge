## v1.3.0 (minor)

Changes since v1.2.0:

- test: cover the write-failure and late-cancellation paths ([@Claude](https://github.com/Claude))
- test: use Path.Join and AddFile, matching the neighbouring batch tests ([@Claude](https://github.com/Claude))
- test: build the batch test paths with the existing CreateFile helper ([@Claude](https://github.com/Claude))
- fix: inline the test path combines with literal segments ([@Claude](https://github.com/Claude))
- fix: combine one path segment at a time in the new batch tests ([@Claude](https://github.com/Claude))
- fix: chain each merge into the next in ProcessSinglePattern [minor] ([@Claude](https://github.com/Claude))

