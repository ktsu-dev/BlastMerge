## v1.0.20 (patch)

Changes since v1.0.19:

- Refactor variable declarations to use explicit types in multiple files ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.19 (patch)

Changes since v1.0.18:

- Enhance error handling in changelog generation ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.18 (patch)

Changes since v1.0.17:

- Enhance error handling in PSBuild.psm1 for changelog generation ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.17 (patch)

Changes since v1.0.16:

- Enhance error handling in PSBuild.psm1 and update derived cursor rules ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.16 (patch)

Changes since v1.0.15:

- Enhance build script to respect release flags for package publishing and GitHub releases ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.15 (patch)

Changes since v1.0.14:

- Fix build errors and improve code quality ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor command line handling to use CommandLineParser library ([@matt-edmondson](https://github.com/matt-edmondson))
- Split classes into their own files and convert to records ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance derived cursor rules and update test structures ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.14 (patch)

Changes since v1.0.13:

- Update YAML schema references in Winget manifests for consistency ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.13 (patch)

Changes since v1.0.12:

- Add YAML schema references to Winget manifests ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.12 (patch)

Changes since v1.0.11:

- Update Winget manifests to version 1.10.0 and add .NET Desktop Runtime dependency ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.11 (patch)

Changes since v1.0.10:

- Fix winget installer executable name in manifests and update PowerShell script for future compatibility ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.10 (patch)

Changes since v1.0.9:

- Add GitHub token to Winget manifests update step ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.9 (patch)

Changes since v1.0.8:

- Add release upload command for Winget manifests ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.8 (patch)

Changes since v1.0.7:

- Adjust publishing settings in Invoke-DotNetPublish function by disabling trimming for output files. This change aims to improve compatibility for self-contained applications. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.7 (patch)

Changes since v1.0.6:

- Optimize publishing settings in Invoke-DotNetPublish function by enabling trimming for smaller output files. This change enhances the build process for self-contained applications. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.6 (patch)

Changes since v1.0.5:

- Update SDK versions in global.json to 1.38.0 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.5 (patch)

Changes since v1.0.4:

- Fix issue trying to submit release twice ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.4 (patch)

Changes since v1.0.3:

- Update ktsu SDK versions in global.json from 1.35.0 to 1.36.0 for all components. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.3 (patch)

Changes since v1.0.2:

- Prepare BlastMerge for winget distribution by enhancing project configuration, consolidating publish functionality, and integrating winget manifest updates. Update the `Invoke-DotNetPublish` function to streamline publishing for all platforms and architectures, ensuring SHA256 hashes are generated for integrity verification. Create scripts for automating winget manifest updates and include necessary files for distribution. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.3-pre.1 (prerelease)

Incremental prerelease update.
## v1.0.2 (patch)

Changes since v1.0.1:

- Rename to BlastMerge ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.1 (patch)

Changes since v1.0.0:

- Refactor test suite to utilize Testable.IO.Abstractions for improved file access testing ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor tests to utilize mock file system and improve file handling ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0 (major)

- Refactor choice handling in CLI to use Contains method for user input ([@matt-edmondson](https://github.com/matt-edmondson))
- Update documentation and enhance primary feature focus for cross-repository synchronization ([@matt-edmondson](https://github.com/matt-edmondson))
- Add cursor ignore file and update runsettings for parallel test execution ([@matt-edmondson](https://github.com/matt-edmondson))
- Implement error handling for concurrent interactive functions in Spectre.Console ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor diffing functionality to fully integrate LibGit2Sharp ([@matt-edmondson](https://github.com/matt-edmondson))
- Integrate LibGit2Sharp for enhanced diffing functionality ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove redundant standalone test files and cleanup ([@matt-edmondson](https://github.com/matt-edmondson))
- Add debug test files and enhance FileDiffer functionality ([@matt-edmondson](https://github.com/matt-edmondson))
- Initial commit for DiffMore ([@matt-edmondson](https://github.com/matt-edmondson))
- Add LibGit2Sharp integration documentation and tests ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor FileDiffer logic and enhance debug test output ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor test files to remove unnecessary using directives and enhance empty file handling in diff algorithm ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance interactive merge display to show context for conflicts ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance debug test and FileDiffer functionality ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor conflict display in iterative merge mode to reduce excessive blank lines ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix null reference warnings and improve test file structure ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor input handling to improve command history functionality ([@matt-edmondson](https://github.com/matt-edmondson))
- Add missing cases suppression for ProcessSpecialKey method ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor MakeConflictColumnLayout for improved clarity and performance ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix test implementations and enhance static class usage ([@matt-edmondson](https://github.com/matt-edmondson))
- Replace LibGit2Sharp with DiffPlex for enhanced diffing functionality ([@matt-edmondson](https://github.com/matt-edmondson))
- Initial version ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor FileDiffer and update test files for improved compatibility ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance iterative merge process and improve diff display functionality ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance debug test and implement TUI for DiffMore CLI ([@matt-edmondson](https://github.com/matt-edmondson))
- Update derived cursor rules and refine CLI input handling ([@matt-edmondson](https://github.com/matt-edmondson))
- Implement manual block selection for merge conflicts ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor test project to resolve package conflicts and enhance mock filesystem compatibility ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor input handling and improve code quality ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove obsolete test files and sample application from the DiffMore project, including test1.txt, test2.txt, and related scripts. ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor test implementations and enhance static class usage ([@matt-edmondson](https://github.com/matt-edmondson))
- Implement command history functionality in DiffMore CLI ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor diffing logic and improve test coverage for core library ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance iterative merge functionality and improve user interface ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance LibGit2Sharp integration for improved diffing functionality ([@matt-edmondson](https://github.com/matt-edmondson))
- Update derived cursor rules documentation and address unnecessary using directives ([@matt-edmondson](https://github.com/matt-edmondson))
- Add directory comparison functionality and enhance test helper methods ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix prologue context display in side-by-side diffs ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance derived cursor rules and update FileDiffer logic ([@matt-edmondson](https://github.com/matt-edmondson))
- Implement iterative merge feature for multiple file versions ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance interactive merge functionality to display conflicts side by side ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance derived cursor rules documentation and add directory comparison guidelines ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance exception handling in test cleanup and update collection types ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor conflict resolution display and update editorconfig settings ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor diffing functionality to integrate LibGit2Sharp and enhance performance ([@matt-edmondson](https://github.com/matt-edmondson))
- Update Directory.Build.props and refine CLI input handling ([@matt-edmondson](https://github.com/matt-edmondson))
- Add SpecStory configuration and enhance project structure ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance error handling and improve interactive function management ([@matt-edmondson](https://github.com/matt-edmondson))
- Update test project dependencies and refactor test classes for mock filesystem compatibility ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove project reference to DiffMore.Core in CLI project ([@matt-edmondson](https://github.com/matt-edmondson))
