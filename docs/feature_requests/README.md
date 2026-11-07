# Feature requests and bugs

This folder is an axTask folder (`docs/axtask/SPEC.md`; the format is `docs/axkit/item-apps.md` section 4): the
maintainer's own feature requests and bugs, each one a small JSON file of typed fields in the list `Features` or
`Bugs`, so that they are in the repository, axTask manages them, axJSON opens any one of them, and a session of the
assistant reads them as files. From the repository's folder, `axtask docs\feature_requests` opens it (or
`AxKit.exe task <this folder>`); All tasks there lists what is open. GitHub's issues and pull requests are for outside
contributors (`CONTRIBUTING.md`) and wait, as the tracker of this work, until there are users (the maintainer's choice,
2026-11-06).

- **An item's fields**: Title; Done, checked when it is built or fixed; App, the app or apps it concerns; Asked
  (Features) or Seen (Bugs), the date; Version (Bugs), the axKit version that showed it; Notes, the ask or the bug in
  the maintainer's words, with how to see it. Each list's `list.json` holds the template, so Ctrl+N in axTask gives a
  new item these fields.
- **An item that becomes work** is written into its place first: a new app into `docs/axkit/plan.md`, Still to come; a
  change to an app into that app's spec; then the code. The commit that ships it checks Done or removes the file, so
  that what is open here is what is still open.
- **Nothing personal** in these files (`CLAUDE.md` rule 6): no path of this computer, no password, nothing pasted from
  axPass or a terminal. `PrivacyTests` reads the repository for an e-mail address or an account path.
