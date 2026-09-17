# ScrapLine agent instructions

- Preserve existing user changes and inspect `git status` before and after work.
- The Unity project is `ScrapLine/`; use the version in `ProjectSettings/ProjectVersion.txt`.
- Read `ScrapLine/TESTING.md` and use `Tools/Invoke-ScrapLineUnity.ps1` for compilation, validation, and tests.
- Unity must be closed for CLI runs. Only one agent may run Unity at a time.
- Run focused tests while iterating and the full EditMode suite before handoff. Do not add `-quit` to `-runTests`.
- Review Unity-created diffs and do not include unrelated generated changes.
- Leave final visual, interaction, and game-feel checks for manual Play Mode testing.
- Keep gameplay content data-driven and preserve save compatibility.
- Give an indication of what you are currently doing. Don't just say "executing a command on your pc" - say what command you are executing and why.
