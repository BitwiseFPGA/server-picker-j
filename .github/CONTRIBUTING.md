# Contributing

Thanks for wanting to help. This is a fork of
[server-picker-x](https://github.com/FN-FAL113/server-picker-x) that reworks the interface, so a
fix to the core server logic may belong upstream as well.

## Reporting a bug

[Open an issue](https://github.com/cankhut/deadlock-server-picker/issues/new/choose) and pick
**Bug report**. Attach `deadlock_server_picker_log.txt` from the app folder if you can, it
usually shows what went wrong.

## Suggesting a feature

Open an issue with **Feature request** and describe the idea before writing any code, so we can
agree on it first.

## Pull requests

1. Fork the repository and create a branch from `main`.
2. Build and run it, see
   [Building from source](https://github.com/cankhut/deadlock-server-picker#building-from-source).
3. Run the tests with `dotnet test ServerPickerX.Tests.slnx`.
4. Keep each pull request to one change.

A few rules:

- The app runs as administrator, so it must only ever touch the firewall rules it created
  itself (their names start with `server_picker_x_`). Anything that changes how rules are
  added or removed needs extra care.
- Don't commit `settings.json`, log files or build output.
- Keep new UI consistent with the existing look. For bigger UI changes, open an issue first.
