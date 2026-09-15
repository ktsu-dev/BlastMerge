## v1.2.0 (minor)

Changes since v1.1.0:

- fix: take the temp directory from the file system, not a hardcoded path [patch] ([@Claude](https://github.com/Claude))
- fix(test): build rooted test paths without Path.Combine [patch] ([@Claude](https://github.com/Claude))
- fix(test): keep TestPaths.Rooted from dropping the platform root [patch] ([@Claude](https://github.com/Claude))
- fix(test): make the suite pass on Linux and macOS [patch] ([@Claude](https://github.com/Claude))
- fix: stop MergeLines dropping and duplicating lines on one-sided diffs [patch] ([@Claude](https://github.com/Claude))
- test: build nested test paths with Path.Join ([@Claude](https://github.com/Claude))
- fix: report files that fail to hash instead of dropping them [minor] ([@Claude](https://github.com/Claude))
- ci: adopt the consolidated .NET workflow [patch] ([@Claude](https://github.com/Claude))
- ci: make the SonarQube quality gate opt in [patch] ([@matt-edmondson](https://github.com/matt-edmondson))
- ci: adopt the unified dotnet workflow [patch] ([@matt-edmondson](https://github.com/matt-edmondson))
- chore: store icon.png in LFS as .gitattributes declares ([@matt-edmondson](https://github.com/matt-edmondson))
- docs: scope build badge to the default branch ([@matt-edmondson](https://github.com/matt-edmondson))
- docs: correct README, DESCRIPTION and TAGS metadata ([@matt-edmondson](https://github.com/matt-edmondson))
- Stop Update SDKs failing when there is nothing to update ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix build for ktsu.Sdk 2.26.1 analyzers: Polyfill PrivateAssets (KTSU0007), InternalsVisibleTo for test project (KTSU0002) [patch] ([@matt-edmondson](https://github.com/matt-edmondson))
- Sync .editorconfig ([@KtsuTools](https://github.com/KtsuTools))
- Sync global.json ([@KtsuTools](https://github.com/KtsuTools))
- chore: update ktsu.Sdk to 2.21.1 [patch] ([@matt-edmondson](https://github.com/matt-edmondson))
- Sync icon.png ([@KtsuTools](https://github.com/KtsuTools))

