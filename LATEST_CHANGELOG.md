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

