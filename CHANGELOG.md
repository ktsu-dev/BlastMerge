## v1.11.5 (patch)

Changes since v1.11.4:

- Restore already-written copies when a merge write fails [patch] (Claude)
- Show binary files by hash in the side-by-side diff [patch] (Claude)
- Print one marker per line in the git-style character diff [patch] (Claude)
- Exclude ./bin/* and */node_modules/ style patterns instead of nothing [patch] (Claude)
- Build test paths with Path.Join, as the other tests do [patch] (Claude)
- Escape the recent batch name in the main menu and its run message [patch] (Claude)
- Map a replace block's choice by whole option, not by substring [patch] (Claude)
- Run an iterative merge when every copy is a different version [patch] (Claude)
- Match relative exclusion patterns with a separator at any depth [patch] (Claude)
- Join test paths with Path.Join instead of Path.Combine (Claude)
- Use the supplied file system for every read and write in the batch resolve phase and directory compare [patch] (Claude)
- Keep a delete block's lines when the user chooses Keep [patch] (Claude)

## v1.11.4 (patch)

Changes since v1.11.3:

- Test that choosing a reference skips a copy that already matches it (Claude)
- Check for matching content inside the sync's own error handling (Claude)
- Check the whole file for invalid UTF-8 before a merge writes it back [patch] (Claude)
- Offer differing versions for sync instead of identical copies [patch] (Claude)

## v1.11.3 (patch)

Changes since v1.11.2:

- Merge main to pick up the ? wildcard attribution test fix (Claude)
- Diff the line arrays directly so a one-blank-line side is not merged twice [patch] (Claude)

## v1.11.2 (patch)

Changes since v1.11.1:

- Hold the gathering progress test's call open without Thread.Sleep (Claude)
- Hold the gathering progress test's call open without Thread.Sleep (Claude)
- Report found files one at a time while patterns gather in parallel [patch] (Claude)
- Report found files one at a time while patterns gather in parallel [patch] (Claude)
- Expect the batch pattern, not the file name, for a ? wildcard (Claude)
- Expect the batch pattern, not the file name, for a ? wildcard (Claude)
- Escape unchanged lines and headers in the git-style diff [patch] (Claude)
- Show iterative merge similarity as a percentage [patch] (Claude)

## v1.11.1 (patch)

Changes since v1.11.0:

- Hold the gathering progress test's call open without Thread.Sleep (Claude)
- Report found files one at a time while patterns gather in parallel [patch] (Claude)
- Expect the batch pattern, not the file name, for a ? wildcard (Claude)
- Let Edit Batch clear the description with a "-" answer [patch] (Claude)
- Move the pattern attribution tests next to the helpers (Claude)
- Attribute batch results to the pattern that found each file [patch] (Claude)
- Search a pattern listed twice in a batch only once [patch] (Claude)

## v1.11.0 (minor)

Changes since v1.10.0:

- Reset the app data singleton in place without reflection (Claude)
- Give every test an assertion and assert booleans directly (Claude)
- Join test paths with Path.Join rather than Path.Combine (Claude)
- Keep test saves out of the real user profile (Claude)
- Add tests for the core library's uncovered paths (Claude)
- Make BlastMergeAppData.ResetForTesting actually reset the singleton (Claude)
- Add tests for the console application service and batch menu (Claude)
- Add tests for the CLI menu handlers (Claude)
- Add tests for the CLI sync, merge and async services (Claude)
- Add tests for the CLI display and progress services (Claude)
- Route console key reads through Spectre and add a console test base (Claude)
- Escape paths and names before they reach Spectre.Console markup [patch] (#76) (matt-edmondson)
- test: cover Utf8BomDetector on missing, empty and short files [patch] (Claude)
- test: cover the batch editor's pattern validation [patch] (Claude)
- fix: keep the UTF-8 byte order mark on merged files [patch] (Claude)
- fix: strip a leading **/ from file patterns so the example pattern matches [patch] (Claude)
- Move CI onto the shared ci-shared.yml pipeline (Claude)
- Merge main into fix/39-line-ending-only-differences (Claude)
- test: build the test paths with Path.Join [patch] (Claude)
- fix: report line-ending and final-newline differences instead of calling the files identical [patch] (Claude)
- fix: treat leading and trailing whitespace as a difference when comparing and diffing [patch] (Claude)
- fix: drop the phantom empty line at EOF from unified diffs [patch] (Claude)

## v1.10.6 (patch)

Changes since v1.10.5:

- Escape paths and names before they reach Spectre.Console markup [patch] (#76) (matt-edmondson)

## v1.10.5 (patch)

Changes since v1.10.4:

- test: cover Utf8BomDetector on missing, empty and short files [patch] (Claude)
- test: cover the batch editor's pattern validation [patch] (Claude)
- fix: keep the UTF-8 byte order mark on merged files [patch] (Claude)
- fix: strip a leading **/ from file patterns so the example pattern matches [patch] (Claude)

## v1.10.4 (patch)

Changes since v1.10.3:

- Move CI onto the shared ci-shared.yml pipeline (Claude)

## v1.10.3 (patch)

Changes since v1.10.2:

- Merge main into fix/39-line-ending-only-differences (Claude)
- test: build the test paths with Path.Join [patch] (Claude)
- fix: report line-ending and final-newline differences instead of calling the files identical [patch] (Claude)

## v1.10.2 (patch)

Changes since v1.10.1:

- fix: treat leading and trailing whitespace as a difference when comparing and diffing [patch] (Claude)

## v1.10.1 (patch)

Changes since v1.10.0:

- fix: drop the phantom empty line at EOF from unified diffs [patch] (Claude)

## v1.10.0 (minor)

Changes since v1.9.0:

- Replace the nested ternary choosing the pattern result message (Claude)
- Build the binary-safety fixture paths with Path.Join (Claude)
- Use Path.Join for the per-repo test fixture paths (Claude)
- Use Assert.EndsWith in the trailing line ending test (Claude)
- Merge only files that share a name in ProcessBatch and ProcessSinglePattern (Claude)
- Pair deletions with insertions only within the same diff block (Claude)
- Keep the final line ending when writing a merged file (Claude)

## v1.9.0 (minor)

Changes since v1.8.0:

- Use Path.Join for the nested search path in the overlap test (Claude)
- Count a file matched by more than one pattern or search path once (Claude)
- test: assert the similarity bounds with IsLessThan [patch] (Claude)
- refactor: sum the matched line counts instead of accumulating in a loop [patch] (Claude)
- fix: score line similarity as a multiset, not a set [patch] (Claude)
- test: use the newer MSTest assertions in the diff tests [patch] (Claude)
- fix: emit correct unified diff hunks and headers [patch] (Claude)

## v1.8.1 (patch)

Changes since v1.8.0:

- test: assert the similarity bounds with IsLessThan [patch] (Claude)
- refactor: sum the matched line counts instead of accumulating in a loop [patch] (Claude)
- fix: score line similarity as a multiset, not a set [patch] (Claude)
- test: use the newer MSTest assertions in the diff tests [patch] (Claude)
- fix: emit correct unified diff hunks and headers [patch] (Claude)

## v1.8.0 (minor)

Changes since v1.7.0:

- Rename BlastMerge.ConsoleApp to BlastMerge.Cli (Claude)

## v1.7.0 (minor)

Changes since v1.6.0:

- refactor: clear the analyzer findings on the binary-detection change (Claude)
- fix: validate the binary fixture path instead of letting Path.Combine drop the test directory (Claude)
- fix: skip binary files instead of merging them as UTF-8 text [minor] (Claude)

## v1.6.0 (minor)

Changes since v1.5.0:

- refactor: clear the analyzer findings on the line-ending change (Claude)
- test: combine the batch test path through the mock file system (Claude)
- fix: preserve the sources' line endings across every merge path [minor] (Claude)
- Add dotnet to TAGS.md (matt-edmondson)
- Gate Dependabot auto-merge on CI actually being green (Claude)
- test: use the MSTest assertions the analyzers ask for (matt-edmondson)
- fix: count the conflicts a manual block merge resolved [patch] (matt-edmondson)

## v1.5.3 (patch)

Changes since v1.5.2:

- Add dotnet to TAGS.md (matt-edmondson)

## v1.5.2 (patch)

Changes since v1.5.1:

- Gate Dependabot auto-merge on CI actually being green (Claude)

## v1.5.1 (patch)

Changes since v1.5.0:

- test: use the MSTest assertions the analyzers ask for (matt-edmondson)
- fix: count the conflicts a manual block merge resolved [patch] (matt-edmondson)

## v1.5.0 (minor)

Changes since v1.4.0:

- fix: match path exclusion globs with ktsu.TextFilter [minor] (Claude)

## v1.4.0 (minor)

Changes since v1.3.0:

- test: use the MSTest assertions the analyzers ask for (Claude)
- test: cover the command line handler's argument dispatch (Claude)
- feat: ship BlastMerge as a dotnet tool [minor] (Claude)

## v1.3.0 (minor)

Changes since v1.2.0:

- test: cover the write-failure and late-cancellation paths (Claude)
- test: use Path.Join and AddFile, matching the neighbouring batch tests (Claude)
- test: build the batch test paths with the existing CreateFile helper (Claude)
- fix: inline the test path combines with literal segments (Claude)
- fix: combine one path segment at a time in the new batch tests (Claude)
- fix: chain each merge into the next in ProcessSinglePattern [minor] (Claude)

## v1.2.0 (minor)

Changes since v1.1.0:

- fix: take the temp directory from the file system, not a hardcoded path [patch] (Claude)
- fix(test): build rooted test paths without Path.Combine [patch] (Claude)
- fix(test): keep TestPaths.Rooted from dropping the platform root [patch] (Claude)
- fix(test): make the suite pass on Linux and macOS [patch] (Claude)
- fix: stop MergeLines dropping and duplicating lines on one-sided diffs [patch] (Claude)
- test: build nested test paths with Path.Join (Claude)
- fix: report files that fail to hash instead of dropping them [minor] (Claude)
- ci: adopt the consolidated .NET workflow [patch] (Claude)
- ci: make the SonarQube quality gate opt in [patch] (matt-edmondson)
- ci: adopt the unified dotnet workflow [patch] (matt-edmondson)
- chore: store icon.png in LFS as .gitattributes declares (matt-edmondson)
- docs: scope build badge to the default branch (matt-edmondson)
- docs: correct README, DESCRIPTION and TAGS metadata (matt-edmondson)
- Stop Update SDKs failing when there is nothing to update (matt-edmondson)
- Fix build for ktsu.Sdk 2.26.1 analyzers: Polyfill PrivateAssets (KTSU0007), InternalsVisibleTo for test project (KTSU0002) [patch] (matt-edmondson)
- Sync .github\workflows\dotnet.yml (KtsuTools)
- Sync .editorconfig (KtsuTools)
- Sync global.json (KtsuTools)
- Sync .github\workflows\dotnet.yml (KtsuTools)
- chore: update ktsu.Sdk to 2.21.1 [patch] (matt-edmondson)
- Sync icon.png (KtsuTools)

## v1.1.4 (patch)

Changes since v1.1.3:

- chore: store icon.png in LFS as .gitattributes declares (matt-edmondson)
- docs: scope build badge to the default branch (matt-edmondson)
- docs: correct README, DESCRIPTION and TAGS metadata (matt-edmondson)

## v1.1.3 (patch)

Changes since v1.1.2:

- Stop Update SDKs failing when there is nothing to update (matt-edmondson)

## v1.1.2 (patch)

Changes since v1.1.1:

- Fix build for ktsu.Sdk 2.26.1 analyzers: Polyfill PrivateAssets (KTSU0007), InternalsVisibleTo for test project (KTSU0002) [patch] (matt-edmondson)
- Sync .github\workflows\dotnet.yml (KtsuTools)
- Sync .editorconfig (KtsuTools)
- Sync global.json (KtsuTools)
- Sync .github\workflows\dotnet.yml (KtsuTools)

## v1.1.1 (patch)

Changes since v1.1.0:

- chore: update ktsu.Sdk to 2.21.1 [patch] (matt-edmondson)
- Sync icon.png (KtsuTools)

## v1.1.0 (minor)

Changes since v1.0.0:

- chore: switch null guards and tests to Ensure/ThrowsExactly (matt-edmondson)
- refactor: modernize SDK targets and null validation (matt-edmondson)
- Remove serena/cursor files (matt-edmondson)
- Sync .github\workflows\dotnet.yml (KtsuTools)
- Remove legacy build scripts (matt-edmondson)
- Enhance test assertions and naming for clarity and robustness (matt-edmondson)
- Remove .github\workflows\project.yml (matt-edmondson)
- Update project configuration and workflows (matt-edmondson)
- Refactor file handling and improve test coverage for file operations (matt-edmondson)
- Implement GenerateUnifiedDiffFromContent method and enhance DiffPlexDiffer functionality (matt-edmondson)
- Fix failing tests and improve coverage for file processing and diffing functionality (matt-edmondson)
- Add new tests for FileHasher and IterativeMergeOrchestrator, improve coverage (matt-edmondson)
- Add code coverage analysis report for new code (matt-edmondson)
- Refactor coverage collection in Invoke-DotNetTest function and update CI workflow (matt-edmondson)
- Enhance coverage collection in Invoke-DotNetTest function (matt-edmondson)
- Enhance coverage collection in Invoke-DotNetTest function and update CI workflow (matt-edmondson)
- Refactor Invoke-DotNetTest function to streamline command execution (matt-edmondson)
- Update ktsu.AppDataStorage package version to 1.15.6 in Directory.Packages.props (matt-edmondson)
- Refactor coverage handling and update test project configuration (matt-edmondson)
- Enhance coverage output handling in Invoke-DotNetTest function (matt-edmondson)
- Refactor Invoke-DotNetTest function for improved command execution (matt-edmondson)
- Update coverage report path in CI workflow for improved file handling (matt-edmondson)
- Refactor coverage handling in CI workflow and PSBuild.psm1 (matt-edmondson)
- Enhance test coverage execution in CI workflow and PSBuild.psm1 (matt-edmondson)
- Refactor test coverage execution in CI workflow to enhance reliability and usability (matt-edmondson)
- Implement AsyncLocal-based FileSystemProvider for improved test isolation and concurrency handling (matt-edmondson)
- Refactor IterativeMergeOrchestrator to enforce non-nullable file system parameter (matt-edmondson)
- Enhance test coverage configuration and execution in .NET CI workflow (matt-edmondson)
- Refactor coverage command execution in .NET CI workflow for improved reliability (matt-edmondson)
- Refactor coverage collection in .NET CI workflow to enhance reliability and diagnostics (matt-edmondson)
- Refactor coverage collection in .NET CI workflow to use dotnet-coverage tool (matt-edmondson)
- Refactor logging messages in .NET CI workflow for clarity (matt-edmondson)
- Enhance .NET CI workflow with improved diagnostics and coverage reporting (matt-edmondson)
- Update file similarity calculation in IterativeMergeOrchestrator to use file system parameter (matt-edmondson)
- Enhance coverage reporting and CI workflow for SonarQube integration (matt-edmondson)
- Update coverage settings and enhance CI workflow for SonarQube integration (matt-edmondson)
- Fix indentation issue in coverage report generation step of CI workflow (matt-edmondson)
- Update SonarQube configuration and coverage report generation in CI workflow (matt-edmondson)
- Fix escaping in `Invoke-DotNetTest` for coverage report command (matt-edmondson)
- Update coverage reporting and SonarQube integration in CI workflow (matt-edmondson)
- Enhance SonarQube integration in GitHub Actions and update test coverage configuration (matt-edmondson)
- Refactor `Invoke-DotNetTest` to streamline test execution and update SonarQube configuration (matt-edmondson)
- Fix SonarQube configuration in `dotnet.yml` by adding missing quotation marks for the host URL (matt-edmondson)
- Enhance coverage collection in `Invoke-DotNetTest` and update SonarQube configuration (matt-edmondson)
- Update test command in `Invoke-DotNetTest` to use simplified coverage collection name (matt-edmondson)
- Fix nullable integer bug in LineDifference by adding null checks (matt-edmondson)
- This sonar config is incompatible with the sonar CI integration (matt-edmondson)
- Update project dependencies and enhance CI workflow (matt-edmondson)
- Refactor `BlastMergeAppData.cs` to remove conditional compilation for `System.Reflection` and `ResetForTesting` method (matt-edmondson)
- Fix hardcoded similarity value in merge status calculation (matt-edmondson)
- Update `BlastMergeAppData.cs` to conditionally include `System.Reflection` directive in debug mode (matt-edmondson)
- Add troubleshooting documentation for CI build error related to unused using directives (matt-edmondson)
- Enhance winget manifest updater script with MSBuild integration and project agnosticism (matt-edmondson)
- Refactor project structure and update dependencies (matt-edmondson)
- Fix validation for empty batch names in AppDataBatchManager (matt-edmondson)
- Fix test failures and improve test isolation in BatchProcessor and AppDataBatchManager (matt-edmondson)
- Add SonarQube configuration and improve CI workflow for .NET project (matt-edmondson)
- Refactor file grouping logic in FileDisplayService for improved clarity (matt-edmondson)
- Refactor loop in BatchOperationsMenuHandler for improved readability (matt-edmondson)
- Refactor loop patterns in BatchOperationsMenuHandler and AsyncFileDiffer for improved clarity and performance (matt-edmondson)
- Update using directives in CompareFilesMenuHandler and BatchProcessor for consistency (matt-edmondson)
- Refactor derived cursor rules and console application service for clarity and maintainability (matt-edmondson)
- Fix empty case clauses in switch statements to comply with SonarQube rule S3458 (matt-edmondson)
- Fix TOCTOU race condition in CreateTempDirectory method (matt-edmondson)
- Refactor MenuNames and related display classes for improved organization and clarity (matt-edmondson)
- Add analysis report on file organization between Console App and Core projects (matt-edmondson)
- Refactor ConsoleApplicationService methods to be static and pass instance as parameter (matt-edmondson)
- Refactor ConsoleApplicationService and CLI components for improved organization and functionality (matt-edmondson)
- Fix build errors in FileDiffer by removing tuple deconstruction for ColorCodePair (matt-edmondson)
- Refactor BlockContext and DiffPlexHelper for improved clarity and maintainability (matt-edmondson)
- Refactor to reduce nested types and improve code organization (matt-edmondson)
- Refactor ConsoleApplicationService to enhance readability and reduce cognitive complexity (matt-edmondson)
- Refactor ConsoleApplicationService to reduce cognitive complexity and improve readability (matt-edmondson)
- Refactor methods in BlockMerger and CharacterLevelDiffer to reduce cognitive complexity and improve readability (matt-edmondson)
- Refactor BlockMerger and DiffPlexDiffer for improved readability and reduced cognitive complexity (matt-edmondson)
- Refactor FileFinder and WhitespaceVisualizer for improved readability and reduced cognitive complexity (matt-edmondson)
- Refactor error handling in ConsoleApplicationService and FileComparisonDisplayService for improved maintainability (matt-edmondson)
- Refactor BatchProcessor to streamline parameter handling and enhance user feedback (matt-edmondson)
- Refactor BatchProcessor for improved readability and maintainability (matt-edmondson)
- Refactor code for improved maintainability and reduced complexity (matt-edmondson)
- Enhance security in FileFinder and improve error handling in DiffPlexDiffer (matt-edmondson)
- Fix regex timeout vulnerability in FileFinder service (matt-edmondson)
- Fix bugs in CompareDirectoriesAsync method to improve file comparison efficiency (matt-edmondson)
- Update authors and copyright information, enhance project descriptions, and refine package dependencies (matt-edmondson)
- Fix duplicate menu item and message display in batch processing (matt-edmondson)
- Enhance batch processing with search paths and exclusion patterns (matt-edmondson)
- Fix null initialization for collection properties in BlockContext (matt-edmondson)
- Refactor code to reduce duplication and improve validation (matt-edmondson)
- Add "Run Recent Batch" option to main menu and implement tracking functionality (matt-edmondson)
- Enhance file merge logic and improve user messaging (matt-edmondson)
- Refactor batch processing output for improved clarity (matt-edmondson)
- Fix unreachable code in whitespace condition and enhance batch processing (matt-edmondson)
- Enhance file comparison functionality and improve documentation (matt-edmondson)
- Implement safer file grouping in diff processing to prevent unrelated file merges (matt-edmondson)
- Enhance welcome and goodbye screens with explosive ASCII art and improve whitespace visualization (matt-edmondson)
- Enhance welcome and goodbye screens with explosive visuals and improve whitespace handling (matt-edmondson)
- Enhance welcome and goodbye screens with epic visuals and whitespace visualization (matt-edmondson)
- Enhance diff display with whitespace visualization and improve goodbye screen functionality (matt-edmondson)
- Enhance whitespace visualization and improve diff display functionality (matt-edmondson)
- Enhance iterative merge functionality and improve diff processing (matt-edmondson)
- Refactor side-by-side diff processing to enhance accuracy and context display (matt-edmondson)
- Refactor side-by-side diff processing for improved accuracy and clarity (matt-edmondson)
- Refactor side-by-side diff processing to enhance accuracy and clarity (matt-edmondson)
- Refactor side-by-side diff processing for improved accuracy and clarity (matt-edmondson)
- Enhance iterative merge display and improve diff line processing (matt-edmondson)
- Fix side-by-side diff alignment and improve file path labeling (matt-edmondson)
- Enhance iterative merge file path display for clarity (matt-edmondson)
- Fix navigation issues and enhance menu display (matt-edmondson)
- Fix navigation stack functionality and improve menu handling (matt-edmondson)
- Enhance navigation stack and refactor menu handling for improved user experience (matt-edmondson)
- Refactor exception handling and reduce cognitive complexity in batch operations (matt-edmondson)
- Implement comprehensive batch management system with CRUD operations (matt-edmondson)
- Implement navigation stack for improved user experience in menu handling (matt-edmondson)
- Refactor memory statistics display and reduce cognitive complexity (matt-edmondson)
- Refactor file comparison handling and reduce cognitive complexity (matt-edmondson)
- Enhance security measures for temporary file handling (matt-edmondson)
- Enhance security and improve side-by-side diff functionality (matt-edmondson)
- Refactor file comparison and UI handling for improved maintainability (matt-edmondson)
- Refactor ConsoleApplicationService and improve code quality (matt-edmondson)
- Extract nested class `InputState` into its own file to improve code organization and maintainability. Updated derived cursor rules to prohibit nested classes, enhancing adherence to best practices. Cleaned up `HistoryInput.cs` by removing the nested class definition. (matt-edmondson)
- Enhance console application structure and functionality (matt-edmondson)
- Refactor ConsoleApplicationService and introduce new services for improved functionality (matt-edmondson)
- Add SonarQube integration to CI workflow (matt-edmondson)
- Enhance console application structure and functionality (matt-edmondson)
- Code Cleanup (matt-edmondson)
- Refactor exception handling in ShowDetailedFileList method (matt-edmondson)
- Enhance console application with TUI menu and improve file processing (matt-edmondson)
- Enhance console application encoding settings (matt-edmondson)
- Refactor and restructure core application components (matt-edmondson)
- Refactor project structure and enhance functionality (matt-edmondson)
- Update project rules (matt-edmondson)
- Add batch processing functionality and command line options (matt-edmondson)
- Refactor variable declarations to use explicit types in multiple files (matt-edmondson)
- Enhance error handling in changelog generation (matt-edmondson)
- Enhance error handling in PSBuild.psm1 for changelog generation (matt-edmondson)
- Enhance error handling in PSBuild.psm1 and update derived cursor rules (matt-edmondson)
- Enhance build script to respect release flags for package publishing and GitHub releases (matt-edmondson)
- Enhance derived cursor rules and update test structures (matt-edmondson)
- Fix build errors and improve code quality (matt-edmondson)
- Split classes into their own files and convert to records (matt-edmondson)
- Refactor command line handling to use CommandLineParser library (matt-edmondson)
- Update YAML schema references in Winget manifests for consistency (matt-edmondson)
- Add YAML schema references to Winget manifests (matt-edmondson)
- Update Winget manifests to version 1.10.0 and add .NET Desktop Runtime dependency (matt-edmondson)
- Fix winget installer executable name in manifests and update PowerShell script for future compatibility (matt-edmondson)
- Add GitHub token to Winget manifests update step (matt-edmondson)
- Add release upload command for Winget manifests (matt-edmondson)
- Adjust publishing settings in Invoke-DotNetPublish function by disabling trimming for output files. This change aims to improve compatibility for self-contained applications. (matt-edmondson)
- Optimize publishing settings in Invoke-DotNetPublish function by enabling trimming for smaller output files. This change enhances the build process for self-contained applications. (matt-edmondson)
- Update SDK versions in global.json to 1.38.0 (matt-edmondson)
- Fix issue trying to submit release twice (matt-edmondson)
- Update ktsu SDK versions in global.json from 1.35.0 to 1.36.0 for all components. (matt-edmondson)
- Prepare BlastMerge for winget distribution by enhancing project configuration, consolidating publish functionality, and integrating winget manifest updates. Update the `Invoke-DotNetPublish` function to streamline publishing for all platforms and architectures, ensuring SHA256 hashes are generated for integrity verification. Create scripts for automating winget manifest updates and include necessary files for distribution. (matt-edmondson)
- Rename to BlastMerge (matt-edmondson)
- Refactor tests to utilize mock file system and improve file handling (matt-edmondson)
- Refactor test suite to utilize Testable.IO.Abstractions for improved file access testing (matt-edmondson)

## v1.0.22-pre.6 (prerelease)

Changes since v1.0.22-pre.5:

- Sync scripts\update-winget-manifests.ps1 (ktsu[bot])

## v1.0.22-pre.5 (prerelease)

Changes since v1.0.22-pre.4:

- Sync .github\workflows\dotnet.yml (ktsu[bot])

## v1.0.22-pre.4 (prerelease)

Changes since v1.0.22-pre.3:

- Sync scripts\PSBuild.psm1 (ktsu[bot])
- Sync .github\workflows\dotnet.yml (ktsu[bot])

## v1.0.22-pre.3 (prerelease)

Changes since v1.0.22-pre.2:

- Sync .specstory\.gitignore (ktsu[bot])

## v1.0.22-pre.2 (prerelease)

Changes since v1.0.22-pre.1:

- Add GitHub Copilot custom instructions ([@Copilot](https://github.com/Copilot))
- Initial plan for Copilot onboarding ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))

## v1.0.22-pre.1 (prerelease)

Changes since v1.0.21:

- Update project configuration and workflows (matt-edmondson)

## v1.0.21 (patch)

Changes since v1.0.20:

- Refactor file handling and improve test coverage for file operations (matt-edmondson)
- Implement GenerateUnifiedDiffFromContent method and enhance DiffPlexDiffer functionality (matt-edmondson)
- Fix failing tests and improve coverage for file processing and diffing functionality (matt-edmondson)
- Add new tests for FileHasher and IterativeMergeOrchestrator, improve coverage (matt-edmondson)
- Add code coverage analysis report for new code (matt-edmondson)
- Refactor coverage collection in Invoke-DotNetTest function and update CI workflow (matt-edmondson)
- Enhance coverage collection in Invoke-DotNetTest function (matt-edmondson)
- Enhance coverage collection in Invoke-DotNetTest function and update CI workflow (matt-edmondson)
- Refactor Invoke-DotNetTest function to streamline command execution (matt-edmondson)
- Update ktsu.AppDataStorage package version to 1.15.6 in Directory.Packages.props (matt-edmondson)
- Refactor coverage handling and update test project configuration (matt-edmondson)
- Enhance coverage output handling in Invoke-DotNetTest function (matt-edmondson)
- Refactor Invoke-DotNetTest function for improved command execution (matt-edmondson)
- Update coverage report path in CI workflow for improved file handling (matt-edmondson)
- Refactor coverage handling in CI workflow and PSBuild.psm1 (matt-edmondson)
- Enhance test coverage execution in CI workflow and PSBuild.psm1 (matt-edmondson)
- Refactor test coverage execution in CI workflow to enhance reliability and usability (matt-edmondson)
- Implement AsyncLocal-based FileSystemProvider for improved test isolation and concurrency handling (matt-edmondson)
- Refactor IterativeMergeOrchestrator to enforce non-nullable file system parameter (matt-edmondson)
- Enhance test coverage configuration and execution in .NET CI workflow (matt-edmondson)
- Refactor coverage command execution in .NET CI workflow for improved reliability (matt-edmondson)
- Refactor coverage collection in .NET CI workflow to enhance reliability and diagnostics (matt-edmondson)
- Refactor coverage collection in .NET CI workflow to use dotnet-coverage tool (matt-edmondson)
- Refactor logging messages in .NET CI workflow for clarity (matt-edmondson)
- Enhance .NET CI workflow with improved diagnostics and coverage reporting (matt-edmondson)
- Update file similarity calculation in IterativeMergeOrchestrator to use file system parameter (matt-edmondson)
- Enhance coverage reporting and CI workflow for SonarQube integration (matt-edmondson)
- Update coverage settings and enhance CI workflow for SonarQube integration (matt-edmondson)
- Fix indentation issue in coverage report generation step of CI workflow (matt-edmondson)
- Update SonarQube configuration and coverage report generation in CI workflow (matt-edmondson)
- Fix escaping in `Invoke-DotNetTest` for coverage report command (matt-edmondson)
- Update coverage reporting and SonarQube integration in CI workflow (matt-edmondson)
- Enhance SonarQube integration in GitHub Actions and update test coverage configuration (matt-edmondson)
- Refactor `Invoke-DotNetTest` to streamline test execution and update SonarQube configuration (matt-edmondson)
- Fix SonarQube configuration in `dotnet.yml` by adding missing quotation marks for the host URL (matt-edmondson)
- Enhance coverage collection in `Invoke-DotNetTest` and update SonarQube configuration (matt-edmondson)
- Update test command in `Invoke-DotNetTest` to use simplified coverage collection name (matt-edmondson)
- Fix nullable integer bug in LineDifference by adding null checks (matt-edmondson)
- This sonar config is incompatible with the sonar CI integration (matt-edmondson)
- Update project dependencies and enhance CI workflow (matt-edmondson)
- Refactor `BlastMergeAppData.cs` to remove conditional compilation for `System.Reflection` and `ResetForTesting` method (matt-edmondson)
- Fix hardcoded similarity value in merge status calculation (matt-edmondson)
- Update `BlastMergeAppData.cs` to conditionally include `System.Reflection` directive in debug mode (matt-edmondson)
- Add troubleshooting documentation for CI build error related to unused using directives (matt-edmondson)
- Enhance winget manifest updater script with MSBuild integration and project agnosticism (matt-edmondson)
- Refactor project structure and update dependencies (matt-edmondson)
- Fix validation for empty batch names in AppDataBatchManager (matt-edmondson)
- Fix test failures and improve test isolation in BatchProcessor and AppDataBatchManager (matt-edmondson)
- Add SonarQube configuration and improve CI workflow for .NET project (matt-edmondson)
- Refactor file grouping logic in FileDisplayService for improved clarity (matt-edmondson)
- Refactor loop in BatchOperationsMenuHandler for improved readability (matt-edmondson)
- Refactor loop patterns in BatchOperationsMenuHandler and AsyncFileDiffer for improved clarity and performance (matt-edmondson)
- Update using directives in CompareFilesMenuHandler and BatchProcessor for consistency (matt-edmondson)
- Refactor derived cursor rules and console application service for clarity and maintainability (matt-edmondson)
- Fix empty case clauses in switch statements to comply with SonarQube rule S3458 (matt-edmondson)
- Fix TOCTOU race condition in CreateTempDirectory method (matt-edmondson)
- Refactor MenuNames and related display classes for improved organization and clarity (matt-edmondson)
- Add analysis report on file organization between Console App and Core projects (matt-edmondson)
- Refactor ConsoleApplicationService methods to be static and pass instance as parameter (matt-edmondson)
- Refactor ConsoleApplicationService and CLI components for improved organization and functionality (matt-edmondson)
- Fix build errors in FileDiffer by removing tuple deconstruction for ColorCodePair (matt-edmondson)
- Refactor BlockContext and DiffPlexHelper for improved clarity and maintainability (matt-edmondson)
- Refactor to reduce nested types and improve code organization (matt-edmondson)
- Refactor ConsoleApplicationService to enhance readability and reduce cognitive complexity (matt-edmondson)
- Refactor ConsoleApplicationService to reduce cognitive complexity and improve readability (matt-edmondson)
- Refactor methods in BlockMerger and CharacterLevelDiffer to reduce cognitive complexity and improve readability (matt-edmondson)
- Refactor BlockMerger and DiffPlexDiffer for improved readability and reduced cognitive complexity (matt-edmondson)
- Refactor FileFinder and WhitespaceVisualizer for improved readability and reduced cognitive complexity (matt-edmondson)
- Refactor error handling in ConsoleApplicationService and FileComparisonDisplayService for improved maintainability (matt-edmondson)
- Refactor BatchProcessor to streamline parameter handling and enhance user feedback (matt-edmondson)
- Refactor BatchProcessor for improved readability and maintainability (matt-edmondson)
- Refactor code for improved maintainability and reduced complexity (matt-edmondson)
- Enhance security in FileFinder and improve error handling in DiffPlexDiffer (matt-edmondson)
- Fix regex timeout vulnerability in FileFinder service (matt-edmondson)
- Fix bugs in CompareDirectoriesAsync method to improve file comparison efficiency (matt-edmondson)
- Update authors and copyright information, enhance project descriptions, and refine package dependencies (matt-edmondson)
- Fix duplicate menu item and message display in batch processing (matt-edmondson)
- Enhance batch processing with search paths and exclusion patterns (matt-edmondson)
- Fix null initialization for collection properties in BlockContext (matt-edmondson)
- Refactor code to reduce duplication and improve validation (matt-edmondson)
- Add "Run Recent Batch" option to main menu and implement tracking functionality (matt-edmondson)
- Enhance file merge logic and improve user messaging (matt-edmondson)
- Refactor batch processing output for improved clarity (matt-edmondson)
- Fix unreachable code in whitespace condition and enhance batch processing (matt-edmondson)
- Enhance file comparison functionality and improve documentation (matt-edmondson)
- Implement safer file grouping in diff processing to prevent unrelated file merges (matt-edmondson)
- Enhance welcome and goodbye screens with explosive ASCII art and improve whitespace visualization (matt-edmondson)
- Enhance welcome and goodbye screens with explosive visuals and improve whitespace handling (matt-edmondson)
- Enhance welcome and goodbye screens with epic visuals and whitespace visualization (matt-edmondson)
- Enhance diff display with whitespace visualization and improve goodbye screen functionality (matt-edmondson)
- Enhance whitespace visualization and improve diff display functionality (matt-edmondson)
- Enhance iterative merge functionality and improve diff processing (matt-edmondson)
- Refactor side-by-side diff processing to enhance accuracy and context display (matt-edmondson)
- Refactor side-by-side diff processing for improved accuracy and clarity (matt-edmondson)
- Refactor side-by-side diff processing to enhance accuracy and clarity (matt-edmondson)
- Refactor side-by-side diff processing for improved accuracy and clarity (matt-edmondson)
- Enhance iterative merge display and improve diff line processing (matt-edmondson)
- Fix side-by-side diff alignment and improve file path labeling (matt-edmondson)
- Enhance iterative merge file path display for clarity (matt-edmondson)
- Fix navigation issues and enhance menu display (matt-edmondson)
- Fix navigation stack functionality and improve menu handling (matt-edmondson)
- Enhance navigation stack and refactor menu handling for improved user experience (matt-edmondson)
- Refactor exception handling and reduce cognitive complexity in batch operations (matt-edmondson)
- Implement comprehensive batch management system with CRUD operations (matt-edmondson)
- Implement navigation stack for improved user experience in menu handling (matt-edmondson)
- Refactor memory statistics display and reduce cognitive complexity (matt-edmondson)
- Refactor file comparison handling and reduce cognitive complexity (matt-edmondson)
- Enhance security measures for temporary file handling (matt-edmondson)
- Enhance security and improve side-by-side diff functionality (matt-edmondson)
- Refactor file comparison and UI handling for improved maintainability (matt-edmondson)
- Refactor ConsoleApplicationService and improve code quality (matt-edmondson)
- Extract nested class `InputState` into its own file to improve code organization and maintainability. Updated derived cursor rules to prohibit nested classes, enhancing adherence to best practices. Cleaned up `HistoryInput.cs` by removing the nested class definition. (matt-edmondson)
- Enhance console application structure and functionality (matt-edmondson)
- Refactor ConsoleApplicationService and introduce new services for improved functionality (matt-edmondson)
- Add SonarQube integration to CI workflow (matt-edmondson)
- Enhance console application structure and functionality (matt-edmondson)
- Code Cleanup (matt-edmondson)
- Refactor exception handling in ShowDetailedFileList method (matt-edmondson)
- Enhance console application with TUI menu and improve file processing (matt-edmondson)
- Enhance console application encoding settings (matt-edmondson)
- Refactor and restructure core application components (matt-edmondson)
- Refactor project structure and enhance functionality (matt-edmondson)
- Update project rules (matt-edmondson)
- Add batch processing functionality and command line options (matt-edmondson)

## v1.0.20 (patch)

Changes since v1.0.19:

- Refactor variable declarations to use explicit types in multiple files (matt-edmondson)

## v1.0.19 (patch)

Changes since v1.0.18:

- Enhance error handling in changelog generation (matt-edmondson)

## v1.0.18 (patch)

Changes since v1.0.17:

- Enhance error handling in PSBuild.psm1 for changelog generation (matt-edmondson)

## v1.0.17 (patch)

Changes since v1.0.16:

- Enhance error handling in PSBuild.psm1 and update derived cursor rules (matt-edmondson)

## v1.0.16 (patch)

Changes since v1.0.15:

- Enhance build script to respect release flags for package publishing and GitHub releases (matt-edmondson)

## v1.0.15 (patch)

Changes since v1.0.14:

- Merge 881f45e86b19f9c90378a1aa6cc50ffcb6ef5610 into 48ede99e477640286bcc0251eae7563c0a332825 (matt-edmondson)
- Enhance derived cursor rules and update test structures (matt-edmondson)
- Fix build errors and improve code quality (matt-edmondson)
- Split classes into their own files and convert to records (matt-edmondson)
- Refactor command line handling to use CommandLineParser library (matt-edmondson)

## v1.0.14 (patch)

Changes since v1.0.13:

- Update YAML schema references in Winget manifests for consistency (matt-edmondson)

## v1.0.13 (patch)

Changes since v1.0.12:

- Add YAML schema references to Winget manifests (matt-edmondson)

## v1.0.12 (patch)

Changes since v1.0.11:

- Update Winget manifests to version 1.10.0 and add .NET Desktop Runtime dependency (matt-edmondson)

## v1.0.11 (patch)

Changes since v1.0.10:

- Fix winget installer executable name in manifests and update PowerShell script for future compatibility (matt-edmondson)

## v1.0.10 (patch)

Changes since v1.0.9:

- Add GitHub token to Winget manifests update step (matt-edmondson)

## v1.0.9 (patch)

Changes since v1.0.8:

- Add release upload command for Winget manifests (matt-edmondson)

## v1.0.8 (patch)

Changes since v1.0.7:

- Adjust publishing settings in Invoke-DotNetPublish function by disabling trimming for output files. This change aims to improve compatibility for self-contained applications. (matt-edmondson)

## v1.0.7 (patch)

Changes since v1.0.6:

- Optimize publishing settings in Invoke-DotNetPublish function by enabling trimming for smaller output files. This change enhances the build process for self-contained applications. (matt-edmondson)

## v1.0.6 (patch)

Changes since v1.0.5:

- Update SDK versions in global.json to 1.38.0 (matt-edmondson)

## v1.0.5 (patch)

Changes since v1.0.4:

- Fix issue trying to submit release twice (matt-edmondson)

## v1.0.4 (patch)

Changes since v1.0.3:

- Update ktsu SDK versions in global.json from 1.35.0 to 1.36.0 for all components. (matt-edmondson)

## v1.0.3 (patch)

Changes since v1.0.2:

- Prepare BlastMerge for winget distribution by enhancing project configuration, consolidating publish functionality, and integrating winget manifest updates. Update the `Invoke-DotNetPublish` function to streamline publishing for all platforms and architectures, ensuring SHA256 hashes are generated for integrity verification. Create scripts for automating winget manifest updates and include necessary files for distribution. (matt-edmondson)

## v1.0.3-pre.1 (prerelease)

Changes since v1.0.2:

- Bump DiffPlex from 1.7.2 to 1.8.0 (dependabot[bot])

## v1.0.2 (patch)

Changes since v1.0.1:

- Rename to BlastMerge (matt-edmondson)

## v1.0.1 (patch)

Changes since v1.0.0:

- Refactor tests to utilize mock file system and improve file handling (matt-edmondson)
- Refactor test suite to utilize Testable.IO.Abstractions for improved file access testing (matt-edmondson)

## v1.0.0 (major)

- Update documentation and enhance primary feature focus for cross-repository synchronization (matt-edmondson)
- Refactor diffing logic and improve test coverage for core library (matt-edmondson)
- Implement error handling for concurrent interactive functions in Spectre.Console (matt-edmondson)
- Enhance iterative merge process and improve diff display functionality (matt-edmondson)
- Enhance iterative merge functionality and improve user interface (matt-edmondson)
- Fix prologue context display in side-by-side diffs (matt-edmondson)
- Replace LibGit2Sharp with DiffPlex for enhanced diffing functionality (matt-edmondson)
- Enhance LibGit2Sharp integration for improved diffing functionality (matt-edmondson)
- Refactor diffing functionality to fully integrate LibGit2Sharp (matt-edmondson)
- Refactor diffing functionality to integrate LibGit2Sharp and enhance performance (matt-edmondson)
- Add LibGit2Sharp integration documentation and tests (matt-edmondson)
- Integrate LibGit2Sharp for enhanced diffing functionality (matt-edmondson)
- Implement manual block selection for merge conflicts (matt-edmondson)
- Refactor MakeConflictColumnLayout for improved clarity and performance (matt-edmondson)
- Refactor conflict resolution display and update editorconfig settings (matt-edmondson)
- Refactor conflict display in iterative merge mode to reduce excessive blank lines (matt-edmondson)
- Refactor input handling and improve code quality (matt-edmondson)
- Add missing cases suppression for ProcessSpecialKey method (matt-edmondson)
- Refactor input handling to improve command history functionality (matt-edmondson)
- Implement command history functionality in DiffMore CLI (matt-edmondson)
- Enhance interactive merge display to show context for conflicts (matt-edmondson)
- Enhance interactive merge functionality to display conflicts side by side (matt-edmondson)
- Enhance error handling and improve interactive function management (matt-edmondson)
- Refactor project structure and enhance package management (matt-edmondson)
- Implement iterative merge feature for multiple file versions (matt-edmondson)
- Remove redundant standalone test files and cleanup (matt-edmondson)
- Enhance derived cursor rules and update FileDiffer logic (matt-edmondson)
- Update Directory.Build.props and refine CLI input handling (matt-edmondson)
- Update derived cursor rules and refine CLI input handling (matt-edmondson)
- Refactor choice handling in CLI to use Contains method for user input (matt-edmondson)
- Enhance debug test and implement TUI for DiffMore CLI (matt-edmondson)
- Refactor FileDiffer logic and enhance debug test output (matt-edmondson)
- Enhance debug test and FileDiffer functionality (matt-edmondson)
- Add debug test files and enhance FileDiffer functionality (matt-edmondson)
- Refactor test files to remove unnecessary using directives and enhance empty file handling in diff algorithm (matt-edmondson)
- Update derived cursor rules documentation and address unnecessary using directives (matt-edmondson)
- Fix null reference warnings and improve test file structure (matt-edmondson)
- Enhance exception handling in test cleanup and update collection types (matt-edmondson)
- Refactor test implementations and enhance static class usage (matt-edmondson)
- Fix test implementations and enhance static class usage (matt-edmondson)
- Refactor FileDiffer and update test files for improved compatibility (matt-edmondson)
- Add directory comparison functionality and enhance test helper methods (matt-edmondson)
- Enhance derived cursor rules documentation and add directory comparison guidelines (matt-edmondson)
- Refactor test project to resolve package conflicts and enhance mock filesystem compatibility (matt-edmondson)
- Update test project dependencies and refactor test classes for mock filesystem compatibility (matt-edmondson)
- Add cursor ignore file and update runsettings for parallel test execution (matt-edmondson)
- Add SpecStory configuration and enhance project structure (matt-edmondson)
- Remove obsolete test files and sample application from the DiffMore project, including test1.txt, test2.txt, and related scripts. (matt-edmondson)
- Remove project reference to DiffMore.Core in CLI project (matt-edmondson)
- Initial version (matt-edmondson)
- Initial commit for DiffMore (matt-edmondson)

